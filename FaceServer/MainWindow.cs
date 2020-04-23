using System;
using System.Windows.Forms;
using System.IO;
using TcpLib;
using Dopple;
using OpenTK;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

namespace FaceServer
{
    public partial class MainWindow : Form
    {
        private TcpServer Server;
        private FaceMeshService Provider;
        UDPer.UDPer udp;
        Recording activeRecording;
        List<ActiveMesh> meshes = new List<ActiveMesh>();
        int currentFrame;
        int CurrentSelectedMesh
        {
            get
            {
                return this.meshLV.SelectedIndices.Count > 0 ? this.meshLV.SelectedIndices[0] : -1;
            }
        }
        ListView.SelectedIndexCollection SelectedMeshes { get { return this.meshLV.SelectedIndices; } }

        float maxdeviation = 0.008f;
        bool clipYNormals = false;
        float meshPtScale = 1.0f;

        enum MeshVisType
        {
            Planes,
            PointCloud,
            RawMesh,
            Decimated
        }

        MeshVisType curMeshVisType = MeshVisType.RawMesh;
        Dictionary<MeshVisType, Panel> meshVisPanels = new Dictionary<MeshVisType, Panel>();
        public MainWindow(string[] args)
        {

            this.SuspendLayout();
            InitializeComponent();
            InitiazlieComponent2();
            this.ResumeLayout();
            ParseArgs(args);
        }

        bool autoLoadPtMesh = false;
        bool autoLoadRec = false;
        string[] loadFiles = null;
        void ParseArgs(string[] args)
        {
            for (int idx = 0; idx < args.Length; ++idx)
            {
                if (args[idx] == "-pt")
                {
                    idx++;
                    string[] filenames = args[idx].Split(';');
                    autoLoadPtMesh = true;
                    loadFiles = filenames;
                }
                if (args[idx] == "-rc")
                {
                    idx++;
                    string filename = args[idx];
                    autoLoadRec = true;
                    loadFiles = new string[] { filename };
                }
            }
        }

        public void RegisterZeroFormatter()
        {
        }
        void ResizeVideoWindow()
        {
            if (this.glView == null)
                return;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            this.glView.KeyStateChanged(e.KeyCode, true);
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            this.glView.KeyStateChanged(e.KeyCode, false);
            base.OnKeyUp(e);
        }

        enum MeshMode
        {
            Single,
            Dual,
            Range,
            MultiSelect
        }

        MeshMode meshMode = MeshMode.Single;
        Vector3 []selectedMeshColors = new Vector3[16];
        void InitiazlieComponent2()
        {
            foreach (string ename in Enum.GetNames(typeof(MeshVisType)))
            {
                this.meshVisTypeCB.Items.Add(ename);
            }
            meshVisPanels.Add(MeshVisType.Planes, this.planeVisPanel);
            meshVisPanels.Add(MeshVisType.PointCloud, this.ptcldVisPanel);
            meshVisPanels.Add(MeshVisType.RawMesh, this.fullMeshPanel);
            this.meshVisTypeCB.SelectedIndex = (int)curMeshVisType;

            this.glView = new GLView();
            this.glView.Location = new System.Drawing.Point(0, 0);
            this.glView.Dock = DockStyle.Fill;
            this.glView.OnRendererCreated += GlView_OnRendererCreated;
            this.glView.OnMeshUpdated += GlView_OnMeshUpdated;
            this.glView.OnItemPicked += GlView_OnItemPicked;
            this.splitContainer1.Panel1.Resize += Panel1_Resize;

            ResizeVideoWindow();
            this.splitContainer1.Panel1.Controls.Add(this.glView);
            this.dataPanel.Visible = false;
            this.DoubleBuffered = true;
            this.videoCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.depthCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.depthPlanesCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.facePtCloudCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.wholeFaceCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.depthPtsCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            RefreshViewCtrls();

            foreach (string meshModeName in Enum.GetNames(typeof(MeshMode)))
            {
                this.meshModeCB.Items.Add(meshModeName);
            }
            this.meshModeCB.SelectedIndex = (int)meshMode;

            
            for (int detail = 0; detail < 8; ++detail)
            {
                this.detailCB.Items.Add(Math.Pow(10, detail + 1).ToString());
            }
            this.detailCB.SelectedIndex = 3;

            for (int nrange = 0; nrange < 5; ++nrange)
            {
                selectRangeCB.Items.Add((1 << nrange).ToString());
            }
            this.selectRangeCB.SelectedIndex = 4;

            double len = (double)selectedMeshColors.Length;
            int colorIdx = 0;
            for (int i = 0; i < selectedMeshColors.Length; ++i)
            {
                double lerpVal = (colorIdx) / len;
                selectedMeshColors[i] = ColorScale.ColorFromHSL(lerpVal, 1, 0.75);
                colorIdx = (colorIdx + 7) % selectedMeshColors.Length;
            }
        }

        int GetMeshTriangles()
        {
            int detailIdx = this.detailCB.SelectedIndex;
            return (int)Math.Pow(10, detailIdx + 1);
        }

        struct SelectionIdx : IEqualityComparer<SelectionIdx>
        {
            public int ItemIdx;
            public int PartIdx;

            public bool Equals(SelectionIdx x, SelectionIdx y)
            {
                return x.ItemIdx == y.ItemIdx && x.PartIdx ==
                    y.PartIdx;
            }

            public int GetHashCode(SelectionIdx obj)
            {
                int hash = 13;
                hash = (hash * 7) + obj.PartIdx;
                hash = (hash * 7) + obj.ItemIdx;
                return hash;
            }
        }

        struct SelectedItem
        {
            public SPlane sp;
            public ActiveMesh am;
        }

        Dictionary<SelectionIdx, SelectedItem> SelectionIdxs = new Dictionary<SelectionIdx, SelectedItem>();
        private void GlView_OnItemPicked(object sender, GLView.OnItemPickedArgs e)
        {
            ActiveMesh am = meshes.Find((ams) => { return ams.id == e.ItemIdx; });
            if (am == null || am.meshPlanes == null)
                return;
            SPlane sp = am.meshPlanes[e.PartIdx];
            SelectionIdx sIdx = new SelectionIdx();
            sIdx.ItemIdx = e.ItemIdx;
            sIdx.PartIdx = e.PartIdx;

            if (!e.IsAdditive)
                SelectionIdxs.Clear();

            if (!SelectionIdxs.ContainsKey(sIdx))
                SelectionIdxs.Add(sIdx, new SelectedItem() { sp = sp, am = am });
            else
                SelectionIdxs.Remove(sIdx);

            string str = "";
            SelectedItem? prevItem = null;
            foreach (SelectedItem item in SelectionIdxs.Values)
            {
                str += $"Loc = {item.sp.node.Loc} normal = {item.sp.nrm}, ctr = {item.sp.ctr}\r\n";
                if (prevItem != null)
                {
                    str += $"dotprd = {Vector3.Dot(item.sp.nrm, prevItem.Value.sp.nrm)}  ";
                    str += $"dist = {(item.sp.ctr - prevItem.Value.sp.ctr).Length}\r\n";
                }                

                prevItem = item;
            }

            List<Visual> visuals = new List<Visual>();
            //GetDirMatchVisuals(visuals);

            this.glView.SetInfoText(str);
            GetSelectionVisuals(visuals);
            this.glView.AddVisuals(visuals);
        }

        void GetDirMatchVisuals(List<Visual> visuals)
        {
            if (SelectionIdxs.Count > 0)
            {
                SelectedItem item = SelectionIdxs.First().Value;

                float dotProdThresh = 0.99f;

                foreach (ActiveMesh amtest in this.meshes)
                {
                    if (amtest == item.am || amtest.meshPlanes == null ||
                        amtest.visible == false)
                        continue;
                    foreach (SPlane sptest in amtest.meshPlanes)
                    {
                        if (Vector3.Dot(sptest.nrm, item.sp.nrm) >
                            dotProdThresh)
                        {
                            Visual v = GetPlaneVisual(sptest, new Vector3(1, 1, 1));
                            v.wireframe = true;
                            visuals.Add(v);
                        }
                    }
                }
            }
        }
        Visual GetPlaneVisual(SPlane sp, Vector3 color)
        {
            List<Vector3> pos = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<uint> ind = new List<uint>();
            sp.GetMeshUV(pos, nrm, ind);
            Visual v = new Visual();
            v.pos = pos.ToArray();
            v.indices = ind.ToArray();
            v.normal = nrm.ToArray();
            v.shadingType = Visual.ShadingType.MeshColor;
            v.color = color;
            return v;
        }
        void GetSelectionVisuals(List<Visual> selList)
        {
            foreach (SelectedItem item in SelectionIdxs.Values)
            {
                selList.Add(GetPlaneVisual(item.sp, item.am.meshcolor));
            }
        }

        private ValueCtrl value1;
        private ValueCtrl imgDepthBlend;
        void InitValueCtrls()
        {
            this.imgDepthBlend = new FaceServer.ValueCtrl();
            this.value1 = new FaceServer.ValueCtrl();
            this.timelinePanel.Controls.Add(this.imgDepthBlend);
            this.timelinePanel.Controls.Add(this.value1);
            // 
            // imgDepthBlend
            // 
            this.imgDepthBlend.Location = new System.Drawing.Point(334, 25);
            this.imgDepthBlend.Maximum = new float[] { 1F };
            this.imgDepthBlend.Minimum = new float[] { 0F };
            this.imgDepthBlend.Name = "imgDepthBlend";
            this.imgDepthBlend.NumFields = 1;
            this.imgDepthBlend.ParamName = "Blend Image/Depth";
            this.imgDepthBlend.Size = new System.Drawing.Size(293, 115);
            this.imgDepthBlend.TabIndex = 11;
            this.imgDepthBlend.Values = new float[] { 0F };
            this.imgDepthBlend.OnNewValue += new System.EventHandler<FaceServer.OnNewValueArgs>(this.ImgDepthBlend_OnNewValue);
            // 
            // value1
            // 
            this.value1.AutoScroll = true;
            this.value1.AutoSize = true;
            this.value1.Location = new System.Drawing.Point(648, 25);
            this.value1.Maximum = new float[] { 1F, 1F };
            this.value1.Minimum = new float[] { 0F, 0F };
            this.value1.Name = "value1";
            this.value1.NumFields = 2;
            this.value1.ParamName = "Value 1";
            this.value1.Size = new System.Drawing.Size(478, 115);
            this.value1.TabIndex = 9;
            this.value1.Values = new float[] {        1F,
        1F};
            this.value1.OnNewValue += new System.EventHandler<FaceServer.OnNewValueArgs>(this.value1_OnNewValue);
        }
        void RefreshViewCtrls()
        {
            this.ptMeshPanel.Visible = this.meshes.Count > 0;
            this.meshLV.VirtualListSize = this.meshes.Count;
            this.recordingPanel.Visible = this.activeRecording != null;
            this.videoCB.CheckedChanged -= new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.depthCB.CheckedChanged -= new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.facePtCloudCB.CheckedChanged -= new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.wholeFaceCB.CheckedChanged -= new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.videoCB.Checked = (this.glView.VMode & GLView.ViewMode.eImage) != 0;
            this.depthCB.Checked = (this.glView.VMode & GLView.ViewMode.eFaceMesh) != 0;
            this.wholeFaceCB.Checked = (this.glView.VMode & GLView.ViewMode.eCombinedFace) != 0;
            this.videoCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.depthCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.facePtCloudCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.wholeFaceCB.CheckedChanged += new System.EventHandler(this.visibleLayers_CheckedChanged);
            this.deviationTB.Text = this.maxdeviation.ToString();
            this.alignStartFrmTB.Text = "0";
            this.alignEndFrmTB.Text = this.activeRecording != null ? (this.activeRecording.NumFrames() - 1).ToString() : "0";
            this.alignStepFrmTB.Text = "1";
        }

        private void Panel1_Resize(object sender, EventArgs e)
        {
            ResizeVideoWindow();
        }

        private void GlView_OnRendererCreated(object sender, EventArgs e)
        {
        }

        private void btnClose_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void MainWindow_Load(object sender, System.EventArgs e)
        {
            Provider = new FaceMeshService();
            Provider.OnLiveFrame += Provider_OnLiveFrame;
            Provider.OnDataReceived += Provider_OnDataReceived;
            Provider.OnNewRecording += Provider_OnNewRecording;
            Server = new TcpServer(Provider, 15555);
            this.ipAddressLabel.Text = TcpServer.GetLocalIPAddress();
            Server.Start();
            udp = new UDPer.UDPer();
            udp.Start();
            udp.Send("Face Server");
            RefreshActiveRecording();
            if (autoLoadPtMesh)
            {
                List<string> fullPaths = new List<string>();
                int idx = 0;
                foreach (string path in this.loadFiles)
                {
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (FileInfo fi in di.GetFiles("*.pts"))
                        {
                            fullPaths.Add(fi.FullName);
                        }
                    }
                    else
                    {
                        fullPaths.Add(path);
                    }
                }
                LoadPtMeshes(fullPaths.ToArray());
            }
            else if (autoLoadRec)
            {
                string fullpath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), this.loadFiles[0]);
                LoadRecording(fullpath);
            }
        }

        private void Provider_OnDataReceived(object sender, OnDataReceived e)
        {
            this.BeginInvoke(new Action(() =>
            {
                switch (e.state)
                {
                    case OnDataReceived.State.Start:
                        this.dataPanel.Visible = true;
                        this.timelinePanel.Visible = false;
                        this.dataLbl.Text = $"Receiving: 0 / {e.totalData / 1000000} mb";
                        this.dataProgress.Value = 0;
                        break;
                    case OnDataReceived.State.Sending:
                        this.dataLbl.Text = $"Receiving: {e.dataSoFar / 1000000 } mb / {e.totalData / 1000000 } mb";
                        this.dataProgress.Value = (int)(e.dataSoFar *
                            this.dataProgress.Maximum / e.totalData);
                        break;
                    case OnDataReceived.State.Complete:
                        this.dataLbl.Text = $"Done";
                        this.dataProgress.Value = 0;
                        this.dataPanel.Visible = false;
                        this.timelinePanel.Visible = true;
                        break;
                }
            }), new object[] { });
        }

        private void Provider_OnLiveFrame(object sender, OnLiveFrameArgs e)
        {
            if (e.IsNewSession)
                glView.InitViewPos(e.frame);
            glView.SetCurrentFrame(e.frame);
            this.settings.depthRange = new Vector2(0.1f, 10);
            RefreshSettings();
        }
        private void Provider_OnNewRecording(object sender, OnNewRecordingArgs e)
        {
            LoadRecording("recorded.str");
        }
        private void MainWindow_Closed(object sender, System.EventArgs e)
        {
            Server.Stop();
            udp.Stop();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new MainWindow(args));
        }

        private void viewRcrd_Click(object sender, EventArgs e)
        {
            LoadRecording("recorded.str");
        }

        void LoadRecording(string path)
        {
            if (File.Exists(path))
            {
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                byte[] data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                fs.Close();
                this.settings.autoAlign = true;
                this.activeRecording = new Recording(data, this.settings);
                this.activeRecording.OnFrameProcessed += ActiveRecording_OnFrameProcessed;
                this.activeRecording.OnMeshBuilt += ActiveRecording_OnMeshBuilt;
                this.activeRecording.Name = Path.GetFileNameWithoutExtension(path);
                this.activeRecording.BuildMeshesThreaded(this.settings);
                this.glView.VMode = GLView.ViewMode.eDepth;
                RefreshViewCtrls();
                RefreshActiveRecording();
                this.exportAllBtn.Enabled = false;
            }
        }
        private void ActiveRecording_OnMeshBuilt(object sender, OnMeshBuiltArgs e)
        {
            this.BeginInvoke(
                new Action(() => { this.exportAllBtn.Enabled = true; }), new object[] { });
        }

        void ExportToXYZ()
        {
            string xyzpath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "facemesh.xyz");
            FileStream fsxyz = new FileStream(xyzpath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fsxyz);

            foreach (PtMesh.V3L pos in this.meshes[0].mesh.pos)
            {
                sw.WriteLine($"{0} {1} {2}", pos.X, pos.Y, pos.Z);
            }
            fsxyz.Close();
        }

        Random colorGen = new Random();

        class ColorScale
        {
            public static Vector3 ColorFromHSL(double h, double s, double l)
            {
                double r = 0, g = 0, b = 0;
                if (l != 0)
                {
                    if (s == 0)
                        r = g = b = l;
                    else
                    {
                        double temp2;
                        if (l < 0.5)
                            temp2 = l * (1.0 + s);
                        else
                            temp2 = l + s - (l * s);

                        double temp1 = 2.0 * l - temp2;

                        r = GetColorComponent(temp1, temp2, h + 1.0 / 3.0);
                        g = GetColorComponent(temp1, temp2, h);
                        b = GetColorComponent(temp1, temp2, h - 1.0 / 3.0);
                    }
                }

                return new Vector3((float)r, (float)g, (float)b);
            }

            private static double GetColorComponent(double temp1, double temp2, double temp3)
            {
                if (temp3 < 0.0)
                    temp3 += 1.0;
                else if (temp3 > 1.0)
                    temp3 -= 1.0;

                if (temp3 < 1.0 / 6.0)
                    return temp1 + (temp2 - temp1) * 6.0 * temp3;
                else if (temp3 < 0.5)
                    return temp2;
                else if (temp3 < 2.0 / 3.0)
                    return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
                else
                    return temp1;
            }
        }

        void RebuildAllMeshes()
        {
            foreach (ActiveMesh am in meshes)
            {
                am.needrebuild = true;
            }

            foreach (ActiveMesh am in meshes)
                RebuildMeshVisual(am);
            this.glView.UpdateRender();
        }
        void RebuildMeshVisual(ActiveMesh am)
        {
            if (am.visible == false)
                return;
            if (am.visuals != null && !am.needrebuild)
                return;

            am.needrebuild = false;
            if (this.curMeshVisType == MeshVisType.Planes)
            {
                am.visuals = new Visual[1];
                am.octTree = am.mesh.GetTree(this.maxdeviation);
                if (clipYNormals)
                    am.octTree.ClipYNrms();

                //topNode.Simplify(Array.ConvertAll(this.pos, p => p.Gl), Array.ConvertAll(this.normal, p => p.Gl));
                am.meshPlanes = am.mesh.GetPlanarVisual(am.octTree, out am.visuals[0], am.id);
                am.visuals[0].color = am.meshcolor;
            }
            else if (this.curMeshVisType == MeshVisType.PointCloud)
            {
                am.visuals = new Visual[2];
                am.visuals[0] = am.mesh.GetRawCubeVisual(0.5f * this.meshPtScale * 0.002f);
                am.visuals[0].color = am.meshcolor;
                am.visuals[0].shadingType = Visual.ShadingType.MeshColor;

                am.visuals[1] = PtMesh.GetPointerVisual(Matrix4.CreateScale(new Vector3(0.01f, 0.01f, 0.5f)));
                am.visuals[0].color = new Vector3(0.5f, 1, 1);
                am.visuals[0].shadingType = Visual.ShadingType.MeshColor;
            }
            else if (this.curMeshVisType == MeshVisType.RawMesh)
            {
                am.visuals = new Visual[1];
                am.visuals[0] = am.mesh.GetMeshVisual();
                am.visuals[0].color = am.meshcolor;
                am.visuals[0].opacity = am.opacity;
                am.visuals[0].shadingType = am.shadingType;
                am.visuals[0].wireframe = am.wireframe;
            }
            else if (this.curMeshVisType == MeshVisType.Decimated)
            {
                am.visuals = new Visual[1];
                am.visuals[0] = am.mesh.GetDecimatedMeshVisual(GetMeshTriangles());
                am.visuals[0].color = am.meshcolor;
                am.visuals[0].shadingType = Visual.ShadingType.MeshColor;
                am.visuals[0].wireframe = false;
            }

            am.isdirty = true;
        }
        void LoadPtMeshes(string[] infiles)
        {
            List<string> files = new List<string>(infiles);
            files.Sort((f1, f2) => int.Parse(Path.GetFileNameWithoutExtension(f1).Substring(5)).CompareTo(int.Parse(
                Path.GetFileNameWithoutExtension(f2).Substring(5))));

            int idx = 0;
            foreach (string filename in files)
            {
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                PtMesh newMesh = PtMesh.FromBytes(bytes);
                newMesh.Init();
                ActiveMesh am = new ActiveMesh();
                am.id = idx;
                am.visible = false;
                am.mesh = newMesh;
                am.name = Path.GetFileName(filename);
                this.meshes.Add(am);
                idx++;
            }
            this.glView.VMode = GLView.ViewMode.eCombinedFace;
            this.glView.SelectedMesh = this.meshes.Last();
            RefreshViewCtrls();
            foreach (ActiveMesh am in this.meshes)
            {
                am.meshcolor = new Vector3(1, 1, 1);
            }
            RebuildAllMeshes();
            RefreshActiveMesh();
        }

        Quaternion GetRotationBetween(Vector3 u, Vector3 v)
        {
            // It is important that the inputs are of equal length when
            // calculating the half-way vector.
            u.Normalize();
            v.Normalize();

            // Unfortunately, we have to check for when u == -v, as u + v
            // in this case will be (0, 0, 0), which cannot be normalized.
            if (u == -v)
            {
                // 180 degree rotation around any orthogonal vector
                return new Quaternion();
            }

            Vector3 half = (u + v).Normalized();
            return new Quaternion(Vector3.Cross(u, half), Vector3.Dot(u, half));
        }
        void OnAlignmentComplete()
        {
            UpdateMeshLabels();
            this.glView.UpdateRender();
        }

        private void RefreshMeshAlignScore()
        {
            if (this.meshes.Count <= 1)
                return;
            int lod = this.meshes[0].mesh.buckets[0].ipt.Lod;
            return;
        }

        private void ActiveRecording_OnFrameProcessed(object sender, OnFrameProcessedArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (e.processed == e.total)
                {
                    this.frameProgress.Visible = false;
                    this.alignMergeBtn.Enabled = true;
                }
                else
                {
                    this.frameProgress.Visible = true;
                    int progval = (e.processed * this.frameProgress.Maximum) / e.total;
                    this.frameProgress.Value = progval;
                }

                if (e.curFrame == 0)
                    RefreshCurrentFrame(0);

            }), new object[] { });
        }

        void RefreshActiveMesh()
        {
            this.glView.SetCurrentMesh(this.meshes);
        }
        void RefreshActiveRecording()
        {
            if (this.activeRecording != null &&
                this.activeRecording.NumFrames() > 0)
            {
                Frame frame = this.activeRecording.GetFrame(0);
                glView.InitViewPos(frame);
                glView.SetCurrentFrame(frame);
                this.timelinePanel.Visible = true;
                this.frameLabel.Text = $"{this.activeRecording.NumFrames()} frames";
                this.frameSlider.Value = 0;
                this.frameSlider.Maximum = this.activeRecording.NumFrames() - 1;
                this.frameSlider.TickFrequency = 1;
                this.timeLabel.Text = "0.0s";
                this.settings.depthRange.X = this.activeRecording.MinDepthVal;
                this.settings.depthRange.Y = this.activeRecording.MaxDepthVal;
                RefreshSettings();
            }
            else
            {
            }
        }

        private void frameSlider_Scroll(object sender, EventArgs e)
        {
            RefreshCurrentFrame(this.frameSlider.Value);
        }

        void RefreshCurrentFrame(int currentFrame)
        { 
            this.activeRecording.RefreshCumulativeTransform(currentFrame);
            bool doAlignment = true;
            {
                Frame f = this.activeRecording.GetFrame(currentFrame);
                glView.SetCurrentFrame(f);
                Matrix4 meshWorldTransform = Matrix4.Identity;
                this.meshes.Clear();
                if (f.ptMesh != null)
                {
                    ActiveMesh am = new ActiveMesh();
                    am.id = 0;
                    am.visible = true;
                    am.mesh = f.ptMesh;
                    am.meshcolor = f.CumulativeAlignTrans.HasValue ? new Vector3(0.5f, 1, 0.5f) :
                        new Vector3(1, 0.5f, 0.5f);
                    am.overrideTransform = doAlignment ? f.CumulativeAlignTrans : null;
                    am.name = $"Frame{currentFrame}";
                    am.opacity = 0.5f;
                    this.meshes.Add(am);
                    meshWorldTransform = f.ptMesh.worldMatrix.Gl;
                }
                PtMesh faceMesh = f.hdr?.GetFaceMesh();
                if (faceMesh != null)
                {
                    faceMesh.ApplyMatrix(meshWorldTransform.Inverted());
                    ActiveMesh fm = new ActiveMesh();
                    fm.id = 1;
                    fm.visible = true;
                    fm.mesh = faceMesh;
                    fm.meshcolor = new Vector3(0, 1.0f, 0.25f);
                    fm.shadingType = Visual.ShadingType.VertexColors;
                    fm.wireframe = true;
                    fm.overrideTransform = doAlignment ? f.CumulativeAlignTrans : null;
                    fm.name = $"Frame{currentFrame}";
                    this.meshes.Add(fm);
                }
            }
            List<Vector3> pts = new List<Vector3>();
            List<uint> indices = new List<uint>();
            for (int frm = 0; frm < currentFrame; ++frm)
            {
                Frame afrm = activeRecording.GetFrame(frm);
                if (afrm.CumulativeAlignTrans != null)
                {
                    ActiveMesh am = new ActiveMesh();
                    am.id = 0;
                    am.visible = true;
                    am.mesh = afrm.ptMesh;
                    am.meshcolor = new Vector3(0.5f, 0.5f, 0.5f);
                    am.overrideTransform = doAlignment ? afrm.CumulativeAlignTrans : null;
                    am.name = $"Frame{frm}";
                    am.opacity = 0.5f;
                    this.meshes.Add(am);
                }
            }
            PtMesh pm = new PtMesh();
            pm.pos = pts.Select((p) => new PtMesh.V3L(p, 0, 0)).ToArray();
            pm.indices = indices.ToArray();
            ActiveMesh cm = new ActiveMesh();
            cm.id = 2;
            cm.visible = true;
            cm.mesh = pm;
            cm.meshcolor = new Vector3(0, 1, 0);
            cm.shadingType = Visual.ShadingType.MeshColor;
            cm.wireframe = false;
            this.meshes.Add(cm);

            RefreshActiveMesh();
            foreach (ActiveMesh ams in this.meshes)
                RebuildMeshVisual(ams);
            RefreshSettings();
            this.glView.SelectedMesh = this.meshes[0];
            this.glView.UpdateRender();
            double time = this.activeRecording.GetTimeStamp(currentFrame);
            this.timeLabel.Text = time.ToString("#.##s");
            this.frameLabel.Text = currentFrame.ToString();
            this.splitContainer1.Panel2.Invalidate();
        }

        Settings settings = new Settings(1.0f);
        void RefreshSettings()
        {
            this.settings.Apply(this.glView.VideoMesh);
            this.glView.UpdateRender();
        }

        private void ImgDepthBlend_OnNewValue(object sender, OnNewValueArgs e)
        {
            ValueCtrl vc = (ValueCtrl)sender;
            this.settings.imageDepthMix = vc.Values[0];
            RefreshSettings();
        }

        private void value1_OnNewValue(object sender, OnNewValueArgs e)
        {
            ValueCtrl vc = (ValueCtrl)sender;
            this.settings.imageScl.X = vc.Values[0];
            this.settings.imageScl.Y = vc.Values[1];
            RefreshSettings();
        }

        private void visibleLayers_CheckedChanged(object sender, EventArgs e)
        {
            this.glView.VMode =
                (this.videoCB.Checked ? GLView.ViewMode.eImage : 0) |
                (this.depthCB.Checked ? GLView.ViewMode.eDepth : 0) |                
                (this.wholeFaceCB.Checked ? GLView.ViewMode.eCombinedFace : 0) |
                (this.depthPlanesCB.Checked ? GLView.ViewMode.eDepthPlanes : 0) |
                (this.depthPtsCB.Checked ? GLView.ViewMode.eDepthPts : 0);
        }

        private void LoadMeshBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Point Clouds (*.pts)|*.pts";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.FileName = $"Frame{currentFrame}.pts";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadPtMeshes(ofd.FileNames);
            }
        }

        private void LoadRcdBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Recording (*.str)|*.str";
            ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofd.FileName = "recorded.str";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadRecording(ofd.FileName);
            }
        }

        private void BldPtMesh_Click(object sender, EventArgs e)
        {
        }

        private void ExportPtMesh_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Point Clouds (*.npts)|*.npts";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.FileName = $"Frame{currentFrame}.npts";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                PtMesh ptMesh = this.activeRecording.Frames[currentFrame].ptMesh;
                string path = sfd.FileName;
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                ptMesh.WriteAsciiPts(fs);
                fs.Close();
            }
        }

        private void SelectRangeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            MeshLV_SelectedIndexChanged(sender, e);
        }

        private void MeshLV_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (meshMode != MeshMode.MultiSelect)
            {
                foreach (ActiveMesh m in this.meshes)
                {
                    m.visible = false;
                }
            }
            string meshname = "";
            if (this.meshLV.SelectedIndices.Count == 0)
                return;

            if (this.CurrentSelectedMesh >= 0)
            {
                this.meshes[this.CurrentSelectedMesh].visible = true;
                this.glView.SelectedMesh = this.meshes[this.CurrentSelectedMesh];
                meshname = this.meshes[this.CurrentSelectedMesh].name;
            }
            else
                this.glView.SelectedMesh = null;

            if (meshMode == MeshMode.Dual || meshMode == MeshMode.Range)
            {
                int range = meshMode == MeshMode.Dual ? 2 :
                    int.Parse((string)this.selectRangeCB.SelectedItem);
                for (int ridx = 0; ridx < range; ++ridx)
                {
                    this.meshes[this.CurrentSelectedMesh + ridx].visible = true;
                    this.meshes[this.CurrentSelectedMesh + ridx].meshcolor =
                        selectedMeshColors[ridx];
                }
                RebuildAllMeshes();
                this.meshLV.Invalidate();
            }
            this.meshNameLbl.Text = meshname;
            UpdateMeshLabels();
        }

        void UpdateMeshLabels()
        {
            if (CurrentSelectedMesh < 0)
                return;
            ActiveMesh am = this.meshes[this.CurrentSelectedMesh];
            Vector4 axisAngle =
                am.rotation.ToAxisAngle();
            Vector3 offset = am.translation;

            this.meshOffsetLbl.Text = $"[{offset.X}, {offset.Y}, {offset.Z}]";
            this.meshRotateLbl.Text = $"[{axisAngle.X.ToString("0.00")}, {axisAngle.Y.ToString("0.00")}, " +
                $"{axisAngle.Z.ToString("0.00")}]  {(axisAngle.W * 180.0f / Math.PI).ToString("0.00")} deg";
        }

        private void GlView_OnMeshUpdated(object sender, EventArgs e)
        {
            RefreshMeshAlignScore();
            UpdateMeshLabels();
        }

        private void Label3_Click(object sender, EventArgs e)
        {

        }

        private void CubeBtn_Click(object sender, EventArgs e)
        {

        }

        PtCldAlignNative curAlignment = null;

        ActiveMesh []GetVisibleMeshes()
        {  return this.meshes.Where(m => m.visible).ToArray(); }
        static float Deg(float rad)
        {
            return rad * 180.0f / (float)Math.PI;
        }
        private void StepAlign_Click(object sender, EventArgs e)
        {
            this.glView.UpdateRender();
        }
        Dictionary<Tuple<PtMesh, PtMesh>, PTCloudAlignScore> alignScoreChecker = 
            new Dictionary<Tuple<PtMesh, PtMesh>, PTCloudAlignScore>();
        private void AlignAllMeshesBtn_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                ActiveMesh[] vmeshes = GetVisibleMeshes();

                string infotext = "";
                Matrix4 accumTransform = Matrix4.Identity;
                Stopwatch sw = Stopwatch.StartNew();
                for (int idx = 1; idx < vmeshes.Length; ++idx)
                {
                    PtCldAlignNative curAlignment = new PtCldAlignNative(vmeshes[idx - 1].mesh, vmeshes[idx].mesh);
                    Matrix4 outTransform = Matrix4.Identity;
                    while (curAlignment.AlignStep(out outTransform) < 2) ;
                    accumTransform = Matrix4.Mult(outTransform, accumTransform);

                    vmeshes[idx].alignmentTransform = outTransform;
                    vmeshes[idx].overrideTransform = accumTransform;
                    
                    /*
                    {
                        PTCloudAlignScore alignScorer = new PTCloudAlignScore(vmeshes[idx - 1].mesh, vmeshes[idx].mesh,
                            outTransform);
                        float bestScore = alignScorer.GetScore(outTransform);
                        this.BeginInvoke(new Action(() =>
                        {
                            infotext += $"score = {bestScore}\ttime = {sw.ElapsedMilliseconds}\r\n";
                            this.glView.SetInfoText(infotext);
                            this.glView.UpdateRender();
                        }), new object[] { });
                    }*/
                }
                sw.Stop();
                this.BeginInvoke(new Action(() =>
                {
                    infotext += $"{sw.ElapsedMilliseconds}\r\n";
                    this.glView.SetInfoText(infotext);
                    this.glView.UpdateRender();
                }), new object[] { });

            });
            t.Start();

            this.alignTransformCB.Checked = true;
        }

        private void ResetAlignBtn_Click(object sender, EventArgs e)
        {
            curAlignment = null;
            foreach (ActiveMesh am in this.meshes)
                am.overrideTransform = null;
            this.alignOffsetLbl.Text = "";
            this.alignRotateLbl.Text = "";
            this.glView.threedpointvis.LoadPoints(null, null);
            this.glView.UpdateRender();
        }

        private void AlignTransformCB_CheckedChanged(object sender, EventArgs e)
        {
            this.glView.ApplyAlignTransform = alignTransformCB.Checked;
            this.glView.UpdateRender();
        }

        private void ExportAllBtn_Click(object sender, EventArgs e)
        {
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                this.activeRecording.Name));
            for (int frameIdx = 0; frameIdx < this.activeRecording.Frames.Count; ++frameIdx)
            {
                PtMesh ptMesh = this.activeRecording.Frames[frameIdx].ptMesh;
                string ptsName = $"frame{frameIdx}.pts";
                string path = Path.Combine(di.FullName, ptsName);
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                byte[] bytes = ptMesh.ToBytes();
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
        }

        private void MeshLV_MouseDown(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = this.meshLV.GetItemAt(e.Location.X, e.Location.Y);
            if (e.X < this.meshLV.Columns[0].Width)
            {
                this.meshes[lvi.Index].visible =
                    !this.meshes[lvi.Index].visible;
                lvi.SubItems[0].Text = this.meshes[lvi.Index].visible ? "V" : "";
                RebuildAllMeshes();
                this.meshLV.Invalidate();
            }
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            this.meshes[this.CurrentSelectedMesh].translation = new Vector3(0, 0, 0);
            this.meshes[this.CurrentSelectedMesh].rotation = new Quaternion(new Vector3(0, 0, 0));
            UpdateMeshLabels();
            this.glView.UpdateRender();
        }

        private void PlanarBtn_Click(object sender, EventArgs e)
        {
            this.maxdeviation = float.Parse(deviationTB.Text);
            RebuildAllMeshes();
        }

        Form animateFrm = null;
        Label frameLbl;
        private void AnimateBtn_Click(object sender, EventArgs e)
        {
            if (animateFrm == null)
            {
                animateFrm = new Form();
                animateFrm.Width = 400;
                TrackBar trackBar = new TrackBar();
                trackBar.Location = new System.Drawing.Point(0, 0);
                trackBar.Width = animateFrm.ClientRectangle.Width;
                trackBar.Maximum = this.meshes.Count - 1;
                trackBar.ValueChanged += TrackBar_ValueChanged;
                foreach (ActiveMesh am in this.meshes)
                    am.visible = false;
                this.meshes[0].visible = true;
                trackBar.Value = 0;
                trackBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                frameLbl = new Label();
                frameLbl.Location = new System.Drawing.Point(0, trackBar.Bottom);
                frameLbl.Text = "Frame: 0";
                animateFrm.Controls.Add(trackBar);
                animateFrm.Controls.Add(this.frameLbl);
                animateFrm.Show();
            }
        }

        private void TrackBar_ValueChanged(object sender, EventArgs e)
        {
            foreach (ActiveMesh am in this.meshes)
            {
                if (am.visible)
                    am.visible = false;
            }

            TrackBar tb = (TrackBar)sender;
            this.meshes[tb.Value].visible = true;
            frameLbl.Text = $"Frame: {tb.Value}";
            RebuildMeshVisual(this.meshes[tb.Value]);
            this.meshLV.Invalidate();
            this.glView.UpdateRender();
        }

        private void ClipYChkBx_CheckedChanged(object sender, EventArgs e)
        {
            this.clipYNormals = this.clipYChkBx.Checked;
            RebuildAllMeshes();
        }

        private void MeshVisTypeCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string ename = (string)this.meshVisTypeCB.SelectedItem;
            MeshVisType vt = (MeshVisType)Enum.Parse(typeof(MeshVisType), ename);
            this.curMeshVisType = vt;
            foreach (var kv in this.meshVisPanels)
            {
                if (kv.Key == vt)
                {
                    kv.Value.Visible = true;
                    kv.Value.Dock = DockStyle.Fill;
                }
                else
                    kv.Value.Visible = false;
            }

            if (this.glView != null)
                RebuildAllMeshes();
        }

        private void PointScaleTB_Scroll(object sender, EventArgs e)
        {
            this.meshPtScale = (float)Math.Pow(2, this.pointScaleTB.Value);
            RebuildAllMeshes();
        }

        private void MeshLV_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            ActiveMesh am = this.meshes[e.ItemIndex];
            ListViewItem lvi = new ListViewItem();
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem());
            lvi.SubItems[0].Text = am.visible ? "V" : "";
            lvi.SubItems[1].Text = am.name;
            e.Item = lvi;
        }

        private void DetailCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebuildAllMeshes();
        }

        private void AlignMergeBtn_Click(object sender, EventArgs e)
        {
            List<Vector3> pts = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector3> col = new List<Vector3>();
            List<uint> indices = new List<uint>();

            int startidx = int.Parse(this.alignStartFrmTB.Text);
            int endidx = int.Parse(this.alignEndFrmTB.Text);
            int steps = int.Parse(this.alignStepFrmTB.Text);


            for (int idx = startidx; idx <= endidx; idx += steps)
            {
                this.activeRecording.RefreshCumulativeTransform(idx);
                PtMesh m = this.activeRecording.Frames[idx].ptMesh;
                if (!this.activeRecording.Frames[idx].CumulativeAlignTrans.HasValue)
                    continue;
                Matrix4 t = this.activeRecording.Frames[idx].CumulativeAlignTrans.Value;
                uint startIndOffset = (uint)pts.Count();
                pts.AddRange(m.pos.Select((p) => Vector3.TransformPosition(p.Gl, t)));
                nrm.AddRange(m.normal.Select((p) => Vector3.TransformNormal(p.Gl, t)));
                col.AddRange(m.color.Select((p) => p.Gl));
                indices.AddRange(m.indices.Select((i) => i + startIndOffset));
            }

            PtMesh ptMesh = new PtMesh();
            ptMesh.nPoints = pts.Count();
            ptMesh.pos = pts.Select((p) => new PtMesh.V3L(p, 0, 0)).ToArray();
            ptMesh.normal = nrm.Select((p) => new PtMesh.V3(p)).ToArray();
            ptMesh.color = col.Select((p) => new PtMesh.V3(p)).ToArray();
            ptMesh.indices = indices.ToArray();

            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                this.activeRecording.Name));
            string ptsName = $"alignedmesh.pts";
            string path = Path.Combine(di.FullName, ptsName);
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            byte[] bytes = ptMesh.ToBytes();
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();

            this.meshes.Clear();
            ActiveMesh am = new ActiveMesh();
            am.id = 0;
            am.visible = true;
            am.mesh = ptMesh;
            am.name = "combined";
            am.meshcolor = new Vector3(1, 1, 1);
            this.meshes.Add(am);

            this.glView.VMode = GLView.ViewMode.eCombinedFace;
            this.glView.SelectedMesh = this.meshes.Last();
            RefreshViewCtrls();
            RebuildAllMeshes();
            RefreshActiveMesh();
        }
    }
}

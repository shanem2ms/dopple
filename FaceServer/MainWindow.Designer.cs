namespace FaceServer
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label label1;
            this.dataLbl = new System.Windows.Forms.Label();
            this.dataProgress = new System.Windows.Forms.ProgressBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.timelinePanel = new System.Windows.Forms.Panel();
            this.alignStepLbl = new System.Windows.Forms.Label();
            this.currentFrmLbl = new System.Windows.Forms.Label();
            this.loadMeshBtn = new System.Windows.Forms.Button();
            this.loadRcdBtn = new System.Windows.Forms.Button();
            this.frameProgress = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.facePtCloudCB = new System.Windows.Forms.CheckBox();
            this.depthCB = new System.Windows.Forms.CheckBox();
            this.videoCB = new System.Windows.Forms.CheckBox();
            this.wholeFaceCB = new System.Windows.Forms.CheckBox();
            this.ipAddressLabel = new System.Windows.Forms.Label();
            this.recordingPanel = new System.Windows.Forms.Panel();
            this.alignEndFrmTB = new System.Windows.Forms.TextBox();
            this.alignStepFrmTB = new System.Windows.Forms.TextBox();
            this.alignStartFrmTB = new System.Windows.Forms.TextBox();
            this.alignMergeBtn = new System.Windows.Forms.Button();
            this.exportAllBtn = new System.Windows.Forms.Button();
            this.exportPtMesh = new System.Windows.Forms.Button();
            this.frameSlider = new System.Windows.Forms.TrackBar();
            this.frameLabel = new System.Windows.Forms.Label();
            this.timeLabel = new System.Windows.Forms.Label();
            this.ptMeshPanel = new System.Windows.Forms.Panel();
            this.alignAllMeshesBtn = new System.Windows.Forms.Button();
            this.selectRangeCB = new System.Windows.Forms.ComboBox();
            this.meshModeCB = new System.Windows.Forms.ComboBox();
            this.meshvzlbl = new System.Windows.Forms.Label();
            this.visSettingsPanel = new System.Windows.Forms.Panel();
            this.fullMeshPanel = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.detailCB = new System.Windows.Forms.ComboBox();
            this.ptcldVisPanel = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.pointScaleTB = new System.Windows.Forms.TrackBar();
            this.planeVisPanel = new System.Windows.Forms.Panel();
            this.deviationTB = new System.Windows.Forms.TextBox();
            this.planarBtn = new System.Windows.Forms.Button();
            this.clipYChkBx = new System.Windows.Forms.CheckBox();
            this.meshVisTypeCB = new System.Windows.Forms.ComboBox();
            this.animateBtn = new System.Windows.Forms.Button();
            this.resetBtn = new System.Windows.Forms.Button();
            this.alignRotateLbl = new System.Windows.Forms.Label();
            this.alignOffsetLbl = new System.Windows.Forms.Label();
            this.meshLV = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.matchesLbl = new System.Windows.Forms.Label();
            this.curLodLbl = new System.Windows.Forms.Label();
            this.alignTransformCB = new System.Windows.Forms.CheckBox();
            this.meshRotateLbl = new System.Windows.Forms.Label();
            this.resetAlignBtn = new System.Windows.Forms.Button();
            this.stepAlign = new System.Windows.Forms.Button();
            this.meshOffsetLbl = new System.Windows.Forms.Label();
            this.meshNameLbl = new System.Windows.Forms.Label();
            this.meshAlignLbl = new System.Windows.Forms.Label();
            this.dataPanel = new System.Windows.Forms.Panel();
            this.meshDepthOffsetY = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.timelinePanel.SuspendLayout();
            this.panel1.SuspendLayout();
            this.recordingPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.frameSlider)).BeginInit();
            this.ptMeshPanel.SuspendLayout();
            this.visSettingsPanel.SuspendLayout();
            this.fullMeshPanel.SuspendLayout();
            this.ptcldVisPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pointScaleTB)).BeginInit();
            this.planeVisPanel.SuspendLayout();
            this.dataPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            label1.Location = new System.Drawing.Point(0, 10);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(129, 20);
            label1.TabIndex = 3;
            label1.Text = "Visible Layers";
            // 
            // dataLbl
            // 
            this.dataLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.dataLbl.AutoSize = true;
            this.dataLbl.Location = new System.Drawing.Point(24, 270);
            this.dataLbl.Name = "dataLbl";
            this.dataLbl.Size = new System.Drawing.Size(68, 20);
            this.dataLbl.TabIndex = 1;
            this.dataLbl.Text = "No Data";
            // 
            // dataProgress
            // 
            this.dataProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.dataProgress.Location = new System.Drawing.Point(310, 270);
            this.dataProgress.Name = "dataProgress";
            this.dataProgress.Size = new System.Drawing.Size(1570, 32);
            this.dataProgress.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.timelinePanel);
            this.splitContainer1.Panel2.Controls.Add(this.dataPanel);
            this.splitContainer1.Size = new System.Drawing.Size(2297, 1058);
            this.splitContainer1.SplitterDistance = 730;
            this.splitContainer1.TabIndex = 4;
            // 
            // timelinePanel
            // 
            this.timelinePanel.Controls.Add(this.alignStepLbl);
            this.timelinePanel.Controls.Add(this.currentFrmLbl);
            this.timelinePanel.Controls.Add(this.loadMeshBtn);
            this.timelinePanel.Controls.Add(this.loadRcdBtn);
            this.timelinePanel.Controls.Add(this.frameProgress);
            this.timelinePanel.Controls.Add(this.panel1);
            this.timelinePanel.Controls.Add(this.ipAddressLabel);
            this.timelinePanel.Controls.Add(this.recordingPanel);
            this.timelinePanel.Controls.Add(this.ptMeshPanel);
            this.timelinePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timelinePanel.Location = new System.Drawing.Point(0, 0);
            this.timelinePanel.Name = "timelinePanel";
            this.timelinePanel.Size = new System.Drawing.Size(2297, 324);
            this.timelinePanel.TabIndex = 3;
            // 
            // alignStepLbl
            // 
            this.alignStepLbl.AutoSize = true;
            this.alignStepLbl.Location = new System.Drawing.Point(740, 204);
            this.alignStepLbl.Name = "alignStepLbl";
            this.alignStepLbl.Size = new System.Drawing.Size(0, 20);
            this.alignStepLbl.TabIndex = 23;
            // 
            // currentFrmLbl
            // 
            this.currentFrmLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.currentFrmLbl.AutoSize = true;
            this.currentFrmLbl.Location = new System.Drawing.Point(2110, 124);
            this.currentFrmLbl.Name = "currentFrmLbl";
            this.currentFrmLbl.Size = new System.Drawing.Size(0, 20);
            this.currentFrmLbl.TabIndex = 17;
            // 
            // loadMeshBtn
            // 
            this.loadMeshBtn.Location = new System.Drawing.Point(173, 204);
            this.loadMeshBtn.Name = "loadMeshBtn";
            this.loadMeshBtn.Size = new System.Drawing.Size(140, 36);
            this.loadMeshBtn.TabIndex = 16;
            this.loadMeshBtn.Text = "Load PtMesh";
            this.loadMeshBtn.UseVisualStyleBackColor = true;
            this.loadMeshBtn.Click += new System.EventHandler(this.LoadMeshBtn_Click);
            // 
            // loadRcdBtn
            // 
            this.loadRcdBtn.Location = new System.Drawing.Point(16, 204);
            this.loadRcdBtn.Name = "loadRcdBtn";
            this.loadRcdBtn.Size = new System.Drawing.Size(140, 36);
            this.loadRcdBtn.TabIndex = 15;
            this.loadRcdBtn.Text = "Load Recording";
            this.loadRcdBtn.UseVisualStyleBackColor = true;
            this.loadRcdBtn.Click += new System.EventHandler(this.LoadRcdBtn_Click);
            // 
            // frameProgress
            // 
            this.frameProgress.Location = new System.Drawing.Point(1729, 288);
            this.frameProgress.Name = "frameProgress";
            this.frameProgress.Size = new System.Drawing.Size(352, 19);
            this.frameProgress.TabIndex = 13;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(label1);
            this.panel1.Controls.Add(this.facePtCloudCB);
            this.panel1.Controls.Add(this.depthCB);
            this.panel1.Controls.Add(this.videoCB);
            this.panel1.Controls.Add(this.wholeFaceCB);
            this.panel1.Location = new System.Drawing.Point(5, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(320, 175);
            this.panel1.TabIndex = 12;
            // 
            // facePtCloudCB
            // 
            this.facePtCloudCB.AutoSize = true;
            this.facePtCloudCB.Location = new System.Drawing.Point(19, 105);
            this.facePtCloudCB.Name = "facePtCloudCB";
            this.facePtCloudCB.Size = new System.Drawing.Size(135, 24);
            this.facePtCloudCB.TabIndex = 2;
            this.facePtCloudCB.Text = "Face Pt Cloud";
            this.facePtCloudCB.UseVisualStyleBackColor = true;
            // 
            // depthCB
            // 
            this.depthCB.AutoSize = true;
            this.depthCB.Location = new System.Drawing.Point(19, 72);
            this.depthCB.Name = "depthCB";
            this.depthCB.Size = new System.Drawing.Size(79, 24);
            this.depthCB.TabIndex = 1;
            this.depthCB.Text = "Depth";
            this.depthCB.UseVisualStyleBackColor = true;
            // 
            // videoCB
            // 
            this.videoCB.AutoSize = true;
            this.videoCB.Location = new System.Drawing.Point(19, 38);
            this.videoCB.Name = "videoCB";
            this.videoCB.Size = new System.Drawing.Size(76, 24);
            this.videoCB.TabIndex = 0;
            this.videoCB.Text = "Video";
            this.videoCB.UseVisualStyleBackColor = true;
            // 
            // wholeFaceCB
            // 
            this.wholeFaceCB.AutoSize = true;
            this.wholeFaceCB.Location = new System.Drawing.Point(19, 138);
            this.wholeFaceCB.Name = "wholeFaceCB";
            this.wholeFaceCB.Size = new System.Drawing.Size(147, 24);
            this.wholeFaceCB.TabIndex = 2;
            this.wholeFaceCB.Text = "Combined Face";
            this.wholeFaceCB.UseVisualStyleBackColor = true;
            // 
            // ipAddressLabel
            // 
            this.ipAddressLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ipAddressLabel.AutoSize = true;
            this.ipAddressLabel.Location = new System.Drawing.Point(19, 276);
            this.ipAddressLabel.Name = "ipAddressLabel";
            this.ipAddressLabel.Size = new System.Drawing.Size(57, 20);
            this.ipAddressLabel.TabIndex = 7;
            this.ipAddressLabel.Text = "0.0.0.0";
            // 
            // recordingPanel
            // 
            this.recordingPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.recordingPanel.Controls.Add(this.meshDepthOffsetY);
            this.recordingPanel.Controls.Add(this.alignEndFrmTB);
            this.recordingPanel.Controls.Add(this.alignStepFrmTB);
            this.recordingPanel.Controls.Add(this.alignStartFrmTB);
            this.recordingPanel.Controls.Add(this.alignMergeBtn);
            this.recordingPanel.Controls.Add(this.exportAllBtn);
            this.recordingPanel.Controls.Add(this.exportPtMesh);
            this.recordingPanel.Controls.Add(this.frameSlider);
            this.recordingPanel.Controls.Add(this.frameLabel);
            this.recordingPanel.Controls.Add(this.timeLabel);
            this.recordingPanel.Location = new System.Drawing.Point(331, 4);
            this.recordingPanel.Name = "recordingPanel";
            this.recordingPanel.Size = new System.Drawing.Size(1963, 170);
            this.recordingPanel.TabIndex = 19;
            // 
            // alignEndFrmTB
            // 
            this.alignEndFrmTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.alignEndFrmTB.Location = new System.Drawing.Point(506, 117);
            this.alignEndFrmTB.Name = "alignEndFrmTB";
            this.alignEndFrmTB.Size = new System.Drawing.Size(72, 19);
            this.alignEndFrmTB.TabIndex = 23;
            // 
            // alignStepFrmTB
            // 
            this.alignStepFrmTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.alignStepFrmTB.Location = new System.Drawing.Point(506, 139);
            this.alignStepFrmTB.Name = "alignStepFrmTB";
            this.alignStepFrmTB.Size = new System.Drawing.Size(72, 19);
            this.alignStepFrmTB.TabIndex = 22;
            // 
            // alignStartFrmTB
            // 
            this.alignStartFrmTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.alignStartFrmTB.Location = new System.Drawing.Point(506, 95);
            this.alignStartFrmTB.Name = "alignStartFrmTB";
            this.alignStartFrmTB.Size = new System.Drawing.Size(72, 19);
            this.alignStartFrmTB.TabIndex = 21;
            // 
            // alignMergeBtn
            // 
            this.alignMergeBtn.Enabled = false;
            this.alignMergeBtn.Location = new System.Drawing.Point(347, 106);
            this.alignMergeBtn.Name = "alignMergeBtn";
            this.alignMergeBtn.Size = new System.Drawing.Size(124, 48);
            this.alignMergeBtn.TabIndex = 20;
            this.alignMergeBtn.Text = "Align And Merge";
            this.alignMergeBtn.UseVisualStyleBackColor = true;
            this.alignMergeBtn.Click += new System.EventHandler(this.AlignMergeBtn_Click);
            // 
            // exportAllBtn
            // 
            this.exportAllBtn.Location = new System.Drawing.Point(185, 106);
            this.exportAllBtn.Name = "exportAllBtn";
            this.exportAllBtn.Size = new System.Drawing.Size(124, 48);
            this.exportAllBtn.TabIndex = 19;
            this.exportAllBtn.Text = "Export All";
            this.exportAllBtn.UseVisualStyleBackColor = true;
            this.exportAllBtn.Click += new System.EventHandler(this.ExportAllBtn_Click);
            // 
            // exportPtMesh
            // 
            this.exportPtMesh.Location = new System.Drawing.Point(29, 106);
            this.exportPtMesh.Name = "exportPtMesh";
            this.exportPtMesh.Size = new System.Drawing.Size(124, 48);
            this.exportPtMesh.TabIndex = 18;
            this.exportPtMesh.Text = "Export PtMesh";
            this.exportPtMesh.UseVisualStyleBackColor = true;
            this.exportPtMesh.Click += new System.EventHandler(this.ExportPtMesh_Click);
            // 
            // frameSlider
            // 
            this.frameSlider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.frameSlider.Location = new System.Drawing.Point(29, 31);
            this.frameSlider.Name = "frameSlider";
            this.frameSlider.Size = new System.Drawing.Size(1473, 69);
            this.frameSlider.TabIndex = 4;
            this.frameSlider.Scroll += new System.EventHandler(this.frameSlider_Scroll);
            // 
            // frameLabel
            // 
            this.frameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.frameLabel.AutoSize = true;
            this.frameLabel.Location = new System.Drawing.Point(1886, 10);
            this.frameLabel.Name = "frameLabel";
            this.frameLabel.Size = new System.Drawing.Size(51, 20);
            this.frameLabel.TabIndex = 5;
            this.frameLabel.Text = "label1";
            // 
            // timeLabel
            // 
            this.timeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.timeLabel.AutoSize = true;
            this.timeLabel.Location = new System.Drawing.Point(1886, 53);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(31, 20);
            this.timeLabel.TabIndex = 6;
            this.timeLabel.Text = "0.0";
            // 
            // ptMeshPanel
            // 
            this.ptMeshPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ptMeshPanel.Controls.Add(this.alignAllMeshesBtn);
            this.ptMeshPanel.Controls.Add(this.selectRangeCB);
            this.ptMeshPanel.Controls.Add(this.meshModeCB);
            this.ptMeshPanel.Controls.Add(this.meshvzlbl);
            this.ptMeshPanel.Controls.Add(this.visSettingsPanel);
            this.ptMeshPanel.Controls.Add(this.meshVisTypeCB);
            this.ptMeshPanel.Controls.Add(this.animateBtn);
            this.ptMeshPanel.Controls.Add(this.resetBtn);
            this.ptMeshPanel.Controls.Add(this.alignRotateLbl);
            this.ptMeshPanel.Controls.Add(this.alignOffsetLbl);
            this.ptMeshPanel.Controls.Add(this.meshLV);
            this.ptMeshPanel.Controls.Add(this.matchesLbl);
            this.ptMeshPanel.Controls.Add(this.curLodLbl);
            this.ptMeshPanel.Controls.Add(this.alignTransformCB);
            this.ptMeshPanel.Controls.Add(this.meshRotateLbl);
            this.ptMeshPanel.Controls.Add(this.resetAlignBtn);
            this.ptMeshPanel.Controls.Add(this.stepAlign);
            this.ptMeshPanel.Controls.Add(this.meshOffsetLbl);
            this.ptMeshPanel.Controls.Add(this.meshNameLbl);
            this.ptMeshPanel.Controls.Add(this.meshAlignLbl);
            this.ptMeshPanel.Location = new System.Drawing.Point(331, 4);
            this.ptMeshPanel.Name = "ptMeshPanel";
            this.ptMeshPanel.Size = new System.Drawing.Size(1963, 317);
            this.ptMeshPanel.TabIndex = 20;
            // 
            // alignAllMeshesBtn
            // 
            this.alignAllMeshesBtn.Location = new System.Drawing.Point(1389, 183);
            this.alignAllMeshesBtn.Name = "alignAllMeshesBtn";
            this.alignAllMeshesBtn.Size = new System.Drawing.Size(124, 48);
            this.alignAllMeshesBtn.TabIndex = 25;
            this.alignAllMeshesBtn.Text = "Align All Meshes";
            this.alignAllMeshesBtn.UseVisualStyleBackColor = true;
            this.alignAllMeshesBtn.Click += new System.EventHandler(this.AlignAllMeshesBtn_Click);
            // 
            // selectRangeCB
            // 
            this.selectRangeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.selectRangeCB.FormattingEnabled = true;
            this.selectRangeCB.Location = new System.Drawing.Point(202, 31);
            this.selectRangeCB.Name = "selectRangeCB";
            this.selectRangeCB.Size = new System.Drawing.Size(81, 28);
            this.selectRangeCB.TabIndex = 40;
            this.selectRangeCB.SelectedIndexChanged += new System.EventHandler(this.SelectRangeCB_SelectedIndexChanged);
            // 
            // meshModeCB
            // 
            this.meshModeCB.FormattingEnabled = true;
            this.meshModeCB.Location = new System.Drawing.Point(29, 31);
            this.meshModeCB.Name = "meshModeCB";
            this.meshModeCB.Size = new System.Drawing.Size(167, 28);
            this.meshModeCB.TabIndex = 39;
            // 
            // meshvzlbl
            // 
            this.meshvzlbl.AutoSize = true;
            this.meshvzlbl.Location = new System.Drawing.Point(648, 10);
            this.meshvzlbl.Name = "meshvzlbl";
            this.meshvzlbl.Size = new System.Drawing.Size(136, 20);
            this.meshvzlbl.TabIndex = 38;
            this.meshvzlbl.Text = "Visualization Type";
            // 
            // visSettingsPanel
            // 
            this.visSettingsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.visSettingsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.visSettingsPanel.Controls.Add(this.fullMeshPanel);
            this.visSettingsPanel.Controls.Add(this.ptcldVisPanel);
            this.visSettingsPanel.Controls.Add(this.planeVisPanel);
            this.visSettingsPanel.Location = new System.Drawing.Point(856, 0);
            this.visSettingsPanel.Name = "visSettingsPanel";
            this.visSettingsPanel.Size = new System.Drawing.Size(500, 317);
            this.visSettingsPanel.TabIndex = 36;
            // 
            // fullMeshPanel
            // 
            this.fullMeshPanel.Controls.Add(this.label4);
            this.fullMeshPanel.Controls.Add(this.detailCB);
            this.fullMeshPanel.Location = new System.Drawing.Point(20, 11);
            this.fullMeshPanel.Name = "fullMeshPanel";
            this.fullMeshPanel.Size = new System.Drawing.Size(407, 142);
            this.fullMeshPanel.TabIndex = 37;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(133, 20);
            this.label4.TabIndex = 1;
            this.label4.Text = "Detail (#triangles)";
            // 
            // detailCB
            // 
            this.detailCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.detailCB.FormattingEnabled = true;
            this.detailCB.Location = new System.Drawing.Point(14, 50);
            this.detailCB.Name = "detailCB";
            this.detailCB.Size = new System.Drawing.Size(121, 28);
            this.detailCB.TabIndex = 0;
            this.detailCB.SelectedIndexChanged += new System.EventHandler(this.DetailCB_SelectedIndexChanged);
            // 
            // ptcldVisPanel
            // 
            this.ptcldVisPanel.Controls.Add(this.label3);
            this.ptcldVisPanel.Controls.Add(this.pointScaleTB);
            this.ptcldVisPanel.Location = new System.Drawing.Point(31, 20);
            this.ptcldVisPanel.Name = "ptcldVisPanel";
            this.ptcldVisPanel.Size = new System.Drawing.Size(359, 113);
            this.ptcldVisPanel.TabIndex = 36;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 20);
            this.label3.TabIndex = 1;
            this.label3.Text = "Point Scale";
            // 
            // pointScaleTB
            // 
            this.pointScaleTB.Location = new System.Drawing.Point(3, 44);
            this.pointScaleTB.Maximum = 5;
            this.pointScaleTB.Minimum = -5;
            this.pointScaleTB.Name = "pointScaleTB";
            this.pointScaleTB.Size = new System.Drawing.Size(127, 69);
            this.pointScaleTB.TabIndex = 0;
            this.pointScaleTB.Scroll += new System.EventHandler(this.PointScaleTB_Scroll);
            // 
            // planeVisPanel
            // 
            this.planeVisPanel.Controls.Add(this.deviationTB);
            this.planeVisPanel.Controls.Add(this.planarBtn);
            this.planeVisPanel.Controls.Add(this.clipYChkBx);
            this.planeVisPanel.Location = new System.Drawing.Point(276, 63);
            this.planeVisPanel.Name = "planeVisPanel";
            this.planeVisPanel.Size = new System.Drawing.Size(200, 100);
            this.planeVisPanel.TabIndex = 35;
            // 
            // deviationTB
            // 
            this.deviationTB.Location = new System.Drawing.Point(30, 20);
            this.deviationTB.Name = "deviationTB";
            this.deviationTB.Size = new System.Drawing.Size(121, 26);
            this.deviationTB.TabIndex = 32;
            // 
            // planarBtn
            // 
            this.planarBtn.Location = new System.Drawing.Point(173, 19);
            this.planarBtn.Name = "planarBtn";
            this.planarBtn.Size = new System.Drawing.Size(75, 28);
            this.planarBtn.TabIndex = 31;
            this.planarBtn.Text = "Planar";
            this.planarBtn.UseVisualStyleBackColor = true;
            this.planarBtn.Click += new System.EventHandler(this.PlanarBtn_Click);
            // 
            // clipYChkBx
            // 
            this.clipYChkBx.AutoSize = true;
            this.clipYChkBx.Location = new System.Drawing.Point(30, 61);
            this.clipYChkBx.Name = "clipYChkBx";
            this.clipYChkBx.Size = new System.Drawing.Size(138, 24);
            this.clipYChkBx.TabIndex = 34;
            this.clipYChkBx.Text = "Clip Y Normals";
            this.clipYChkBx.UseVisualStyleBackColor = true;
            this.clipYChkBx.CheckedChanged += new System.EventHandler(this.ClipYChkBx_CheckedChanged);
            // 
            // meshVisTypeCB
            // 
            this.meshVisTypeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.meshVisTypeCB.FormattingEnabled = true;
            this.meshVisTypeCB.Location = new System.Drawing.Point(652, 50);
            this.meshVisTypeCB.Name = "meshVisTypeCB";
            this.meshVisTypeCB.Size = new System.Drawing.Size(172, 28);
            this.meshVisTypeCB.TabIndex = 35;
            this.meshVisTypeCB.SelectedIndexChanged += new System.EventHandler(this.MeshVisTypeCB_SelectedIndexChanged);
            // 
            // animateBtn
            // 
            this.animateBtn.Location = new System.Drawing.Point(436, 120);
            this.animateBtn.Name = "animateBtn";
            this.animateBtn.Size = new System.Drawing.Size(110, 34);
            this.animateBtn.TabIndex = 33;
            this.animateBtn.Text = "Animate";
            this.animateBtn.UseVisualStyleBackColor = true;
            this.animateBtn.Click += new System.EventHandler(this.AnimateBtn_Click);
            // 
            // resetBtn
            // 
            this.resetBtn.Location = new System.Drawing.Point(309, 120);
            this.resetBtn.Name = "resetBtn";
            this.resetBtn.Size = new System.Drawing.Size(75, 34);
            this.resetBtn.TabIndex = 30;
            this.resetBtn.Text = "Reset";
            this.resetBtn.UseVisualStyleBackColor = true;
            this.resetBtn.Click += new System.EventHandler(this.ResetBtn_Click);
            // 
            // alignRotateLbl
            // 
            this.alignRotateLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.alignRotateLbl.AutoSize = true;
            this.alignRotateLbl.Location = new System.Drawing.Point(1843, 70);
            this.alignRotateLbl.Name = "alignRotateLbl";
            this.alignRotateLbl.Size = new System.Drawing.Size(91, 20);
            this.alignRotateLbl.TabIndex = 29;
            this.alignRotateLbl.Text = "alignRotate";
            // 
            // alignOffsetLbl
            // 
            this.alignOffsetLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.alignOffsetLbl.AutoSize = true;
            this.alignOffsetLbl.Location = new System.Drawing.Point(1843, 31);
            this.alignOffsetLbl.Name = "alignOffsetLbl";
            this.alignOffsetLbl.Size = new System.Drawing.Size(86, 20);
            this.alignOffsetLbl.TabIndex = 28;
            this.alignOffsetLbl.Text = "alignOffset";
            // 
            // meshLV
            // 
            this.meshLV.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.meshLV.FullRowSelect = true;
            this.meshLV.HideSelection = false;
            this.meshLV.Location = new System.Drawing.Point(29, 79);
            this.meshLV.Name = "meshLV";
            this.meshLV.Size = new System.Drawing.Size(254, 152);
            this.meshLV.TabIndex = 27;
            this.meshLV.UseCompatibleStateImageBehavior = false;
            this.meshLV.View = System.Windows.Forms.View.Details;
            this.meshLV.VirtualMode = true;
            this.meshLV.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.MeshLV_RetrieveVirtualItem);
            this.meshLV.SelectedIndexChanged += new System.EventHandler(this.MeshLV_SelectedIndexChanged);
            this.meshLV.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MeshLV_MouseDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "";
            this.columnHeader1.Width = 30;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Name";
            this.columnHeader2.Width = 220;
            // 
            // matchesLbl
            // 
            this.matchesLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.matchesLbl.AutoSize = true;
            this.matchesLbl.Location = new System.Drawing.Point(1550, 79);
            this.matchesLbl.Name = "matchesLbl";
            this.matchesLbl.Size = new System.Drawing.Size(74, 20);
            this.matchesLbl.TabIndex = 26;
            this.matchesLbl.Text = "matches:";
            // 
            // curLodLbl
            // 
            this.curLodLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.curLodLbl.AutoSize = true;
            this.curLodLbl.Location = new System.Drawing.Point(1550, 114);
            this.curLodLbl.Name = "curLodLbl";
            this.curLodLbl.Size = new System.Drawing.Size(34, 20);
            this.curLodLbl.TabIndex = 25;
            this.curLodLbl.Text = "lod:";
            // 
            // alignTransformCB
            // 
            this.alignTransformCB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.alignTransformCB.AutoSize = true;
            this.alignTransformCB.Location = new System.Drawing.Point(1551, 31);
            this.alignTransformCB.Name = "alignTransformCB";
            this.alignTransformCB.Size = new System.Drawing.Size(190, 24);
            this.alignTransformCB.TabIndex = 24;
            this.alignTransformCB.Text = "Show Align Transform";
            this.alignTransformCB.UseVisualStyleBackColor = true;
            this.alignTransformCB.CheckedChanged += new System.EventHandler(this.AlignTransformCB_CheckedChanged);
            // 
            // meshRotateLbl
            // 
            this.meshRotateLbl.AutoSize = true;
            this.meshRotateLbl.Location = new System.Drawing.Point(305, 92);
            this.meshRotateLbl.Name = "meshRotateLbl";
            this.meshRotateLbl.Size = new System.Drawing.Size(97, 20);
            this.meshRotateLbl.TabIndex = 4;
            this.meshRotateLbl.Text = "meshRotate";
            // 
            // resetAlignBtn
            // 
            this.resetAlignBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resetAlignBtn.Location = new System.Drawing.Point(1389, 94);
            this.resetAlignBtn.Name = "resetAlignBtn";
            this.resetAlignBtn.Size = new System.Drawing.Size(124, 48);
            this.resetAlignBtn.TabIndex = 22;
            this.resetAlignBtn.Text = "Reset Align";
            this.resetAlignBtn.UseVisualStyleBackColor = true;
            this.resetAlignBtn.Click += new System.EventHandler(this.ResetAlignBtn_Click);
            // 
            // stepAlign
            // 
            this.stepAlign.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.stepAlign.Location = new System.Drawing.Point(1389, 31);
            this.stepAlign.Name = "stepAlign";
            this.stepAlign.Size = new System.Drawing.Size(124, 48);
            this.stepAlign.TabIndex = 21;
            this.stepAlign.Text = "Step Align";
            this.stepAlign.UseVisualStyleBackColor = true;
            this.stepAlign.Click += new System.EventHandler(this.StepAlign_Click);
            // 
            // meshOffsetLbl
            // 
            this.meshOffsetLbl.AutoSize = true;
            this.meshOffsetLbl.Location = new System.Drawing.Point(305, 53);
            this.meshOffsetLbl.Name = "meshOffsetLbl";
            this.meshOffsetLbl.Size = new System.Drawing.Size(92, 20);
            this.meshOffsetLbl.TabIndex = 3;
            this.meshOffsetLbl.Text = "meshOffset";
            this.meshOffsetLbl.Click += new System.EventHandler(this.Label3_Click);
            // 
            // meshNameLbl
            // 
            this.meshNameLbl.AutoSize = true;
            this.meshNameLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.meshNameLbl.Location = new System.Drawing.Point(305, 10);
            this.meshNameLbl.Name = "meshNameLbl";
            this.meshNameLbl.Size = new System.Drawing.Size(122, 20);
            this.meshNameLbl.TabIndex = 2;
            this.meshNameLbl.Text = "meshNameLbl";
            // 
            // meshAlignLbl
            // 
            this.meshAlignLbl.AutoSize = true;
            this.meshAlignLbl.Location = new System.Drawing.Point(511, 10);
            this.meshAlignLbl.Name = "meshAlignLbl";
            this.meshAlignLbl.Size = new System.Drawing.Size(0, 20);
            this.meshAlignLbl.TabIndex = 1;
            // 
            // dataPanel
            // 
            this.dataPanel.Controls.Add(this.dataProgress);
            this.dataPanel.Controls.Add(this.dataLbl);
            this.dataPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataPanel.Location = new System.Drawing.Point(0, 0);
            this.dataPanel.Name = "dataPanel";
            this.dataPanel.Size = new System.Drawing.Size(2297, 324);
            this.dataPanel.TabIndex = 10;
            this.dataPanel.Visible = false;
            // 
            // meshDepthOffsetY
            // 
            this.meshDepthOffsetY.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.meshDepthOffsetY.Location = new System.Drawing.Point(973, 91);
            this.meshDepthOffsetY.Name = "meshDepthOffsetY";
            this.meshDepthOffsetY.Size = new System.Drawing.Size(72, 19);
            this.meshDepthOffsetY.TabIndex = 24;
            // 
            // MainWindow
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(8, 19);
            this.ClientSize = new System.Drawing.Size(2297, 1058);
            this.Controls.Add(this.splitContainer1);
            this.KeyPreview = true;
            this.Name = "MainWindow";
            this.Text = "FaceServer";
            this.Closed += new System.EventHandler(this.MainWindow_Closed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.timelinePanel.ResumeLayout(false);
            this.timelinePanel.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.recordingPanel.ResumeLayout(false);
            this.recordingPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.frameSlider)).EndInit();
            this.ptMeshPanel.ResumeLayout(false);
            this.ptMeshPanel.PerformLayout();
            this.visSettingsPanel.ResumeLayout(false);
            this.fullMeshPanel.ResumeLayout(false);
            this.fullMeshPanel.PerformLayout();
            this.ptcldVisPanel.ResumeLayout(false);
            this.ptcldVisPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pointScaleTB)).EndInit();
            this.planeVisPanel.ResumeLayout(false);
            this.planeVisPanel.PerformLayout();
            this.dataPanel.ResumeLayout(false);
            this.dataPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        private GLView glView;

        #endregion

        private System.Windows.Forms.Label dataLbl;
        private System.Windows.Forms.ProgressBar dataProgress;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label frameLabel;
        private System.Windows.Forms.TrackBar frameSlider;
        private System.Windows.Forms.Label timeLabel;
        private System.Windows.Forms.Label ipAddressLabel;
        private System.Windows.Forms.Panel dataPanel;
        private System.Windows.Forms.Panel timelinePanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox videoCB;
        private System.Windows.Forms.CheckBox facePtCloudCB;
        private System.Windows.Forms.CheckBox depthCB;
        private System.Windows.Forms.CheckBox wholeFaceCB;
        private System.Windows.Forms.ProgressBar frameProgress;
        private System.Windows.Forms.Button loadMeshBtn;
        private System.Windows.Forms.Button loadRcdBtn;
        private System.Windows.Forms.Label currentFrmLbl;
        private System.Windows.Forms.Button exportPtMesh;
        private System.Windows.Forms.Panel recordingPanel;
        private System.Windows.Forms.Panel ptMeshPanel;
        private System.Windows.Forms.Label meshAlignLbl;
        private System.Windows.Forms.Label meshOffsetLbl;
        private System.Windows.Forms.Label meshNameLbl;
        private System.Windows.Forms.Label meshRotateLbl;
        private System.Windows.Forms.Button resetAlignBtn;
        private System.Windows.Forms.Button stepAlign;
        private System.Windows.Forms.Label alignStepLbl;
        private System.Windows.Forms.CheckBox alignTransformCB;
        private System.Windows.Forms.Label curLodLbl;
        private System.Windows.Forms.Label matchesLbl;
        private System.Windows.Forms.Button exportAllBtn;
        private System.Windows.Forms.ListView meshLV;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label alignRotateLbl;
        private System.Windows.Forms.Label alignOffsetLbl;
        private System.Windows.Forms.Button resetBtn;
        private System.Windows.Forms.Button planarBtn;
        private System.Windows.Forms.TextBox deviationTB;
        private System.Windows.Forms.Button animateBtn;
        private System.Windows.Forms.CheckBox clipYChkBx;
        private System.Windows.Forms.Panel visSettingsPanel;
        private System.Windows.Forms.ComboBox meshVisTypeCB;
        private System.Windows.Forms.Label meshvzlbl;
        private System.Windows.Forms.Panel planeVisPanel;
        private System.Windows.Forms.Panel ptcldVisPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar pointScaleTB;
        private System.Windows.Forms.Panel fullMeshPanel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox detailCB;
        private System.Windows.Forms.ComboBox meshModeCB;
        private System.Windows.Forms.ComboBox selectRangeCB;
        private System.Windows.Forms.Button alignAllMeshesBtn;
        private System.Windows.Forms.Button alignMergeBtn;
        private System.Windows.Forms.TextBox alignEndFrmTB;
        private System.Windows.Forms.TextBox alignStepFrmTB;
        private System.Windows.Forms.TextBox alignStartFrmTB;
        private System.Windows.Forms.TextBox meshDepthOffsetY;
    }
}
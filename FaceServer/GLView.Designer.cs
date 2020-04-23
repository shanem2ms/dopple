namespace FaceServer
{
    partial class GLView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.glControl = new OpenTK.GLControl();
            this.cameraTypeCB = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.twodrotLbl = new System.Windows.Forms.Label();
            this.twodtrnLbl = new System.Windows.Forms.Label();
            this.alignScoreTB = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.rotateBtn = new System.Windows.Forms.RadioButton();
            this.translateBtn = new System.Windows.Forms.RadioButton();
            this.selectToolBtn = new System.Windows.Forms.RadioButton();
            this.alignStepBtn = new System.Windows.Forms.Button();
            this.planesMinSizeTB = new System.Windows.Forms.TextBox();
            this.planesThreshTB = new System.Windows.Forms.TextBox();
            this.planeDPMinTB = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // glControl
            // 
            this.glControl.BackColor = System.Drawing.Color.Black;
            this.glControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glControl.Location = new System.Drawing.Point(0, 0);
            this.glControl.Margin = new System.Windows.Forms.Padding(0);
            this.glControl.Name = "glControl";
            this.glControl.Size = new System.Drawing.Size(394, 682);
            this.glControl.TabIndex = 0;
            this.glControl.VSync = false;
            // 
            // cameraTypeCB
            // 
            this.cameraTypeCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cameraTypeCB.FormattingEnabled = true;
            this.cameraTypeCB.Location = new System.Drawing.Point(41, 57);
            this.cameraTypeCB.Name = "cameraTypeCB";
            this.cameraTypeCB.Size = new System.Drawing.Size(260, 28);
            this.cameraTypeCB.TabIndex = 1;
            this.cameraTypeCB.SelectedIndexChanged += new System.EventHandler(this.CameraTypeCB_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Camera";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(41, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Tool";
            // 
            // twodrotLbl
            // 
            this.twodrotLbl.AutoSize = true;
            this.twodrotLbl.Location = new System.Drawing.Point(13, 365);
            this.twodrotLbl.Name = "twodrotLbl";
            this.twodrotLbl.Size = new System.Drawing.Size(18, 20);
            this.twodrotLbl.TabIndex = 6;
            this.twodrotLbl.Text = "d";
            // 
            // twodtrnLbl
            // 
            this.twodtrnLbl.AutoSize = true;
            this.twodtrnLbl.Location = new System.Drawing.Point(13, 318);
            this.twodtrnLbl.Name = "twodtrnLbl";
            this.twodtrnLbl.Size = new System.Drawing.Size(18, 20);
            this.twodtrnLbl.TabIndex = 7;
            this.twodtrnLbl.Text = "d";
            // 
            // alignScoreTB
            // 
            this.alignScoreTB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.alignScoreTB.Location = new System.Drawing.Point(17, 430);
            this.alignScoreTB.Multiline = true;
            this.alignScoreTB.Name = "alignScoreTB";
            this.alignScoreTB.ReadOnly = true;
            this.alignScoreTB.Size = new System.Drawing.Size(1209, 184);
            this.alignScoreTB.TabIndex = 8;
            this.alignScoreTB.WordWrap = false;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.glControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.planeDPMinTB);
            this.splitContainer1.Panel2.Controls.Add(this.planesMinSizeTB);
            this.splitContainer1.Panel2.Controls.Add(this.planesThreshTB);
            this.splitContainer1.Panel2.Controls.Add(this.rotateBtn);
            this.splitContainer1.Panel2.Controls.Add(this.translateBtn);
            this.splitContainer1.Panel2.Controls.Add(this.selectToolBtn);
            this.splitContainer1.Panel2.Controls.Add(this.alignStepBtn);
            this.splitContainer1.Panel2.Controls.Add(this.alignScoreTB);
            this.splitContainer1.Panel2.Controls.Add(this.twodtrnLbl);
            this.splitContainer1.Panel2.Controls.Add(this.cameraTypeCB);
            this.splitContainer1.Panel2.Controls.Add(this.twodrotLbl);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Size = new System.Drawing.Size(1334, 682);
            this.splitContainer1.SplitterDistance = 394;
            this.splitContainer1.TabIndex = 9;
            // 
            // rotateBtn
            // 
            this.rotateBtn.Appearance = System.Windows.Forms.Appearance.Button;
            this.rotateBtn.AutoSize = true;
            this.rotateBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rotateBtn.Location = new System.Drawing.Point(124, 179);
            this.rotateBtn.Name = "rotateBtn";
            this.rotateBtn.Size = new System.Drawing.Size(33, 32);
            this.rotateBtn.TabIndex = 12;
            this.rotateBtn.TabStop = true;
            this.rotateBtn.Text = "R";
            this.rotateBtn.UseVisualStyleBackColor = true;
            this.rotateBtn.CheckedChanged += new System.EventHandler(this.SelectToolBtn_CheckedChanged);
            // 
            // translateBtn
            // 
            this.translateBtn.Appearance = System.Windows.Forms.Appearance.Button;
            this.translateBtn.AutoSize = true;
            this.translateBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.translateBtn.Location = new System.Drawing.Point(76, 179);
            this.translateBtn.Name = "translateBtn";
            this.translateBtn.Size = new System.Drawing.Size(30, 32);
            this.translateBtn.TabIndex = 11;
            this.translateBtn.TabStop = true;
            this.translateBtn.Text = "T";
            this.translateBtn.UseVisualStyleBackColor = true;
            this.translateBtn.CheckedChanged += new System.EventHandler(this.SelectToolBtn_CheckedChanged);
            // 
            // selectToolBtn
            // 
            this.selectToolBtn.Appearance = System.Windows.Forms.Appearance.Button;
            this.selectToolBtn.AutoSize = true;
            this.selectToolBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectToolBtn.Location = new System.Drawing.Point(26, 179);
            this.selectToolBtn.Name = "selectToolBtn";
            this.selectToolBtn.Size = new System.Drawing.Size(32, 32);
            this.selectToolBtn.TabIndex = 10;
            this.selectToolBtn.TabStop = true;
            this.selectToolBtn.Text = "S";
            this.selectToolBtn.UseVisualStyleBackColor = true;
            this.selectToolBtn.CheckedChanged += new System.EventHandler(this.SelectToolBtn_CheckedChanged);
            // 
            // alignStepBtn
            // 
            this.alignStepBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.alignStepBtn.Location = new System.Drawing.Point(17, 629);
            this.alignStepBtn.Name = "alignStepBtn";
            this.alignStepBtn.Size = new System.Drawing.Size(149, 40);
            this.alignStepBtn.TabIndex = 9;
            this.alignStepBtn.Text = "Align Step";
            this.alignStepBtn.UseVisualStyleBackColor = true;
            this.alignStepBtn.Click += new System.EventHandler(this.AlignStepBtn_Click);
            // 
            // planesMinSizeTB
            // 
            this.planesMinSizeTB.Location = new System.Drawing.Point(425, 59);
            this.planesMinSizeTB.Name = "planesMinSizeTB";
            this.planesMinSizeTB.Size = new System.Drawing.Size(100, 26);
            this.planesMinSizeTB.TabIndex = 44;
            this.planesMinSizeTB.Leave += new System.EventHandler(this.planesMinSizeTB_Leave);
            // 
            // planesThreshTB
            // 
            this.planesThreshTB.Location = new System.Drawing.Point(425, 109);
            this.planesThreshTB.Name = "planesThreshTB";
            this.planesThreshTB.Size = new System.Drawing.Size(100, 26);
            this.planesThreshTB.TabIndex = 43;
            this.planesThreshTB.Leave += new System.EventHandler(this.planesThreshTB_Leave);
            // 
            // planeDPMinTB
            // 
            this.planeDPMinTB.Location = new System.Drawing.Point(425, 179);
            this.planeDPMinTB.Name = "planeDPMinTB";
            this.planeDPMinTB.Size = new System.Drawing.Size(100, 26);
            this.planeDPMinTB.TabIndex = 45;
            this.planeDPMinTB.Leave += new System.EventHandler(this.planeDPMinTB_Leave);
            // 
            // GLView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "GLView";
            this.Size = new System.Drawing.Size(1334, 682);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenTK.GLControl glControl;
        private System.Windows.Forms.ComboBox cameraTypeCB;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label twodrotLbl;
        private System.Windows.Forms.Label twodtrnLbl;
        private System.Windows.Forms.TextBox alignScoreTB;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button alignStepBtn;
        private System.Windows.Forms.RadioButton rotateBtn;
        private System.Windows.Forms.RadioButton translateBtn;
        private System.Windows.Forms.RadioButton selectToolBtn;
        private System.Windows.Forms.TextBox planesMinSizeTB;
        private System.Windows.Forms.TextBox planesThreshTB;
        private System.Windows.Forms.TextBox planeDPMinTB;
    }
}

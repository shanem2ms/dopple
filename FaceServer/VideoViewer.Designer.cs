namespace TcpServerDemo
{
    partial class VideoViewer
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
            this.RenderControl = new OpenGL.GlControl();
            this.SuspendLayout();
            // 
            // RenderControl
            // 
            this.RenderControl.AnimationTimer = false;
            this.RenderControl.BackColor = System.Drawing.Color.DimGray;
            this.RenderControl.ColorBits = ((uint)(24u));
            this.RenderControl.DepthBits = ((uint)(8u));
            this.RenderControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RenderControl.Location = new System.Drawing.Point(0, 0);
            this.RenderControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.RenderControl.MultisampleBits = ((uint)(0u));
            this.RenderControl.Name = "RenderControl";
            this.RenderControl.Size = new System.Drawing.Size(1128, 935);
            this.RenderControl.StencilBits = ((uint)(0u));
            this.RenderControl.TabIndex = 0;
            this.RenderControl.ContextCreated += new System.EventHandler<OpenGL.GlControlEventArgs>(this.RenderControl_ContextCreated);
            this.RenderControl.ContextDestroying += new System.EventHandler<OpenGL.GlControlEventArgs>(this.RenderControl_ContextDestroying);
            this.RenderControl.Render += new System.EventHandler<OpenGL.GlControlEventArgs>(this.RenderControl_Render);
            this.RenderControl.ContextUpdate += new System.EventHandler<OpenGL.GlControlEventArgs>(this.RenderControl_ContextUpdate);
            // 
            // VideoViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.RenderControl);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "VideoViewer";
            this.Size = new System.Drawing.Size(1128, 935);
            this.ResumeLayout(false);

        }

        #endregion

        private OpenGL.GlControl RenderControl;
    }
}

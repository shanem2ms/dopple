using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using TcpLib;

namespace TcpServerDemo
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainForm: System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.btnClose = new System.Windows.Forms.Button();
            this.videoViewer1 = new TcpServerDemo.VideoViewer();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(641, 580);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // videoViewer1
            // 
            this.videoViewer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.videoViewer1.Location = new System.Drawing.Point(12, 12);
            this.videoViewer1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.videoViewer1.Name = "videoViewer1";
            this.videoViewer1.Size = new System.Drawing.Size(678, 546);
            this.videoViewer1.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(726, 610);
            this.Controls.Add(this.videoViewer1);
            this.Controls.Add(this.btnClose);
            this.Name = "MainForm";
            this.Text = "TcpServerDemo";
            this.Closed += new System.EventHandler(this.MainForm_Closed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new MainForm());
		}


		private TcpServer Server;
		private System.Windows.Forms.Button btnClose;
        private VideoViewer videoViewer1;
        private FaceMeshService Provider;
        UDPer.UDPer udp;

        private void MainForm_Load(object sender, System.EventArgs e)
		{
			Provider = new FaceMeshService();
            Provider.OnNewVideoFrame += Provider_OnNewVideoFrame;
            Provider.OnNewARFrame += Provider_OnNewARFrame;
            Server = new TcpServer(Provider, 15555);
			Server.Start();
            udp = new UDPer.UDPer();
            udp.Start();
            udp.Send("Face Server");
            System.IO.FileStream fs = new System.IO.FileStream(@"d:\homep4\dopple\data\tmpface.frm", System.IO.FileMode.Open,
                 System.IO.FileAccess.Read);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();
            //VideoFrame vf = VideoFrame.FromBytes(data);
            //videoViewer1.SetCurrentFrame(vf);
        }

        private void Provider_OnNewARFrame(object sender, OnNewARFrameArgs e)
        {
            videoViewer1.SetCurrentARFrame(e.vf);
            //throw new NotImplementedException();
        }

        private void Provider_OnNewVideoFrame(object sender, OnNewVideoFrameArgs e)
        {
            //System.IO.FileStream fs = new System.IO.FileStream(@"c:\tmpface.frm", System.IO.FileMode.Create);
            //e.vf.WriteToDisk(fs);
            //fs.Close();
            videoViewer1.SetCurrentVideoFrame(e.vf);
        }

        private void btnClose_Click(object sender, System.EventArgs e)
		{
			this.Close();
		}

		private void MainForm_Closed(object sender, System.EventArgs e)
		{
            Server.Stop();
            udp.Stop();
		}
    }
}

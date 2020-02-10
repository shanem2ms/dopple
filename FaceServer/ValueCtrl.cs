using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FaceServer
{
    public partial class ValueCtrl : UserControl
    {

        bool needReload = false;
        int numFields = 2;
        public int NumFields { get { return this.numFields; } set { this.numFields = value; needReload = true; } }
        TrackBar[] trackbars;
        TextBox[] textBox;
        string paramName = "";
        public string ParamName { get { return this.paramName; } set { this.paramName = value; needReload = true; } }
        public float[] Values { get; set; }
        public float[] Minimum { get; set; }
        public float[] Maximum { get; set; }
        public ValueCtrl()
        {
            InitializeComponent();
            Reload();            
        }        

        public event EventHandler<OnNewValueArgs> OnNewValue;

        void Reload()
        {
            if (Values == null || Values.Length != NumFields)
            {
                Values = new float[NumFields];
                for (int i = 0; i < NumFields; ++i) Values[i] = 0;
            }

            if (Minimum == null || Minimum.Length != NumFields)
            {
                Minimum = new float[NumFields];
                for (int i = 0; i < NumFields; ++i) Minimum[i] = 0;
            }

            if (Maximum == null || Maximum.Length != NumFields)
            { 
                Maximum = new float[NumFields];
                for (int i = 0; i < NumFields; ++i) Maximum[i] = 1;
            }

            string[] lbls = { "X", "Y", "Z" };
            Color[] cols = { Color.Red, Color.Blue, Color.Yellow };
            this.Controls.Clear();
            trackbars = new TrackBar[NumFields];
            textBox = new TextBox[NumFields];
            int yOffset = 0;
            int rowSize = 25;
            int editWidth = 50;
            if (ParamName.Length > 0)
            {
                Label lbl = new Label();
                lbl.Text = ParamName;
                lbl.Location = new Point(20, 0);
                lbl.Height = 25;
                lbl.Width = this.Width - 20;
                lbl.Anchor = AnchorStyles.Top |
                    AnchorStyles.Left |
                    AnchorStyles.Right;
                this.Controls.Add(lbl);
                yOffset += 25;
            }
            for (int idx = 0; idx < NumFields; ++idx)
            {
                Label labl = new Label();
                labl.Text = lbls[idx];
                labl.Location = new Point(10, yOffset + idx * rowSize + 5);
                labl.Width = 20;
                trackbars[idx] = new TrackBar();
                trackbars[idx].Location = new Point(30, yOffset + idx * rowSize);
                trackbars[idx].TickStyle = TickStyle.None;
                trackbars[idx].AutoSize = false;
                trackbars[idx].Height = rowSize;
                trackbars[idx].Width = this.Width - editWidth - 30;
                trackbars[idx].BackColor = cols[idx];
                trackbars[idx].Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                trackbars[idx].Scroll += ValueCtrl_Scroll;
                trackbars[idx].Maximum = 1000;
                trackbars[idx].TickFrequency = 1;
                trackbars[idx].Tag = idx;
                textBox[idx] = new TextBox();
                textBox[idx].BorderStyle = BorderStyle.FixedSingle;
                textBox[idx].Height = rowSize;
                textBox[idx].Width = editWidth;
                textBox[idx].Location = new Point(this.Width - editWidth, yOffset + idx * rowSize);
                textBox[idx].Anchor = AnchorStyles.Top | AnchorStyles.Right;
                textBox[idx].TextChanged += ValueCtrl_TextChanged;
                textBox[idx].Leave += ValueCtrl_Leave;
                textBox[idx].Tag = idx;
                textBox[idx].Text = Values[idx].ToString();
                this.Controls.Add(labl);
                this.Controls.Add(trackbars[idx]);
                this.Controls.Add(textBox[idx]);
            }
            needReload = false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            if (needReload)
                Reload();
            base.OnHandleCreated(e);
        }
        private void ValueCtrl_Leave(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            float val;

            if (float.TryParse(tb.Text, out val))
            {
                int idx = (int)tb.Tag;
                val = Math.Min(Math.Max(val, Minimum[idx]), Maximum[idx]);
                if (val != Values[idx])
                {
                    Values[idx] = val;
                    OnNewValue(this, new OnNewValueArgs(Values));
                }
            }
        }

        private void ValueCtrl_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)(sender);
            float val;
            if (float.TryParse(tb.Text, out val) && !float.IsNaN(val))
            {
                int idx = (int)tb.Tag;
                val = Math.Min(Math.Max(val, Minimum[idx]), Maximum[idx]);
                val = (val - Minimum[idx]) / (Maximum[idx] - Minimum[idx]);
                trackbars[idx].Value = (int)(trackbars[idx].Maximum * val);
            }
        }

        private void ValueCtrl_Scroll(object sender, EventArgs e)
        {
            TrackBar tb = (TrackBar)sender;
            int idx = (int)tb.Tag;
            float lerpVal = ((float)tb.Value / (float)tb.Maximum);
            float newVal = (lerpVal * (Maximum[idx] - Minimum[idx])) + Minimum[idx];
            Values[idx] = newVal;
            textBox[idx].Text = newVal.ToString();
            OnNewValue(this, new OnNewValueArgs(Values));
        }
    }

    public class OnNewValueArgs : EventArgs
    {
        public float[] value;
        public OnNewValueArgs(float[] val)
        {
            value = val;
        }
    }
}

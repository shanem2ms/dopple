﻿using System;
using System.Windows;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Windows.Markup;
using wf = System.Windows.Forms;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Documents;

namespace Planes
{

    public abstract class IRenderer
    {
        public delegate void InvalidateDel();
        public InvalidateDel Invalidate;

        public abstract void Load();
        public abstract void Paint();
        public abstract void Resize(int width, int height);
        public abstract void MouseUp(int x, int y);
        public abstract void MouseDn(int x, int y, wf.MouseButtons button);
        public abstract void MouseMove(int x, int y, wf.MouseButtons button);
        public abstract void MouseWheel(int x, int y, int delta);
        public abstract void KeyDown(wf.KeyEventArgs e);
        public abstract void KeyUp(wf.KeyEventArgs e);
        public abstract void Action(int param);

    }

    public enum Modes
    {
        Video,
        Depth,
        Points,
        Planes,
        Motion,
        Scene
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Modes Mode
        {
            get => mode; set
            {
                this.mode = value; RefreshMode();
            }
        }
        Modes mode = Modes.Scene;

        public Dopple.Recording ActiveRecording => App.Recording;
        IRenderer[] renderers = new IRenderer[] {
                new VideoRenderer(),
                new DepthRenderer(),
                new PtsRenderer(),
                new PlanesRenderer(),
                new MotionRenderer(),
                new SceneRenderer()};
        System.Timers.Timer renderTimer = new System.Timers.Timer();

        public Settings Settings => App.Settings;

        public IRenderer AR => renderers[(int)Mode];

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            foreach (var r in renderers)
                r.Invalidate = OnInvalidate;
        }

        void RefreshMode()
        {

        }

        void OnInvalidate()
        {
            glControl.Invalidate();
        }

        private void glControl_Resize(object sender, EventArgs e)
        {
            foreach (var r in renderers)
                r.Resize(glControl.ClientRectangle.Width, glControl.ClientRectangle.Height);
        }

        private void RenderTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            glControl.Invalidate();
        }

        public void GlDebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            glControl.Paint += GlControl_Paint;
            glControl.MouseDown += GlControl_MouseDown;
            glControl.MouseMove += GlControl_MouseMove;
            glControl.MouseUp += GlControl_MouseUp;
            glControl.MouseWheel += GlControl_MouseWheel;
            glControl.KeyDown += GlControl_KeyDown;
            glControl.KeyUp += GlControl_KeyUp;
            renderTimer.Interval = 1.0f / 60.0f;
            renderTimer.Elapsed += RenderTimer_Elapsed;

            GL.DebugMessageCallback(GlDebugProc, IntPtr.Zero);
            Registry.LoadAllPrograms();
            foreach (var r in renderers)
                r.Load();

            renderTimer.Start();

            ActiveRecording.OnDownloadProgress += ActiveRecording_OnDownloadProgress;
        }

        private void GlControl_KeyUp(object sender, wf.KeyEventArgs e)
        {
            AR.KeyUp(e);
        }

        private void GlControl_KeyDown(object sender, wf.KeyEventArgs e)
        {
            AR.KeyDown(e);
        }

        private void ActiveRecording_OnDownloadProgress(object sender, double e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                this.DownloadProgres.Value = e * 10000.0;
            }));
        }

        private void GlControl_Paint(object sender, wf.PaintEventArgs e)
        {
            AR.Paint();
            glControl.SwapBuffers();
        }


        private void GlControl_MouseUp(object sender, wf.MouseEventArgs e)
        {
            AR.MouseUp(e.X, e.Y);
        }

        private void GlControl_MouseWheel(object sender, wf.MouseEventArgs e)
        {
            AR.MouseWheel(e.X, e.Y, e.Delta);
        }

        Vector2 ScreenToViewport(System.Drawing.Point pt)
        {
            return new Vector2(((float)pt.X / (float)glControl.Width) * 2 - 1.0f,
                             1.0f - ((float)pt.Y / (float)glControl.Height) * 2);
        }

        private void GlControl_MouseMove(object sender, wf.MouseEventArgs e)
        {
            AR.MouseMove(e.X, e.Y, e.Button);
        }

        private void GlControl_MouseDown(object sender, wf.MouseEventArgs e)
        {
            AR.MouseDn(e.X, e.Y, e.Button);
        }

        private void Back_Clicked(object sender, RoutedEventArgs e)
        {
            App.Recording.CurrentFrameIdx--;
        }
        private void Next_Clicked(object sender, RoutedEventArgs e)
        {
            App.Recording.CurrentFrameIdx++;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AR.Action(0);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AR.Action(1);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            AR.Action(2);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            AR.Action(3);
        }        
    }

    public class EnumBindingSourceExtension : MarkupExtension
    {
        private Type _enumType;
        public Type EnumType
        {
            get { return this._enumType; }
            set
            {
                if (value != this._enumType)
                {
                    if (null != value)
                    {
                        Type enumType = Nullable.GetUnderlyingType(value) ?? value;
                        if (!enumType.IsEnum)
                            throw new ArgumentException("Type must be for an Enum.");
                    }

                    this._enumType = value;
                }
            }
        }

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType)
        {
            this.EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (null == this._enumType)
                throw new InvalidOperationException("The EnumType must be specified.");

            Type actualEnumType = Nullable.GetUnderlyingType(this._enumType) ?? this._enumType;
            Array enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == this._enumType)
                return enumValues;

            Array tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}

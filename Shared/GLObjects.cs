using System;
using System.Text;
using OpenTK.Graphics.ES20;
using Gl = OpenTK.Graphics.ES20.GL;
using System.Runtime.InteropServices;

namespace GLObjects
{
    using GlID = System.Int32;
    // Note: abstractions for drawing using programmable pipeline.

    /// <summary>
    /// Shader object abstraction.
    /// </summary>
    public class Object : IDisposable
    {        
        public Object(ShaderType shaderType, string path)
        {
            string source = System.IO.File.ReadAllText(path);

            // Create
            ShaderName = Gl.CreateShader(shaderType);
            // Submit source code
            Gl.ShaderSource(ShaderName, source);
            // Compile
            Gl.CompileShader(ShaderName);
            // Check compilation status
            GlID compiled = 0;

            Gl.GetShader(ShaderName, ShaderParameter.CompileStatus, out compiled);
            if (compiled != 0)
                return;

            // Throw exception on compilation errors
            const int logMaxLength = 1024;

            StringBuilder infolog = new StringBuilder(logMaxLength);
            int infologLength;

            Gl.GetShaderInfoLog(ShaderName, logMaxLength, out infologLength, infolog);

            throw new InvalidOperationException($"unable to compile shader: {infolog}");
        }

        public readonly GlID ShaderName;

        public void Dispose()
        {
            Gl.DeleteShader(ShaderName);
        }
    }

    /// <summary>
    /// Program abstraction.
    /// </summary>
    public class Program : IDisposable
    {
        public Program(string vertexSource, string fragmentSource)
        {
            // Create vertex and frament shaders
            // Note: they can be disposed after linking to program; resources are freed when deleting the program
            using (Object vObject = new Object(ShaderType.VertexShader, vertexSource))
            using (Object fObject = new Object(ShaderType.FragmentShader, fragmentSource))
            {
                // Create program
                ProgramName = Gl.CreateProgram();
                // Attach shaders
                Gl.AttachShader(ProgramName, vObject.ShaderName);
                Gl.AttachShader(ProgramName, fObject.ShaderName);
                // Link program
                Gl.LinkProgram(ProgramName);

                // Check linkage status
                int linked;

                Gl.GetProgram(ProgramName, ProgramParameter.LinkStatus, out linked);

                if (linked == 0)
                {
                    const int logMaxLength = 1024;

                    StringBuilder infolog = new StringBuilder(logMaxLength);
                    int infologLength;

                    Gl.GetProgramInfoLog(ProgramName, 1024, out infologLength, infolog);

                    throw new InvalidOperationException($"unable to link program: {infolog}");
                }

                // Get uniform locations
                LocationMVP = Gl.GetUniformLocation(ProgramName, "uMVP");

                // Get attributes locations
                if ((LocationPosition = Gl.GetAttribLocation(ProgramName, "aPosition")) < 0)
                    throw new InvalidOperationException("no attribute aPosition");
                LocationTexCoords = Gl.GetAttribLocation(ProgramName, "aTexCoord");
                LocationNormals = Gl.GetAttribLocation(ProgramName, "aNormal");                
            }
        }

        public readonly GlID ProgramName;
        public readonly int LocationMVP;
        public readonly int LocationPosition;
        public readonly int LocationTexCoords;
        public readonly int LocationNormals;

        public void Dispose()
        {
            Gl.DeleteProgram(ProgramName);
        }
    }

    /// <summary>
    /// Buffer abstraction.
    /// </summary>
    public class Buffer : IDisposable
    {
        public Buffer(OpenTK.Vector3 []vectors)
        {
            // Generate a buffer name: buffer does not exists yet
            Gl.GenBuffers(1, out BufferName);
            // First bind create the buffer, determining its type
            Gl.BindBuffer(BufferTarget.ArrayBuffer, BufferName);
            // Set buffer information, 'buffer' is pinned automatically
            Gl.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(12 * vectors.Length), vectors, BufferUsage.StaticDraw);

        }
        public Buffer(float[] buffer)
        {
            // Generate a buffer name: buffer does not exists yet
            Gl.GenBuffers(1, out BufferName);
            // First bind create the buffer, determining its type
            Gl.BindBuffer(BufferTarget.ArrayBuffer, BufferName);
            // Set buffer information, 'buffer' is pinned automatically
            Gl.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(4 * buffer.Length), buffer, BufferUsage.StaticDraw);
        }

        public Buffer(ushort[] buffer)
        {
            // Generate a buffer name: buffer does not exists yet
            Gl.GenBuffers(1, out BufferName);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, BufferName);
            // Set buffer information, 'buffer' is pinned automatically
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(2 * buffer.Length), buffer, BufferUsage.StaticDraw);
        }

        public Buffer(uint[] buffer)
        {
            // Generate a buffer name: buffer does not exists yet
            Gl.GenBuffers(1, out BufferName);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, BufferName);
            // Set buffer information, 'buffer' is pinned automatically
            Gl.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(4 * buffer.Length), buffer, BufferUsage.StaticDraw);
        }

        public GlID BufferName;

        public void Dispose()
        {
            Gl.DeleteBuffers(1, ref BufferName);
        }
    }


    public class TextureFloat : IDisposable
    {
        public TextureFloat()
        {
            TextureName = Gl.GenTexture();
        }
        
        public void LoadDepthFrame(int depthWidth, int depthHeight, IntPtr pixels)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, TextureName);

            Gl.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance,
                depthWidth, depthHeight, 0, PixelFormat.Luminance, PixelType.UnsignedByte,
                pixels);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new OpenGL.Vertex3f(0, 0, 0));
        }

        public GlID TextureName;
        public void BindToIndex(int idx)
        {
            Gl.ActiveTexture(TextureUnit.Texture0 + idx);
            Gl.BindTexture(TextureTarget.Texture2D, TextureName);
        }

        public void Dispose()
        {
            Gl.DeleteTextures(1, ref TextureName);
        }
    }

    class TextureYUV : IDisposable
    {
        public TextureYUV()
        {
            Gl.GenTextures(1, out TextureNameY);
            Gl.GenTextures(1, out TextureNameUV);
        }

        public delegate void OnGlErrorDel();
        public void LoadImageFrame(int imageWidth, int imageHeight, IntPtr data)
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, TextureNameY);
            int ySize = imageHeight * imageWidth;
            int uvWidth = (imageWidth / 2);
            int uvHeight = (imageHeight / 2);
            int uvSize = uvHeight * uvWidth * 2;
            
            byte[] yuvData = new byte[ySize];
            Marshal.Copy(data, yuvData, 0, ySize);

            /*
            Gl.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Luminance,
                imageWidth, imageHeight, 0, PixelFormat.Luminance, PixelType.UnsignedByte,
                yuvData);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new OpenGL.Vertex3f(0, 0, 0));
            /*
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2D, TextureNameUV);
            
            byte[] uvData = new byte[uvSize];
            System.Buffer.BlockCopy(yuvData, ySize, uvData, 0, uvSize);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.LuminanceAlpha,
                uvWidth, uvHeight, 0, PixelFormat.LuminanceAlpha, PixelType.UnsignedByte,
                uvData);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);*/
        }

        public void BindToIndex(int idx0, int idx1)
        {
            Gl.ActiveTexture(TextureUnit.Texture0 + idx0);
            Gl.BindTexture(TextureTarget.Texture2D, TextureNameY);
            Gl.ActiveTexture(TextureUnit.Texture0 + idx1);
            Gl.BindTexture(TextureTarget.Texture2D, TextureNameUV);
        }

        public uint TextureNameY;
        public uint TextureNameUV;

        public void Dispose()
        {
            Gl.DeleteTextures(1, ref TextureNameY);
            Gl.DeleteTextures(1, ref TextureNameUV);
        }
    }

    /// <summary>
    /// Vertex array abstraction.
    /// </summary>
    class VertexArray : IDisposable
    {
        public VertexArray(OpenTK.Vector3[] positions, object elems, OpenTK.Vector3[] texCoords,
            OpenTK.Vector3[] normals)
        {           
            int stride = 3;
            // Allocate buffers referenced by this vertex array
            _BufferPosition = new GLObjects.Buffer(positions);
            if (texCoords != null)
            {
                _BufferTexCoords = new GLObjects.Buffer(texCoords);
                stride += 3;
            }
            if (normals != null)
            {
                _BufferNormal = new GLObjects.Buffer(normals);
                stride += 3;
            }
            if (elems != null)
            {
                if (elems is ushort[])
                    _BufferElems = new GLObjects.Buffer((ushort[])elems);
                else
                    _BufferElems = new GLObjects.Buffer((uint[])elems);
            }
        }

        public void Bind(Program program)
        {
            int stride = 0;
            // Select the buffer object
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _BufferPosition.BufferName);
            Gl.VertexAttribPointer((uint)program.LocationPosition, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray((uint)program.LocationPosition);

            if (_BufferTexCoords != null)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, _BufferTexCoords.BufferName);
                Gl.VertexAttribPointer((uint)program.LocationTexCoords, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), IntPtr.Zero);
                Gl.EnableVertexAttribArray((uint)program.LocationTexCoords);
            }

            if (_BufferNormal != null)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, _BufferNormal.BufferName);
                Gl.VertexAttribPointer((uint)program.LocationNormals, 3, VertexAttribPointerType.Float, false, stride * sizeof(float), IntPtr.Zero);
                Gl.EnableVertexAttribArray((uint)program.LocationNormals);
            }

            if (_BufferElems != null)
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, _BufferElems.BufferName);
        }

        public readonly uint ArrayName;

        private readonly GLObjects.Buffer _BufferPosition;
        private readonly GLObjects.Buffer _BufferTexCoords = null;
        private readonly GLObjects.Buffer _BufferNormal = null;
        private readonly GLObjects.Buffer _BufferElems = null;

        public void Dispose()
        {
            _BufferPosition.Dispose();
            _BufferTexCoords.Dispose();
            _BufferNormal.Dispose();
            _BufferElems.Dispose();
        }
    }
}

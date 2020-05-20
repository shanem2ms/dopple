using System;
using OpenTK.Graphics.ES30;
using OpenTK;
using GLObjects;
using System.Collections.Generic;
using System.Linq;


namespace Planes
{
    class Selection
    {
        GLObjects.Program Program;
        VertexArray cube;
        public Selection()
        {
            Program = Registry.Programs["main"];
            this.cube = Cube.MakeCube(Program);
        }

        public Vector3 wPos;

        public void Draw(Matrix4 viewProj)
        {
            Vector3 sPos = Vector3.TransformPerspective(wPos, viewProj);
            sPos += new Vector3(0.01f, 0.01f, 0);
            Vector3 wPos2 = Vector3.TransformPerspective(sPos, viewProj.Inverted());
            float scale = (wPos2 - wPos).Length;
            Matrix4 wvpMat = Matrix4.CreateScale(scale) *
                Matrix4.CreateTranslation(wPos) * viewProj;

            Program.Use(0);

            Program.SetMat4("uMVP", ref wvpMat);
            Program.Set1("opacity", 1.0f);
            Program.Set3("meshColor", new Vector3(1, 1, 1));
            Program.Set1("ambient", 0.75f);
            Program.Set3("lightPos", new Vector3(2, 5, 2));
            Program.Set1("opacity", 1.0f);
            Matrix4 matWorldInvT = Matrix4.Identity;
            Program.SetMat4("uWorld", ref matWorldInvT);
            Program.SetMat4("uWorldInvTranspose", ref matWorldInvT);

            cube.Draw();

        }
    }
}

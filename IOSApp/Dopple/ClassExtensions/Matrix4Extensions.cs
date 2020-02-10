using System;
using Foundation;
using SceneKit;
using ARKit;
using OpenTK;

namespace Dopple
{
	public static class Matrix4Extensions
	{
		public static SCNMatrix4 ToSCNMatrix4(this NMatrix4 self)
		{
            return new SCNMatrix4(self.M11, self.M12, self.M13, self.M14,
                 self.M21, self.M22, self.M23, self.M24,
                 self.M31, self.M32, self.M33, self.M34,
                 self.M41, self.M42, self.M43, self.M44);
		}

        public static NMatrix4 ToNMatrix4(this SCNMatrix4 self)
        {
            return new NMatrix4(self.M11, self.M12, self.M13, self.M14,
                self.M21, self.M22, self.M23, self.M24,
                self.M31, self.M32, self.M33, self.M34,
                self.M41, self.M42, self.M43, self.M44);

        }
	}
}

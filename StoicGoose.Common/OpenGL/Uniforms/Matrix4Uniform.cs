using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Matrix4Uniform(string name, Matrix4 value) : GenericUniform<Matrix4>(name, value)
    {
        public Matrix4Uniform(string name) : this(name, Matrix4.Identity) { }

        protected override void SubmitUniform(int location)
        {
            GL.UniformMatrix4(location, false, ref value);
        }
    }
}

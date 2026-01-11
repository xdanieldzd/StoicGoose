using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Vector4Uniform(string name, Vector4 value) : GenericUniform<Vector4>(name, value)
    {
        public Vector4Uniform(string name) : this(name, Vector4.Zero) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform4(location, value);
        }
    }
}

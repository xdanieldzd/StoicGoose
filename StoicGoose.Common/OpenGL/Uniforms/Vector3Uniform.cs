using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Vector3Uniform(string name, Vector3 value) : GenericUniform<Vector3>(name, value)
    {
        public Vector3Uniform(string name) : this(name, Vector3.Zero) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform3(location, value);
        }
    }
}

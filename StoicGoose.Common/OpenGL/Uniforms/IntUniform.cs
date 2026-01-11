using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class IntUniform(string name, int value) : GenericUniform<int>(name, value)
    {
        public IntUniform(string name) : this(name, 0) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform1(location, value);
        }
    }
}

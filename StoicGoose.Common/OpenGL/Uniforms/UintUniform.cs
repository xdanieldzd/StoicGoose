using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class UintUniform(string name, uint value) : GenericUniform<uint>(name, value)
    {
        public UintUniform(string name) : this(name, 0) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform1(location, value);
        }
    }
}

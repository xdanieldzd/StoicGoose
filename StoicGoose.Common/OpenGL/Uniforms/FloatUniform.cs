using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class FloatUniform(string name, float value) : GenericUniform<float>(name, value)
    {
        public FloatUniform(string name) : this(name, 0.0f) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform1(location, value);
        }
    }
}

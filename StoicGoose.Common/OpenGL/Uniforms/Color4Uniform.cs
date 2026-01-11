using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Color4Uniform(string name, Color4 value) : GenericUniform<Color4>(name, value)
    {
        public Color4Uniform(string name) : this(name, Color4.White) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform4(location, value);
        }
    }
}

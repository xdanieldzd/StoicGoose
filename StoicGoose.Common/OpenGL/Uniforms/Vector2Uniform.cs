using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Vector2Uniform(string name, Vector2 value) : GenericUniform<Vector2>(name, value)
    {
        public Vector2Uniform(string name) : this(name, Vector2.Zero) { }

        protected override void SubmitUniform(int location)
        {
            GL.Uniform2(location, value);
        }
    }
}

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public sealed class Matrix2Uniform(string name, Matrix2 value) : GenericUniform<Matrix2>(name, value)
    {
        public Matrix2Uniform(string name) : this(name, Matrix2.Identity) { }

        protected override void SubmitUniform(int location)
        {
            GL.UniformMatrix2(location, false, ref value);
        }
    }
}

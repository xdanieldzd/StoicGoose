using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Matrix2Uniform : GenericUniform<Matrix2>
	{
		public Matrix2Uniform(string name) : this(name, Matrix2.Identity) { }
		public Matrix2Uniform(string name, Matrix2 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.UniformMatrix2(location, false, ref value);
		}
	}
}

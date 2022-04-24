using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Matrix3Uniform : GenericUniform<Matrix3>
	{
		public Matrix3Uniform(string name) : this(name, Matrix3.Identity) { }
		public Matrix3Uniform(string name, Matrix3 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.UniformMatrix3(location, false, ref value);
		}
	}
}

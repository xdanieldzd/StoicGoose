using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Matrix4Uniform : GenericUniform<Matrix4>
	{
		public Matrix4Uniform(string name) : this(name, Matrix4.Identity) { }
		public Matrix4Uniform(string name, Matrix4 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.UniformMatrix4(location, false, ref value);
		}
	}
}

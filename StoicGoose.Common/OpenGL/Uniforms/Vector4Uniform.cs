using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Vector4Uniform : GenericUniform<Vector4>
	{
		public Vector4Uniform(string name) : this(name, Vector4.Zero) { }
		public Vector4Uniform(string name, Vector4 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform4(location, value);
		}
	}
}

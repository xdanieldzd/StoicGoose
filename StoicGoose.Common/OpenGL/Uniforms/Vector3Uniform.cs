using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Vector3Uniform : GenericUniform<Vector3>
	{
		public Vector3Uniform(string name) : this(name, Vector3.Zero) { }
		public Vector3Uniform(string name, Vector3 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform3(location, value);
		}
	}
}

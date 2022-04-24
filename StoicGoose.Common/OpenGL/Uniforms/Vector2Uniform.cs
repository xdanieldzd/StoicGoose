using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Vector2Uniform : GenericUniform<Vector2>
	{
		public Vector2Uniform(string name) : this(name, Vector2.Zero) { }
		public Vector2Uniform(string name, Vector2 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform2(location, value);
		}
	}
}

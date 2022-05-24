using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class Color4Uniform : GenericUniform<Color4>
	{
		public Color4Uniform(string name) : this(name, Color4.White) { }
		public Color4Uniform(string name, Color4 value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform4(location, value);
		}
	}
}

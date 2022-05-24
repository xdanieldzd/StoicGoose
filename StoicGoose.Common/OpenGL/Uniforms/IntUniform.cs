using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class IntUniform : GenericUniform<int>
	{
		public IntUniform(string name) : this(name, 0) { }
		public IntUniform(string name, int value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform1(location, value);
		}
	}
}

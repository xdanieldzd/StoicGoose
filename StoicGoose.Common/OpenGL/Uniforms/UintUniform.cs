using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class UintUniform : GenericUniform<uint>
	{
		public UintUniform(string name) : this(name, 0) { }
		public UintUniform(string name, uint value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform1(location, value);
		}
	}
}

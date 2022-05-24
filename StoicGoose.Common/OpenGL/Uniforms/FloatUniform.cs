using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public sealed class FloatUniform : GenericUniform<float>
	{
		public FloatUniform(string name) : this(name, 0.0f) { }
		public FloatUniform(string name, float value) : base(name, value) { }

		protected override void SubmitUniform(int location)
		{
			GL.Uniform1(location, value);
		}
	}
}

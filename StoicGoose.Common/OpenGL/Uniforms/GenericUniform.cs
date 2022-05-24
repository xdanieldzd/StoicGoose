using ShaderProgram = StoicGoose.Common.OpenGL.Shaders.Program;

namespace StoicGoose.Common.OpenGL.Uniforms
{
	public abstract class GenericUniform<T>
	{
		protected readonly string name;
		protected T value;

		public string Name => name;
		public T Value
		{
			get => value;
			set => this.value = value;
		}

		public GenericUniform(string name) : this(name, default) { }

		public GenericUniform(string name, T value)
		{
			this.name = name;
			this.value = value;
		}

		public void SubmitToProgram(ShaderProgram shaderProgram)
		{
			var location = shaderProgram.GetUniformLocation(name);
			if (location != -1) SubmitUniform(location);
		}

		protected abstract void SubmitUniform(int location);
	}
}

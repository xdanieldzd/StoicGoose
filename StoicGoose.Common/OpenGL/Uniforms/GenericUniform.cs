using ShaderProgram = StoicGoose.Common.OpenGL.Shaders.Program;

namespace StoicGoose.Common.OpenGL.Uniforms
{
    public abstract class GenericUniform<T>(string name, T value)
    {
        protected readonly string name = name;
        protected T value = value;

        public string Name => name;
        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public GenericUniform(string name) : this(name, default) { }

        public void SubmitToProgram(ShaderProgram shaderProgram)
        {
            var location = shaderProgram.GetUniformLocation(name);
            if (location != -1) SubmitUniform(location);
        }

        protected abstract void SubmitUniform(int location);
    }
}

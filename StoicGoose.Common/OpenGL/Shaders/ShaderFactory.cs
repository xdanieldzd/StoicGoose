using System;
using System.Linq;

using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.Common.OpenGL.Shaders
{
	public static class ShaderFactory
	{
		public static int FromSource(ShaderType shaderType, params string[] shaderSource)
		{
			shaderSource = Sanitize(shaderSource);

			int handle = GL.CreateShader(shaderType);
			GL.ShaderSource(handle, shaderSource.Length, shaderSource, (int[])null);
			GL.CompileShader(handle);

			GL.GetShader(handle, ShaderParameter.CompileStatus, out int status);
			if (status != 1)
			{
				GL.GetShaderInfoLog(handle, out string info);
				GL.DeleteShader(handle);
				throw new Exception($"{shaderType} compile failed:\n{info}");
			}

			return handle;
		}

		private static string[] Sanitize(string[] shaderSource)
		{
			return shaderSource.Where(x => !string.IsNullOrEmpty(x)).Select(z => string.Concat(z, Environment.NewLine)).ToArray();
		}
	}
}

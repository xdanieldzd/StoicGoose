using System;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.OpenGL.Shaders
{
	public sealed class Program : IDisposable
	{
		public int Handle { get; } = GL.CreateProgram();

		readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

		public Program(params int[] shaders)
		{
			foreach (var shader in shaders) GL.AttachShader(Handle, shader);
			GL.LinkProgram(Handle);

			GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
			if (status != 1)
			{
				GL.GetProgramInfoLog(Handle, out string info);
				GL.DeleteProgram(Handle);
				throw new Exception($"Program link failed:\n{info}");
			}

			foreach (var shader in shaders)
			{
				GL.DetachShader(Handle, shader);
				GL.DeleteShader(shader);
			}

			GL.UseProgram(0);
		}

		~Program()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (GL.IsProgram(Handle))
				GL.DeleteProgram(Handle);
		}

		public void Bind()
		{
			GL.UseProgram(Handle);
		}

		public int GetUniformLocation(string name)
		{
			if (!uniformLocations.ContainsKey(name))
			{
				var location = GL.GetUniformLocation(Handle, name);
				if (location != -1) uniformLocations[name] = location;
				return location;
			}
			else
				return uniformLocations[name];
		}
	}
}

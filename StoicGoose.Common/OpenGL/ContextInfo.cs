using System.Linq;

using OpenTK.Graphics.OpenGL4;

using StoicGoose.Common.Utilities;

namespace StoicGoose.Common.OpenGL
{
	public static class ContextInfo
	{
		public static string GLRenderer { get; } = GL.GetString(StringName.Renderer);
		public static string GLShadingLanguageVersion { get; } = GL.GetString(StringName.ShadingLanguageVersion);
		public static string GLVendor { get; } = GL.GetString(StringName.Vendor);
		public static string GLVersion { get; } = GL.GetString(StringName.Version);
		public static string[] GLExtensions { get; } = new string[GL.GetInteger(GetPName.NumExtensions)].Select((x, i) => x = GL.GetString(StringNameIndexed.Extensions, i)).ToArray();

		public static void WriteToLog(object source, bool withExtensions = false)
		{
			Log.WriteEvent(LogSeverity.Debug, source, "OpenGL context:");
			Log.WriteEvent(LogSeverity.Debug, source, $"- Renderer: {GLRenderer}");
			Log.WriteEvent(LogSeverity.Debug, source, $"- Vendor: {GLVendor}");
			Log.WriteEvent(LogSeverity.Debug, source, $"- Version: {GLVersion}");
			Log.WriteEvent(LogSeverity.Debug, source, $"- GLSL version: {GLShadingLanguageVersion}");
			Log.WriteEvent(LogSeverity.Debug, source, $"- {GLExtensions.Length} extension(s) supported.");
			if (withExtensions)
				foreach (var extension in GLExtensions)
					Log.WriteEvent(LogSeverity.Debug, source, $" {extension}");
		}
	}
}

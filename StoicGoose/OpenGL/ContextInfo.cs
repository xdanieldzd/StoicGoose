using System.Linq;

using OpenTK.Graphics.OpenGL4;

namespace StoicGoose.OpenGL
{
	public static class ContextInfo
	{
		public static string GLRenderer { get; } = GL.GetString(StringName.Renderer);
		public static string GLShadingLanguageVersion { get; } = GL.GetString(StringName.ShadingLanguageVersion);
		public static string GLVendor { get; } = GL.GetString(StringName.Vendor);
		public static string GLVersion { get; } = GL.GetString(StringName.Version);
		public static string[] GLExtensions { get; } = new string[GL.GetInteger(GetPName.NumExtensions)].Select((x, i) => x = GL.GetString(StringNameIndexed.Extensions, i)).ToArray();
	}
}

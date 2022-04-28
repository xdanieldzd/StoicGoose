using System.IO;
using System.Reflection;

using StoicGoose.Common.Drawing;

namespace StoicGoose.Common.Utilities
{
	public static class Resources
	{
		private static Stream GetEmbeddedResourceStream(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			name = $"{assembly.GetName().Name}.{name}";
			return assembly.GetManifestResourceStream(name);
		}

		public static RgbaFile GetEmbeddedRgbaFile(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return null;
			return new RgbaFile(stream);
		}

		public static string GetEmbeddedText(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return string.Empty;
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static RgbaFile GetEmbeddedSystemIcon(string name)
		{
			return GetEmbeddedRgbaFile($"Assets.Icons.{name}");
		}

		public static string GetEmbeddedShaderFile(string name)
		{
			return GetEmbeddedText($"Assets.Shaders.{name}");
		}
	}
}

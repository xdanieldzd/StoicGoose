using System.Drawing;
using System.IO;
using System.Reflection;

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

		public static Bitmap GetEmbeddedBitmap(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return null;
			return new Bitmap(stream);
		}

		public static string GetEmbeddedText(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return string.Empty;
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static Bitmap GetEmbeddedSystemIcon(string name)
		{
			return GetEmbeddedBitmap($"Assets.Icons.{name}");
		}

		public static string GetEmbeddedShaderFile(string name)
		{
			return GetEmbeddedText($"Assets.Shaders.{name}");
		}
	}
}

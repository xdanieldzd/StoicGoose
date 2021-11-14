using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StoicGoose.OpenGL.Shaders.Bundles
{
	public enum FilterMode { Linear, Nearest }

	public enum WrapMode { Repeat, Edge, Border, Mirror }

	public class BundleManifest
	{
		// TODO: move these strings elsewhere?
		public static string DefaultManifestFilename { get; } = "Manifest.json";
		public static string DefaultSourceFilename { get; } = "Fragment.glsl";
		public static string DefaultShaderName { get; } = "Basic";

		[JsonConverter(typeof(StringEnumConverter))]
		public FilterMode Filter { get; set; } = FilterMode.Linear;
		[JsonConverter(typeof(StringEnumConverter))]
		public WrapMode Wrap { get; set; } = WrapMode.Repeat;
		public int Samplers { get; set; } = 3;
	}
}

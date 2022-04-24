using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StoicGoose.Common.OpenGL.Shaders.Bundles
{
	public enum FilterMode { Linear, Nearest }

	public enum WrapMode { Repeat, Edge, Border, Mirror }

	public class BundleManifest
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public FilterMode Filter { get; set; } = FilterMode.Linear;
		[JsonConverter(typeof(StringEnumConverter))]
		public WrapMode Wrap { get; set; } = WrapMode.Repeat;
		public int Samplers { get; set; } = 3;
	}
}

using System.Collections.Generic;

namespace StoicGoose.Interface
{
	public sealed class Configuration : ConfigurationBase<Configuration>
	{
		public GeneralConfiguration General { get; set; } = new GeneralConfiguration();
		public VideoConfiguration Video { get; set; } = new VideoConfiguration();
		public SoundConfiguration Sound { get; set; } = new SoundConfiguration();
		public InputConfiguration Input { get; set; } = new InputConfiguration();
	}

	public sealed class GeneralConfiguration : ConfigurationBase<GeneralConfiguration>
	{
		public string BootstrapFile { get; set; } = string.Empty;
		public bool LimitFps { get; set; } = true;
		public List<string> RecentFiles { get; set; } = new List<string>(15);
	}

	public sealed class VideoConfiguration : ConfigurationBase<VideoConfiguration>
	{
		public int ScreenSize { get; set; } = 3;
	}

	public sealed class SoundConfiguration : ConfigurationBase<SoundConfiguration>
	{
		public bool Mute { get; set; } = false;
		public bool LowPassFilter { get; set; } = true;
	}

	public sealed class InputConfiguration : ConfigurationBase<InputConfiguration>
	{
		public Dictionary<string, string> Controls { get; set; } = new Dictionary<string, string>();
		public Dictionary<string, string> Hardware { get; set; } = new Dictionary<string, string>();
	}
}

using System.Collections.Generic;
using System.ComponentModel;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;

namespace StoicGoose.WPF
{
	public sealed class Configuration : ConfigurationBase<Configuration>
	{
		[DisplayName("Preferred System")]
		[Description("Preferred system to emulate.")]
		public string PreferredSystem { get; set; } = typeof(WonderSwanColor).FullName;

		[DisplayName("Use Bootstrap ROMs")]
		[Description("Toggle using WonderSwan bootstrap ROM images.")]
		public bool UseBootstrap { get; set; } = false;
		[DisplayName("Bootstrap ROM Paths")]
		[Description("Paths to the WonderSwan bootstrap ROM images to use.")]
		public Dictionary<string, string> BootstrapFiles { get; set; } = new();

		[DisplayName("Limit FPS")]
		[Description("Toggle limiting the framerate to the system's native ~75.47 Hz.")]
		public bool LimitFps { get; set; } = true;

		[DisplayName("Mute")]
		[Description("Toggles muting all sound output.")]
		public bool Mute { get; set; } = false;

		[DisplayName("Game Controls")]
		[Description("Controls related to game input, i.e. X-/Y-pads, etc.")]
		public Dictionary<string, string> GameControls { get; set; } = new();
		[DisplayName("System Controls")]
		[Description("Controls related to hardware functions, i.e. volume button.")]
		public Dictionary<string, string> SystemControls { get; set; } = new();
	}
}

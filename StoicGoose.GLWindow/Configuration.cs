using System.ComponentModel;
using System.Collections.Generic;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;

namespace StoicGoose.GLWindow
{
	public sealed class Configuration : ConfigurationBase<Configuration>
	{
		[DisplayName("Preferred System")]
		[Description("Preferred system to emulate.")]
		public string PreferredSystem { get; set; } = typeof(WonderSwan).FullName;

		[DisplayName("Use Bootstrap ROMs")]
		[Description("Toggle using WonderSwan bootstrap ROM images.")]
		public bool UseBootstrap { get; set; } = false;
		[DisplayName("Bootstrap ROM Paths")]
		[Description("Path to the WonderSwan bootstrap ROM image to use.")]
		public Dictionary<string, string> BootstrapFiles { get; set; } = new();

		[DisplayName("Limit FPS")]
		[Description("Toggle limiting the framerate to the system's native ~75.47 Hz.")]
		public bool LimitFps { get; set; } = true;

		[DisplayName("Screen Size")]
		[Description("Size of the emulated screen, in times original display resolution.")]
		public int ScreenSize { get; set; } = 3;

		[DisplayName("Mute")]
		[Description("Toggles muting all sound output.")]
		public bool Mute { get; set; } = false;

		[DisplayName("Game Controls")]
		[Description("Controls related to game input, i.e. X-/Y-pads, etc.")]
		public Dictionary<string, string> GameControls { get; set; } = new Dictionary<string, string>();
		[DisplayName("System Controls")]
		[Description("Controls related to hardware functions, i.e. volume button.")]
		public Dictionary<string, string> SystemControls { get; set; } = new Dictionary<string, string>();
	}
}

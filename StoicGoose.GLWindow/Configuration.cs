﻿using System.Collections.Generic;
using System.ComponentModel;

using StoicGoose.Common.Localization;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;

namespace StoicGoose.GLWindow
{
	public sealed class Configuration : ConfigurationBase<Configuration>
	{
		[DisplayName("UI Language")]
		[Description("Preferred user interface language.")]
		public string Language { get; set; } = Localizer.FallbackCulture;

		[DisplayName("Preferred System")]
		[Description("Preferred system to emulate.")]
		public string PreferredSystem { get; set; } = typeof(WonderSwan).FullName;

		[DisplayName("Use Bootstrap ROMs")]
		[Description("Toggle using WonderSwan bootstrap ROM images.")]
		public bool UseBootstrap { get; set; } = false;
		[DisplayName("Bootstrap ROM Paths")]
		[Description("Paths to the WonderSwan bootstrap ROM images to use.")]
		public Dictionary<string, string> BootstrapFiles { get; set; } = new();

		[DisplayName("Last ROM Loaded")]
		[Description("Most recently loaded ROM image.")]
		public string LastRomLoaded { get; set; } = string.Empty;

		[DisplayName("Enable Patch Callbacks")]
		[Description("Enable or disable callbacks functions for patches.")]
		public bool EnablePatchCallbacks { get; set; } = false;

		[DisplayName("Enable Breakpoints")]
		[Description("Enable or disable breakpoints.")]
		public bool EnableBreakpoints { get; set; } = false;

		[DisplayName("Limit FPS")]
		[Description("Toggle limiting the framerate to the system's native ~75.47 Hz.")]
		public bool LimitFps { get; set; } = true;

		[DisplayName("Display Size")]
		[Description("Size of the emulated screen, in times original display resolution.")]
		public int DisplaySize { get; set; } = 3;

		[DisplayName("Mute")]
		[Description("Toggles muting all sound output.")]
		public bool Mute { get; set; } = false;

		[DisplayName("Automatic Remapping")]
		[Description("Automatically remap X-/Y-pads with game orientation.")]
		public bool AutoRemap { get; set; } = true;
		[DisplayName("Game Controls")]
		[Description("Controls related to game input, i.e. X-/Y-pads, etc.")]
		public Dictionary<string, string> GameControls { get; set; } = new();
		[DisplayName("System Controls")]
		[Description("Controls related to hardware functions, i.e. volume button.")]
		public Dictionary<string, string> SystemControls { get; set; } = new();

		[DisplayName("Restored Windows")]
		[Description("Windows restored on program start.")]
		public List<string> WindowsToRestore { get; set; } = new();
	}
}

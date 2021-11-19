﻿using System.ComponentModel;
using System.Collections.Generic;

namespace StoicGoose.Interface
{
	public sealed class Configuration : ConfigurationBase<Configuration>
	{
		[DisplayName("General")]
		[Description("General settings.")]
		public GeneralConfiguration General { get; set; } = new GeneralConfiguration();
		[DisplayName("Video")]
		[Description("Settings related to video output.")]
		public VideoConfiguration Video { get; set; } = new VideoConfiguration();
		[DisplayName("Sound")]
		[Description("Settings related to sound output.")]
		public SoundConfiguration Sound { get; set; } = new SoundConfiguration();
		[DisplayName("Input")]
		[Description("Settings related to emulation input.")]
		public InputConfiguration Input { get; set; } = new InputConfiguration();
	}

	public sealed class GeneralConfiguration : ConfigurationBase<GeneralConfiguration>
	{
		[DisplayName("Bootstrap ROM")]
		[Description("Path to the WonderSwan bootstrap ROM image to use.")]
		public string BootstrapFile { get; set; } = string.Empty;
		[DisplayName("Limit FPS")]
		[Description("Toggle limiting the framerate to the system's native ~75.47 Hz.")]
		public bool LimitFps { get; set; } = true;
		[DisplayName("Recent Files")]
		[Description("List of recently loaded files.")]
		public List<string> RecentFiles { get; set; } = new List<string>(15);
	}

	public sealed class VideoConfiguration : ConfigurationBase<VideoConfiguration>
	{
		[DisplayName("Screen Size")]
		[Description("Size of the emulated screen, in times original display resolution.")]
		public int ScreenSize { get; set; } = 3;
		[DisplayName("Shader")]
		[Description("Currently selected shader.")]
		public string Shader { get; set; } = string.Empty;
	}

	public sealed class SoundConfiguration : ConfigurationBase<SoundConfiguration>
	{
		[DisplayName("Mute")]
		[Description("Toggles muting all sound output.")]
		public bool Mute { get; set; } = false;
		[DisplayName("Low-Pass Filter")]
		[Description("Toggles low-pass filter for all sound output.")]
		public bool LowPassFilter { get; set; } = true;
	}

	public sealed class InputConfiguration : ConfigurationBase<InputConfiguration>
	{
		[DisplayName("Game Controls")]
		[Description("Controls related to game input, i.e. X-/Y-pads, etc.")]
		public Dictionary<string, string> GameControls { get; set; } = new Dictionary<string, string>();
		[DisplayName("System Controls")]
		[Description("Controls related to hardware functions, i.e. volume button.")]
		public Dictionary<string, string> SystemControls { get; set; } = new Dictionary<string, string>();
	}
}

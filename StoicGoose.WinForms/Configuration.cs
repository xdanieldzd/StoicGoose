using StoicGoose.Common.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace StoicGoose.WinForms
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
        [DisplayName("Prefer Original WS")]
        [Description("Prefer emulation of the original non-Color system.")]
        public bool PreferOriginalWS { get; set; } = false;
        [DisplayName("Use Bootstrap ROM")]
        [Description("Toggle using WonderSwan bootstrap ROM images.")]
        public bool UseBootstrap { get; set; } = false;
        [DisplayName("WS Bootstrap ROM Path")]
        [Description("Path to the WonderSwan bootstrap ROM image to use.")]
        public string BootstrapFile { get; set; } = string.Empty;
        [DisplayName("WSC Bootstrap ROM Path")]
        [Description("Path to the WonderSwan Color bootstrap ROM image to use.")]
        public string BootstrapFileWSC { get; set; } = string.Empty;
        [DisplayName("Limit FPS")]
        [Description("Toggle limiting the framerate to the system's native ~75.47 Hz.")]
        public bool LimitFps { get; set; } = true;
        [DisplayName("Enable Cheats")]
        [Description("Toggle using the cheat system.")]
        public bool EnableCheats { get; set; } = true;
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
        [DisplayName("Brightness")]
        [Description("Adjust the brightness of the emulated screen, in percent.")]
        [Range(-100, 100)]
        public int Brightness { get; set; } = 0;
        [DisplayName("Contrast")]
        [Description("Adjust the contrast of the emulated screen, in percent.")]
        [Range(0, 200)]
        public int Contrast { get; set; } = 100;
        [DisplayName("Saturation")]
        [Description("Adjust the saturation of the emulated screen, in percent.")]
        [Range(0, 200)]
        public int Saturation { get; set; } = 100;
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
        [DisplayName("Automatic Remapping")]
        [Description("Automatically remap X-/Y-pads with game orientation.")]
        public bool AutoRemap { get; set; } = true;
        [DisplayName("Game Controls")]
        [Description("Controls related to game input, i.e. X-/Y-pads, etc.")]
        public Dictionary<string, List<string>> GameControls { get; set; } = [];
        [DisplayName("System Controls")]
        [Description("Controls related to hardware functions, i.e. volume button.")]
        public Dictionary<string, List<string>> SystemControls { get; set; } = [];
    }
}

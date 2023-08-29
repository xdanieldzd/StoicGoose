using System.Windows.Forms;

using static StoicGoose.WinForms.Windows.ControlHelpers;

namespace StoicGoose.WinForms
{
	public partial class SettingsForm : Form
	{
		public Configuration Configuration { get; } = default;

		public SettingsForm(Configuration configuration)
		{
			Configuration = configuration;

			InitializeComponent();

			InitializePages();
		}

		private void InitializePages()
		{
			var pageGeneral = new SettingsPage(Configuration, nameof(Configuration.General));
			pageGeneral.Append(CreateToggle(Configuration.General, nameof(Configuration.General.PreferOriginalWS)));
			pageGeneral.Append(CreateToggle(Configuration.General, nameof(Configuration.General.UseBootstrap)));
			pageGeneral.Append(CreatePathSelector(Configuration.General, nameof(Configuration.General.BootstrapFile)));
			pageGeneral.Append(CreatePathSelector(Configuration.General, nameof(Configuration.General.BootstrapFileWSC)));
			pageGeneral.Append(CreateToggle(Configuration.General, nameof(Configuration.General.LimitFps)));
			pageGeneral.Append(CreateToggle(Configuration.General, nameof(Configuration.General.EnableCheats)));
			pageGeneral.Attach(tvSettings);

			var pageVideo = new SettingsPage(Configuration, nameof(Configuration.Video));
			pageVideo.Append(CreateSlider<int>(Configuration.Video, nameof(Configuration.Video.Brightness)));
			pageVideo.Append(CreateSlider<int>(Configuration.Video, nameof(Configuration.Video.Contrast)));
			pageVideo.Append(CreateSlider<int>(Configuration.Video, nameof(Configuration.Video.Saturation)));
			pageVideo.Attach(tvSettings);

			var pageSound = new SettingsPage(Configuration, nameof(Configuration.Sound));
			pageSound.Append(CreateToggle(Configuration.Sound, nameof(Configuration.Sound.Mute)));
			pageSound.Append(CreateToggle(Configuration.Sound, nameof(Configuration.Sound.LowPassFilter)));
			pageSound.Attach(tvSettings);

			var pageInput = new SettingsPage(Configuration, nameof(Configuration.Input));
			pageInput.Append(CreateToggle(Configuration.Input, nameof(Configuration.Input.AutoRemap)));
			var pageInputGame = new SettingsPage(Configuration.Input, nameof(Configuration.Input.GameControls));
			foreach (var (key, _) in Configuration.Input.GameControls)
				pageInputGame.Append(CreateKeyInput(Configuration.Input, nameof(Configuration.Input.GameControls), key));
			pageInput.Append(pageInputGame);
			var pageInputSystem = new SettingsPage(Configuration.Input, nameof(Configuration.Input.SystemControls));
			foreach (var (key, _) in Configuration.Input.SystemControls)
				pageInputSystem.Append(CreateKeyInput(Configuration.Input, nameof(Configuration.Input.SystemControls), key));
			pageInput.Append(pageInputSystem);
			pageInput.Attach(tvSettings);
		}

		private void tvSettings_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node.Tag is Control[] controls)
			{
				tlpSettings.SuspendLayout();
				tlpSettings.Controls.Clear();
				foreach (var control in controls)
				{
					tlpSettings.Controls.Add(control);
					if (control.Tag is int span) { tlpSettings.SetColumnSpan(control, span); }
				}
				lblNothing.Text = string.Empty;
				lblNothing.Visible = false;

				tlpSettings.ResumeLayout();
				tlpSettings.Visible = true;
			}
			else
			{
				tlpSettings.Visible = false;

				lblNothing.Text = "Honk.";
				lblNothing.Visible = true;
			}
		}
	}
}

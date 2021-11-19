﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using StoicGoose.Interface;

using static StoicGoose.WinForms.ControlHelpers;

namespace StoicGoose
{
	public partial class SettingsForm : Form
	{
		public Configuration Configuration { get; } = default;

		public SettingsForm(Configuration configuration)
		{
			Configuration = configuration;

			InitializeComponent();

			InitializePages();

			pbBackground.BackgroundImage = Utilities.GetEmbeddedBitmap("Assets.Goose.png");
			pbBackground.BackgroundImageLayout = ImageLayout.Zoom;
		}

		private void InitializePages()
		{
			var pageGeneral = new SettingsPage(Configuration, nameof(Configuration.General));
			pageGeneral.Append(CreatePathSelector(Configuration.General, nameof(Configuration.General.BootstrapFile)));
			pageGeneral.Append(CreateToggle(Configuration.General, nameof(Configuration.General.LimitFps)));
			pageGeneral.Attach(tvSettings);

			var pageVideo = new SettingsPage(Configuration, nameof(Configuration.Video));
			//
			pageVideo.Attach(tvSettings);

			var pageSound = new SettingsPage(Configuration, nameof(Configuration.Sound));
			pageSound.Append(CreateToggle(Configuration.Sound, nameof(Configuration.Sound.Mute)));
			pageSound.Append(CreateToggle(Configuration.Sound, nameof(Configuration.Sound.LowPassFilter)));
			pageSound.Attach(tvSettings);

			var pageInput = new SettingsPage(Configuration, nameof(Configuration.Input));
			var pageInputGame = new SettingsPage(Configuration.Input, nameof(Configuration.Input.GameControls));
			//
			pageInput.Append(pageInputGame);
			var pageInputSystem = new SettingsPage(Configuration.Input, nameof(Configuration.Input.SystemControls));
			//
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
				pbBackground.Visible = false;

				tlpSettings.ResumeLayout();
				tlpSettings.Visible = true;
			}
			else
			{
				tlpSettings.Visible = false;

				lblNothing.Text = "Honk.";
				lblNothing.Visible = true;
				pbBackground.Visible = true;
			}
		}
	}
}

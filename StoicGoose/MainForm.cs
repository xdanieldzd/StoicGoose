using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

using StoicGoose.Emulation;
using StoicGoose.Handlers;

namespace StoicGoose
{
	public partial class MainForm : Form
	{
		// constants
		readonly static int maxScreenSizeFactor = 5;
		readonly static int maxRecentFiles = 15;

		// emulator handler & co
		GraphicsHandler graphicsHandler = default;
		SoundHandler soundHandler = default;
		InputHandler inputHandler = default;
		EmulatorHandler emulatorHandler = default;

		// debug vars
		readonly bool debugSoundRecording = false, debugAutostartLastRom = false;

		// misc vars
		bool isBootstrapAvailable = false, isVertical = false;

		public MainForm()
		{
			//OpenTK.Windowing.GraphicsLibraryFramework.GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);		//TODO doublecheck if needed for dbg

			InitializeComponent();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			InitializeHandlers();
			VerifyConfiguration();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			CreateRecentFilesMenu();
			CreateScreenSizeMenu();

			InitializeUIMiscellanea();

			if (debugAutostartLastRom)
				LoadAndRunCartridge(Program.Configuration.General.RecentFiles.First());


			//var tmp = new SettingsForm(Program.Configuration);
			//tmp.ShowDialog();
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			renderControl.Focus();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveRam();
			emulatorHandler.Shutdown();

			Program.SaveConfiguration();

			if (soundHandler.IsRecording)
				soundHandler.SaveRecording(@"D:\Temp\Goose\sound.wav");
		}

		private void MainForm_Layout(object sender, LayoutEventArgs e)
		{
			MinimumSize = CalculateRequiredClientSize(2) + (Size - ClientSize);
		}

		private void InitializeHandlers()
		{
			//TODO: don't directly reference wonderswan class here...somehow
			graphicsHandler = new GraphicsHandler(Emulation.Machines.WonderSwan.Metadata) { IsVertical = isVertical };

			soundHandler = new SoundHandler(44100, 2);
			soundHandler.SetVolume(1.0f);
			soundHandler.SetMute(Program.Configuration.Sound.Mute);
			soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);

			if (debugSoundRecording)
				soundHandler.BeginRecording();

			inputHandler = new InputHandler(renderControl) { IsVertical = isVertical };
			inputHandler.SetKeyMapping(Program.Configuration.Input.Controls, Program.Configuration.Input.Hardware);

			emulatorHandler = new EmulatorHandler();
			emulatorHandler.RenderScreen += graphicsHandler.RenderScreen;
			emulatorHandler.EnqueueSamples += soundHandler.EnqueueSamples;
			emulatorHandler.PollInput += inputHandler.PollInput;
			emulatorHandler.StartOfFrame += (s, e) => { e.ToggleMasterVolume = inputHandler.GetMappedKeysPressed().Contains("volume"); };
			emulatorHandler.EndOfFrame += (s, e) => { /* anything to do here...? */ };

			renderControl.Paint += graphicsHandler.Paint;
			renderControl.Resize += graphicsHandler.Resize;
		}

		private void VerifyConfiguration()
		{
			var metadata = emulatorHandler.GetMetadata();

			isBootstrapAvailable = File.Exists(Program.Configuration.General.BootstrapFile);

			foreach (var button in metadata["machine/input/controls"].Value.StringArray)
			{
				if (!Program.Configuration.Input.Controls.ContainsKey(button) || !Enum.IsDefined(typeof(Keys), Program.Configuration.Input.Controls[button]))
					Program.Configuration.Input.Controls[button] = string.Empty;
			}

			foreach (var button in metadata["machine/input/hardware"].Value.StringArray)
			{
				if (!Program.Configuration.Input.Hardware.ContainsKey(button) || !Enum.IsDefined(typeof(Keys), Program.Configuration.Input.Hardware[button]))
					Program.Configuration.Input.Hardware[button] = string.Empty;
			}

			if (Program.Configuration.Video.ScreenSize < 2 || Program.Configuration.Video.ScreenSize >= maxScreenSizeFactor)
				Program.Configuration.Video.ResetToDefault(nameof(Program.Configuration.Video.ScreenSize));
		}

		private void SizeAndPositionWindow()
		{
			if (WindowState == FormWindowState.Maximized)
				WindowState = FormWindowState.Normal;

			ClientSize = CalculateRequiredClientSize(Program.Configuration.Video.ScreenSize);

			var screen = Screen.FromControl(this);
			var workingArea = screen.WorkingArea;
			Location = new Point()
			{
				X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - Width) / 2),
				Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - Height) / 2)
			};
		}

		private Size CalculateRequiredClientSize(int screenSize)
		{
			if (graphicsHandler == null)
				return ClientSize;

			var screenWidth = graphicsHandler.ScreenSize.X;
			var screenHeight = graphicsHandler.ScreenSize.Y + emulatorHandler.GetMetadata()["interface/icons/size"].Value.Integer;

			if (!isVertical)
				return new Size(
					screenWidth * screenSize,
					(screenHeight * screenSize) + menuStrip.Height + statusStrip.Height);
			else
				return new Size(
					screenHeight * screenSize,
					(screenWidth * screenSize) + menuStrip.Height + statusStrip.Height);
		}

		private void SetWindowTitleAndStatus()
		{
			var titleStringBuilder = new StringBuilder();

			titleStringBuilder.Append(GetProductNameAndVersionString(false));

			var metadata = emulatorHandler.GetMetadata();
			var cartridgeId = metadata["cartridge/id"].Value;

			if (cartridgeId != null)
			{
				titleStringBuilder.Append($" - [{Path.GetFileName(Program.Configuration.General.RecentFiles.First())}]");

				var statusStringBuilder = new StringBuilder();
				statusStringBuilder.Append($"Emulating {metadata["machine/description/manufacturer"].Value} {metadata["machine/description/model"].Value}, ");
				statusStringBuilder.Append($"playing {cartridgeId}");

				tsslStatus.Text = statusStringBuilder.ToString();
				tsslEmulationStatus.Text = emulatorHandler.IsRunning ? (emulatorHandler.IsPaused ? "Paused" : "Running") : "Stopped";
			}
			else
			{
				tsslStatus.Text = "Ready";
				tsslEmulationStatus.Text = "Stopped";
			}

			Text = titleStringBuilder.ToString();
		}

		private string GetProductNameAndVersionString(bool appendBuildName)
		{
			var titleStringBuilder = new StringBuilder();

			var version = new Version(Application.ProductVersion);
			var versionMinor = version.Minor != 0 ? $".{version.Minor}" : string.Empty;
			titleStringBuilder.Append($"{Application.ProductName} v{version.Major:D3}{versionMinor}");

			if (appendBuildName)
				titleStringBuilder.Append($" (unknown build)");

			return titleStringBuilder.ToString();
		}

		private void CreateRecentFilesMenu()
		{
			recentFilesToolStripMenuItem.DropDownItems.Clear();

			var clearMenuItem = new ToolStripMenuItem("&Clear List...");
			clearMenuItem.Click += (s, e) =>
			{
				Program.Configuration.General.RecentFiles.Clear();
				CreateRecentFilesMenu();
			};
			recentFilesToolStripMenuItem.DropDownItems.Add(clearMenuItem);
			recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

			for (int i = 0; i < maxRecentFiles; i++)
			{
				var file = i < Program.Configuration.General.RecentFiles.Count ? Program.Configuration.General.RecentFiles[i] : null;
				var menuItem = new ToolStripMenuItem(file != null ? file.Replace("&", "&&") : "-")
				{
					Enabled = file != null,
					Tag = file
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string filename)
						LoadAndRunCartridge(filename);
				};
				recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void AddToRecentFiles(string filename)
		{
			if (Program.Configuration.General.RecentFiles.Contains(filename))
			{
				var index = Program.Configuration.General.RecentFiles.IndexOf(filename);
				var newList = new List<string>(maxRecentFiles) { filename };
				newList.AddRange(Program.Configuration.General.RecentFiles.Where(x => x != filename));
				Program.Configuration.General.RecentFiles = newList;
			}
			else
			{
				Program.Configuration.General.RecentFiles.Insert(0, filename);
				if (Program.Configuration.General.RecentFiles.Count > maxRecentFiles)
					Program.Configuration.General.RecentFiles.RemoveAt(Program.Configuration.General.RecentFiles.Count - 1);
			}
		}

		private void CreateScreenSizeMenu()
		{
			screenSizeToolStripMenuItem.DropDownItems.Clear();

			for (int i = 2; i <= maxScreenSizeFactor; i++)
			{
				var menuItem = new ToolStripMenuItem($"{i}x")
				{
					Checked = Program.Configuration.Video.ScreenSize == i,
					Tag = i
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is int screenSizeFactor)
					{
						Program.Configuration.Video.ScreenSize = screenSizeFactor;
						SizeAndPositionWindow();

						foreach (ToolStripMenuItem screenSizeMenuItem in screenSizeToolStripMenuItem.DropDownItems)
							screenSizeMenuItem.Checked = (int)screenSizeMenuItem.Tag == Program.Configuration.Video.ScreenSize;
					}
				};
				screenSizeToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void InitializeUIMiscellanea()
		{
			// ... aka all the minor stuff I didn't want directly in the Load event, but also doesn't merit a separate function

			limitFPSToolStripMenuItem.Checked = emulatorHandler.LimitFps = Program.Configuration.General.LimitFps; // TODO databinding?
			ofdOpenRom.Filter = $"{emulatorHandler.GetMetadata()["interface/files/romfilter"]}|All Files (*.*)|*.*";
		}

		private void LoadBootstrap(string filename)
		{
			if (!emulatorHandler.IsRunning && isBootstrapAvailable && !emulatorHandler.IsBootstrapLoaded)
			{
				using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				emulatorHandler.LoadBootstrap(data);
			}
		}

		private void LoadAndRunCartridge(string filename)
		{
			if (emulatorHandler.IsRunning)
			{
				SaveRam();
				emulatorHandler.Shutdown();
			}

			using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			emulatorHandler.LoadRom(data);

			graphicsHandler.IsVertical = inputHandler.IsVertical = isVertical =
					emulatorHandler.GetMetadata()["cartridge/orientation"] == "vertical";

			AddToRecentFiles(filename);
			CreateRecentFilesMenu();

			LoadRam();

			LoadBootstrap(Program.Configuration.General.BootstrapFile);

			emulatorHandler.Startup();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			Program.SaveConfiguration();
		}

		private void LoadRam()
		{
			var path = Path.Combine(Program.SaveDataPath, Path.ChangeExtension(Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First()), ".sav"));
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0)
				emulatorHandler.LoadSaveData(data);
		}

		private void SaveRam()
		{
			var data = emulatorHandler.GetSaveData();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.SaveDataPath, Path.ChangeExtension(Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First()), ".sav"));

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void loadROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var lastFile = Program.Configuration.General.RecentFiles.FirstOrDefault();
			if (lastFile != string.Empty)
			{
				ofdOpenRom.FileName = Path.GetFileName(lastFile);
				ofdOpenRom.InitialDirectory = Path.GetDirectoryName(lastFile);
			}

			if (ofdOpenRom.ShowDialog() == DialogResult.OK)
			{
				LoadAndRunCartridge(ofdOpenRom.FileName);
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			emulatorHandler.Reset();
		}

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsRunning) return;

			(sender as ToolStripMenuItem).Checked = !emulatorHandler.IsPaused;
			emulatorHandler.Pause();
		}

		private void rotateScreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			graphicsHandler.IsVertical = inputHandler.IsVertical = isVertical =
				!isVertical;

			SizeAndPositionWindow();
		}

		private void limitFPSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			(sender as ToolStripMenuItem).Checked = !emulatorHandler.LimitFps;
			emulatorHandler.LimitFps = Program.Configuration.General.LimitFps = !emulatorHandler.LimitFps;
		}

		private void dumpRAMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			File.WriteAllBytes(@"D:\Temp\Goose\iram.bin", emulatorHandler?.GetInternalRam());
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show($"{GetProductNameAndVersionString(false)} by {Application.CompanyName}\n\nPrototype WIP build, should be safe to use, kinda.\n\nHONK!", $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

using StoicGoose.Emulation;
using StoicGoose.Emulation.Machines;
using StoicGoose.Extensions;
using StoicGoose.Handlers;
using StoicGoose.Interface;
using StoicGoose.OpenGL;

namespace StoicGoose
{
	public partial class MainForm : Form
	{
		/* Constants */
		readonly static int maxScreenSizeFactor = 5;
		readonly static int maxRecentFiles = 15;

		/* Log window */
		readonly ImGuiLogWindow logWindow = default;

		/* Various handlers */
		GraphicsHandler graphicsHandler = default;
		SoundHandler soundHandler = default;
		InputHandler inputHandler = default;
		ImGuiHandler imGuiHandler = default;
		EmulatorHandler emulatorHandler = default;

		/* Debugger stuff */
		DebuggerMainForm debuggerMainForm = default;

		/* Misc. runtime variables */
		Type machineType = default;
		readonly List<Binding> uiDataBindings = new();
		bool isVerticalOrientation = false;
		string internalEepromPath = string.Empty;

		public MainForm()
		{
			InitializeComponent();

			if (GlobalVariables.EnableConsoleOutput && (logWindow = new()) != null)
			{
				logWindow.IsWindowOpen = true;

				Console.SetOut(logWindow.TextWriter);
				Console.WriteLine($"{Application.ProductName} {Program.GetVersionString(true)}");
				Console.WriteLine("HONK, HONK, pork cheek!");
			}

			if (GlobalVariables.EnableOpenGLDebug)
			{
				GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
				renderControl.Flags |= ContextFlags.Debug;
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			machineType = Program.Configuration.General.PreferOriginalWS ? typeof(WonderSwan) : typeof(WonderSwanColor);

			InitializeHandlers();
			VerifyConfiguration();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			CreateRecentFilesMenu();
			CreateScreenSizeMenu();
			CreateShaderMenu();

			InitializeUIMiscellanea();

			InitializeDebugger();

			if (GlobalVariables.EnableAutostartLastRom)
				LoadAndRunCartridge(Program.Configuration.General.RecentFiles.First());

			if (GlobalVariables.IsAuthorsMachine && GlobalVariables.EnableDebugNewUIStuffs)
			{
				openDebuggerToolStripMenuItem_Click(openDebuggerToolStripMenuItem, EventArgs.Empty);
				PauseEmulation();
				ResetEmulation();
			}

			if (true)
			{
				Console.WriteLine("OpenGL context info");
				Console.WriteLine($" Version: {ContextInfo.GLVersion}");
				Console.WriteLine($" Vendor: {ContextInfo.GLVendor}");
				Console.WriteLine($" Renderer: {ContextInfo.GLRenderer}");
				Console.WriteLine($" GLSL version: {ContextInfo.GLShadingLanguageVersion}");
				Console.WriteLine($" {ContextInfo.GLExtensions.Length} extensions supported");
			}
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			renderControl.Focus();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveInternalEeprom();
			SaveRam();
			SaveCheatList();

			emulatorHandler.Shutdown();

			Program.SaveConfiguration();

			if (GlobalVariables.EnableLocalDebugIO && soundHandler.IsRecording)
				soundHandler.SaveRecording(@"D:\Temp\Goose\sound.wav");
		}

		private void MainForm_Layout(object sender, LayoutEventArgs e)
		{
			MinimumSize = CalculateRequiredClientSize(2) + (Size - ClientSize);
		}

		private void InitializeHandlers()
		{
			emulatorHandler = new EmulatorHandler(machineType);
			emulatorHandler.SetFpsLimiter(Program.Configuration.General.LimitFps);

			graphicsHandler = new GraphicsHandler(emulatorHandler.Machine.Metadata) { IsVerticalOrientation = isVerticalOrientation };

			soundHandler = new SoundHandler(44100, 2);
			soundHandler.SetVolume(1.0f);
			soundHandler.SetMute(Program.Configuration.Sound.Mute);
			soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);

			if (GlobalVariables.EnableDebugSoundRecording)
				soundHandler.BeginRecording();

			inputHandler = new InputHandler(renderControl) { IsVerticalOrientation = isVerticalOrientation };
			inputHandler.SetKeyMapping(Program.Configuration.Input.GameControls, Program.Configuration.Input.SystemControls);

			imGuiHandler = new ImGuiHandler(renderControl);

			emulatorHandler.Machine.UpdateScreen += graphicsHandler.UpdateScreen;
			emulatorHandler.Machine.EnqueueSamples += soundHandler.EnqueueSamples;
			emulatorHandler.Machine.PollInput += inputHandler.PollInput;
			emulatorHandler.Machine.StartOfFrame += (s, e) => { e.ToggleMasterVolume = inputHandler.GetMappedKeysPressed().Contains("volume"); };
			emulatorHandler.Machine.EndOfFrame += (s, e) => { /* anything to do here...? */ };

			renderControl.Resize += (s, e) => { if (s is Control control) imGuiHandler.Resize(control.ClientSize.Width, control.ClientSize.Height); };
			renderControl.Resize += (s, e) => { if (s is Control control) graphicsHandler.Resize(control.ClientRectangle); };
			renderControl.Paint += (s, e) =>
			{
				imGuiHandler.BeginFrame();
				graphicsHandler.DrawFrame();
				emulatorHandler.Machine.DrawImGuiWindows();
				logWindow.Draw();
				imGuiHandler.EndFrame();
			};

			internalEepromPath = Path.Combine(Program.InternalDataPath, emulatorHandler.Machine.Metadata["machine/eeprom/filename"]);
		}

		private void VerifyConfiguration()
		{
			var metadata = emulatorHandler.Machine.Metadata;

			foreach (var button in metadata["machine/input/controls"].StringArray)
			{
				if (!Program.Configuration.Input.GameControls.ContainsKey(button) || !Enum.IsDefined(typeof(Keys), Program.Configuration.Input.GameControls[button]))
					Program.Configuration.Input.GameControls[button] = string.Empty;
			}

			foreach (var button in metadata["machine/input/hardware"].StringArray)
			{
				if (!Program.Configuration.Input.SystemControls.ContainsKey(button) || !Enum.IsDefined(typeof(Keys), Program.Configuration.Input.SystemControls[button]))
					Program.Configuration.Input.SystemControls[button] = string.Empty;
			}

			if (Program.Configuration.Video.ScreenSize < 2 || Program.Configuration.Video.ScreenSize > maxScreenSizeFactor)
				Program.Configuration.Video.ResetToDefault(nameof(Program.Configuration.Video.ScreenSize));

			if (Program.Configuration.Video.Shader == string.Empty || !graphicsHandler.AvailableShaders.Contains(Program.Configuration.Video.Shader))
				Program.Configuration.Video.Shader = GraphicsHandler.DefaultShaderName;
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
			var screenHeight = graphicsHandler.ScreenSize.Y + emulatorHandler.Machine.Metadata["interface/icons/size"].Integer;

			if (!isVerticalOrientation)
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

			titleStringBuilder.Append($"{Application.ProductName} {Program.GetVersionString(false)}");

			var metadata = emulatorHandler.Machine.Metadata;
			var cartridgeId = metadata.GetValueOrDefault("cartridge/id");

			if (cartridgeId != null)
			{
				titleStringBuilder.Append($" - [{Path.GetFileName(Program.Configuration.General.RecentFiles.First())}]");

				var statusStringBuilder = new StringBuilder();
				statusStringBuilder.Append($"Emulating {metadata["machine/description/manufacturer"]} {metadata["machine/description/model"]}, ");
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

		private void CreateShaderMenu()
		{
			shadersToolStripMenuItem.DropDownItems.Clear();

			foreach (var shaderName in graphicsHandler.AvailableShaders)
			{
				var menuItem = new ToolStripMenuItem(shaderName)
				{
					Checked = shaderName == Program.Configuration.Video.Shader,
					Tag = shaderName
				};
				menuItem.Click += (s, e) =>
				{
					if ((s as ToolStripMenuItem).Tag is string shader)
					{
						graphicsHandler?.ChangeShader(shader);

						Program.Configuration.Video.Shader = shader;
						foreach (ToolStripMenuItem shaderMenuItem in shadersToolStripMenuItem.DropDownItems)
							shaderMenuItem.Checked = (shaderMenuItem.Tag as string) == Program.Configuration.Video.Shader;
					}
				};
				shadersToolStripMenuItem.DropDownItems.Add(menuItem);
			}
		}

		private void CreateDataBinding(ControlBindingsCollection bindings, string propertyName, object dataSource, string dataMember)
		{
			var binding = new Binding(propertyName, dataSource, dataMember, false, DataSourceUpdateMode.OnPropertyChanged);
			bindings.Add(binding);
			uiDataBindings.Add(binding);
		}

		private void InitializeUIMiscellanea()
		{
			// ... aka all the minor stuff I didn't want directly in the Load event, but also doesn't merit a separate function

			uiDataBindings.Clear();

			CreateDataBinding(limitFPSToolStripMenuItem.DataBindings, nameof(limitFPSToolStripMenuItem.Checked), Program.Configuration.General, nameof(Program.Configuration.General.LimitFps));
			limitFPSToolStripMenuItem.CheckedChanged += (s, e) => { emulatorHandler.SetFpsLimiter(Program.Configuration.General.LimitFps); };

			CreateDataBinding(muteSoundToolStripMenuItem.DataBindings, nameof(muteSoundToolStripMenuItem.Checked), Program.Configuration.Sound, nameof(Program.Configuration.Sound.Mute));
			muteSoundToolStripMenuItem.CheckedChanged += (s, e) => { soundHandler.SetMute(Program.Configuration.Sound.Mute); };

			ofdOpenRom.Filter = $"{emulatorHandler.Machine.Metadata["interface/files/romfilter"]}|All Files (*.*)|*.*";
		}

		private void InitializeDebugger()
		{
			debuggerMainForm = new DebuggerMainForm(emulatorHandler, PauseEmulation, UnpauseEmulation, ResetEmulation);
		}

		private void LoadBootstrap(string filename)
		{
			if (GlobalVariables.EnableSkipBootstrapIfFound) return;

			if (!emulatorHandler.IsRunning &&
				Program.Configuration.General.UseBootstrap && File.Exists(filename) && !emulatorHandler.Machine.IsBootstrapLoaded)
			{
				using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				emulatorHandler.Machine.LoadBootstrap(data);
			}
		}

		private void LoadInternalEeprom()
		{
			if (!emulatorHandler.IsRunning && File.Exists(internalEepromPath))
			{
				using var stream = new FileStream(internalEepromPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				emulatorHandler.Machine.LoadInternalEeprom(data);
			}
		}

		private void LoadAndRunCartridge(string filename)
		{
			if (emulatorHandler.IsRunning)
			{
				SaveInternalEeprom();
				SaveRam();
				SaveCheatList();

				emulatorHandler.Shutdown();
			}

			using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			emulatorHandler.Machine.LoadRom(data);

			graphicsHandler.IsVerticalOrientation = inputHandler.IsVerticalOrientation = isVerticalOrientation =
				emulatorHandler.Machine.Metadata.GetValueOrDefault("cartridge/orientation") == "vertical";

			AddToRecentFiles(filename);
			CreateRecentFilesMenu();

			LoadRam();
			LoadCheatList();

			LoadBootstrap(emulatorHandler.Machine is WonderSwan ? Program.Configuration.General.BootstrapFile : Program.Configuration.General.BootstrapFileWSC);
			LoadInternalEeprom();

			emulatorHandler.Startup();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			Program.SaveConfiguration();
		}

		private void LoadRam()
		{
			var path = Path.Combine(Program.SaveDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.sav");
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0)
				emulatorHandler.Machine.LoadSaveData(data);
		}

		private void LoadCheatList()
		{
			var path = Path.Combine(Program.CheatsDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.json");
			emulatorHandler.Machine.LoadCheatList(File.Exists(path) ? path.DeserializeFromFile<List<MachineCommon.Cheat>>() : new List<MachineCommon.Cheat>());
		}

		private void SaveRam()
		{
			var data = emulatorHandler.Machine.GetSaveData();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.SaveDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.sav");

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void SaveCheatList()
		{
			var data = emulatorHandler.Machine.GetCheatList();
			if (data.Count == 0) return;

			var path = Path.Combine(Program.CheatsDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.json");
			data.SerializeToFile(path);
		}

		private void SaveInternalEeprom()
		{
			var data = emulatorHandler.Machine.GetInternalEeprom();
			if (data.Length == 0) return;

			using var stream = new FileStream(internalEepromPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void PauseEmulation()
		{
			if (!emulatorHandler.IsRunning) return;

			emulatorHandler.Pause();
			soundHandler.Pause();

			SetWindowTitleAndStatus();
		}

		private void UnpauseEmulation()
		{
			if (!emulatorHandler.IsRunning) return;

			emulatorHandler.Unpause();
			soundHandler.Unpause();

			SetWindowTitleAndStatus();
		}

		private void ResetEmulation()
		{
			SaveInternalEeprom();
			SaveRam();
			SaveCheatList();

			emulatorHandler.Reset();

			Program.SaveConfiguration();
		}

		private void loadROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PauseEmulation();

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

			UnpauseEmulation();
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ResetEmulation();
		}

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsRunning) return;

			if (sender is ToolStripMenuItem pauseMenuItem)
			{
				if (!pauseMenuItem.Checked)
				{
					PauseEmulation();
					pauseMenuItem.Checked = true;
				}
				else
				{
					UnpauseEmulation();
					pauseMenuItem.Checked = false;
				}
			}
		}

		private void rotateScreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			graphicsHandler.IsVerticalOrientation = inputHandler.IsVerticalOrientation = isVerticalOrientation =
				!isVerticalOrientation;

			SizeAndPositionWindow();
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PauseEmulation();

			using var dialog = new SettingsForm(Program.Configuration.Clone());
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				var requiresRestart = dialog.Configuration.General.PreferOriginalWS != Program.Configuration.General.PreferOriginalWS;

				Program.ReplaceConfiguration(dialog.Configuration);
				VerifyConfiguration();

				foreach (var binding in uiDataBindings) binding.ReadValue();

				if (requiresRestart)
				{
					if (MessageBox.Show("Changing the system preference setting requires a restart of the emulator to take effect.\n\nDo you want to restart the emulator now?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						Application.Restart();
						Environment.Exit(0);
					}
				}

				inputHandler.SetKeyMapping(Program.Configuration.Input.GameControls, Program.Configuration.Input.SystemControls);
			}

			UnpauseEmulation();
		}

		private void openDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			debuggerMainForm.Show();
		}

		private void traceLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsRunning) return;

			if (sender is ToolStripMenuItem traceLogMenuItem)
			{
				if (!traceLogMenuItem.Checked)
				{
					PauseEmulation();

					if (MessageBox.Show("Trace logs are highly verbose and can consume a lot of storage space, even during short sessions.\n\nDo you still want to continue and enable logging?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
					{
						emulatorHandler.Machine.BeginTraceLog(Path.Combine(Program.DebuggingDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}-{DateTime.Now:yyyyMMdd-HHmmss}.log"));
						traceLogMenuItem.Checked = true;
					}

					UnpauseEmulation();
				}
				else
				{
					emulatorHandler.Machine.EndTraceLog();
					traceLogMenuItem.Checked = false;
				}
			}
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PauseEmulation();

			MessageBox.Show($"{Application.ProductName} {Program.GetVersionString(true)} by {Application.CompanyName}\n\n{ThisAssembly.Git.RepositoryUrl}\n\nPrototype WIP build, should be safe to use, kinda.{(GlobalVariables.IsDebugBuild ? "\n\nThis is a HONK!ing debug build! 🔪🦢🖥, y'all!" : string.Empty)}", $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);

			UnpauseEmulation();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

using ImGuiNET;

using StoicGoose.Common.Console;
using StoicGoose.Common.OpenGL;
using StoicGoose.Core.Machines;
//using StoicGoose.Debugging;
using StoicGoose.Extensions;
using StoicGoose.Handlers;
using StoicGoose.Interface.Windows;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose
{
	// TODO restore cheats, breakpoints

	public partial class MainForm : Form
	{
		/* Constants */
		readonly static int maxScreenSizeFactor = 5;
		readonly static int maxRecentFiles = 15;

		/* Log window */
		readonly ImGuiLogWindow logWindow = default;

		/* Various handlers */
		DatabaseHandler databaseHandler = default;
		GraphicsHandler graphicsHandler = default;
		SoundHandler soundHandler = default;
		InputHandler inputHandler = default;
		ImGuiHandler imGuiHandler = default;
		EmulatorHandler emulatorHandler = default;

		/* Misc. windows */
		SoundRecorderForm soundRecorderForm = default;

		/* Misc. runtime variables */
		bool isInitialized = false;
		Type machineType = default;
		readonly List<Binding> uiDataBindings = new();
		bool isVerticalOrientation = false;
		string internalEepromPath = string.Empty;

		public MainForm()
		{
			InitializeComponent();

			if ((logWindow = new()) != null)
			{
				Console.SetOut(logWindow.TextWriter);
				Console.WriteLine($"{Ansi.Green}{Application.ProductName} {Program.GetVersionString(true)}");
				Console.WriteLine("HONK, HONK, pork cheek!");
				Console.WriteLine("----------------------------------------");
			}

			if (GlobalVariables.EnableOpenGLDebug)
			{
				GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
				renderControl.Flags |= ContextFlags.Debug;

				ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, "Enabled OpenGL debugging.");
			}

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Constructor done.");
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Initializing emulator and UI...");

			machineType = Program.Configuration.General.PreferOriginalWS ? typeof(WonderSwan) : typeof(WonderSwanColor);

			InitializeEmulatorHandler();
			VerifyConfiguration();
			InitializeOtherHandlers();
			InitializeWindows();

			SizeAndPositionWindow();
			SetWindowTitleAndStatus();

			CreateRecentFilesMenu();
			CreateScreenSizeMenu();
			CreateShaderMenu();

			InitializeUIMiscellanea();

			if (GlobalVariables.IsDebugBuild && GlobalVariables.EnableSuperVerbosity)
			{
				Console.WriteLine($"~ {Ansi.Cyan}Global variables{Ansi.Reset} ~");
				foreach (var var in GlobalVariables.Dump()) Console.WriteLine($" {var}");
			}

			if (GlobalVariables.EnableOpenGLDebug)
			{
				Console.WriteLine($"~ {Ansi.Yellow}OpenGL debugging enabled{Ansi.Reset} ~");
				if (GlobalVariables.EnableSuperVerbosity)
				{
					Console.WriteLine($"~ {Ansi.Cyan}GL context info{Ansi.Reset} ~");
					Console.WriteLine($" Version: {ContextInfo.GLVersion}");
					Console.WriteLine($" Vendor: {ContextInfo.GLVendor}");
					Console.WriteLine($" Renderer: {ContextInfo.GLRenderer}");
					Console.WriteLine($" GLSL version: {ContextInfo.GLShadingLanguageVersion}");
					Console.WriteLine($" {ContextInfo.GLExtensions.Length} extensions supported");
				}
			}

			if (GlobalVariables.IsAuthorsMachine && GlobalVariables.EnableSuperVerbosity)
			{
				Console.WriteLine();
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.White}########################################");
				Console.WriteLine($"{Ansi.White}########################################");
				Console.WriteLine($"{Ansi.White}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.Magenta}########################################");
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine($"{Ansi.Cyan}########################################");
				Console.WriteLine();
				Console.WriteLine($"Ze goose sez: {Ansi.Cyan}TRANS {Ansi.Magenta}RIGHTS {Ansi.White}ARE {Ansi.Magenta}HUMAN {Ansi.Cyan}RIGHTS{Ansi.Reset}!");
				Console.WriteLine();
			}

			if (GlobalVariables.EnableAutostartLastRom)
				LoadAndRunCartridge(Program.Configuration.General.RecentFiles.First());

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Initialization done!");

			isInitialized = true;
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			renderControl.Focus();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			SaveAllData();
			emulatorHandler.Shutdown();

			Program.SaveConfiguration();
		}

		private void InitializeEmulatorHandler()
		{
			emulatorHandler = new EmulatorHandler(machineType);
			emulatorHandler.SetFpsLimiter(Program.Configuration.General.LimitFps);
		}

		private void VerifyConfiguration()
		{
			var metadata = emulatorHandler.Machine.Metadata;

			foreach (var button in metadata.GameControls.Replace(" ", "").Split(','))
			{
				if (!Program.Configuration.Input.GameControls.ContainsKey(button))
					Program.Configuration.Input.GameControls[button] = new();
			}

			foreach (var button in metadata.HardwareControls.Replace(" ", "").Split(','))
			{
				if (!Program.Configuration.Input.SystemControls.ContainsKey(button))
					Program.Configuration.Input.SystemControls[button] = new();
			}

			if (Program.Configuration.Video.ScreenSize < 2 || Program.Configuration.Video.ScreenSize > maxScreenSizeFactor)
				Program.Configuration.Video.ResetToDefault(nameof(Program.Configuration.Video.ScreenSize));

			if (string.IsNullOrEmpty(Program.Configuration.Video.Shader) || (graphicsHandler != null && !graphicsHandler.AvailableShaders.Contains(Program.Configuration.Video.Shader)))
				Program.Configuration.Video.Shader = GraphicsHandler.DefaultShaderName;
		}

		private void InitializeOtherHandlers()
		{
			databaseHandler = new DatabaseHandler(Program.NoIntroDatPath);

			graphicsHandler = new GraphicsHandler(emulatorHandler.Machine.Metadata, Program.Configuration.Video.Shader) { IsVerticalOrientation = isVerticalOrientation };

			soundHandler = new SoundHandler(44100, 2);
			soundHandler.SetVolume(1.0f);
			soundHandler.SetMute(Program.Configuration.Sound.Mute);
			soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);

			inputHandler = new InputHandler(renderControl) { IsVerticalOrientation = isVerticalOrientation };
			inputHandler.SetKeyMapping(Program.Configuration.Input.GameControls, Program.Configuration.Input.SystemControls);
			inputHandler.SetVerticalRemapping(emulatorHandler.Machine.Metadata.VerticalControlRemap
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.ToDictionary(x => x[0], x => x[1]));

			imGuiHandler = new ImGuiHandler(renderControl);
			imGuiHandler.RegisterWindow(new ImGuiScreenWindow() { IsWindowOpen = Program.Configuration.Debugging.StartInDebugUI }, () => (graphicsHandler.DisplayTexture, isVerticalOrientation));

			var disassemblerWindow = new ImGuiDisassemblerWindow() { IsWindowOpen = Program.Configuration.Debugging.StartInDebugUI };
			disassemblerWindow.PauseEmulation += (s, e) => { PauseEmulation(); };
			disassemblerWindow.UnpauseEmulation += (s, e) => { UnpauseEmulation(); };
			imGuiHandler.RegisterWindow(disassemblerWindow, () => emulatorHandler);
			imGuiHandler.RegisterWindow(new ImGuiMemoryWindow(), () => emulatorHandler);
			imGuiHandler.RegisterWindow(new ImGuiMachineStatusWindow($"{emulatorHandler.Machine.Metadata.Model} System", machineType), () => emulatorHandler.Machine);
			imGuiHandler.RegisterWindow(new ImGuiDisplayStatusWindow($"{emulatorHandler.Machine.Metadata.Model} Display Controller", emulatorHandler.Machine.DisplayController.GetType()), () => emulatorHandler.Machine.DisplayController);
			imGuiHandler.RegisterWindow(new ImGuiSoundStatusWindow($"{emulatorHandler.Machine.Metadata.Model} Sound Controller", emulatorHandler.Machine.SoundController.GetType()), () => emulatorHandler.Machine.SoundController);
			imGuiHandler.RegisterWindow(new ImGuiSoundVisualizerWindow() { IsWindowOpen = true }, () => emulatorHandler.Machine.SoundController);

			emulatorHandler.Machine.DisplayController.SendFramebuffer = graphicsHandler.UpdateScreen;
			emulatorHandler.Machine.SoundController.SendSamples = soundHandler.EnqueueSamples;

			emulatorHandler.Machine.ReceiveInput += () =>
			{
				var buttonsPressed = new List<string>();
				var buttonsHeld = new List<string>();

				var screenWindow = imGuiHandler.GetWindow<ImGuiScreenWindow>();
				if ((screenWindow.IsWindowOpen && screenWindow.IsFocused) || (!screenWindow.IsWindowOpen && !ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow)))
					inputHandler.PollInput(ref buttonsPressed, ref buttonsHeld);

				if (buttonsPressed.Contains("Volume"))
					emulatorHandler.Machine.SoundController.ChangeMasterVolume();

				return (buttonsPressed, buttonsHeld);
			};

			renderControl.Resize += (s, e) => { if (s is Control control) imGuiHandler.Resize(control.ClientSize.Width, control.ClientSize.Height); };
			renderControl.Resize += (s, e) => { if (s is Control control) graphicsHandler.Resize(control.ClientRectangle); };
			renderControl.Paint += (s, e) =>
			{
				var isScreenWindowOpen = imGuiHandler.GetWindow<ImGuiScreenWindow>().IsWindowOpen;

				imGuiHandler.BeginFrame();

				if (!isScreenWindowOpen) graphicsHandler.SetClearColor(Color.Black);
				else graphicsHandler.SetClearColor(Color.FromArgb(0x3E, 0x4F, 0x65)); // 🧲

				graphicsHandler.ClearFrame();

				if (!isScreenWindowOpen) graphicsHandler.DrawFrame();
				else graphicsHandler.BindTextures();

				logWindow.Draw(null);
				imGuiHandler.EndFrame();
			};

			internalEepromPath = Path.Combine(Program.InternalDataPath, emulatorHandler.Machine.Metadata.InternalEepromFilename);
		}

		private void InitializeWindows()
		{
			soundRecorderForm = new(soundHandler.SampleRate, soundHandler.NumChannels);

			//emulatorHandler.Machine.SoundController.EnqueueSamples += soundRecorderForm.EnqueueSamples;
		}

		private void SizeAndPositionWindow()
		{
			if (imGuiHandler.GetWindow<ImGuiScreenWindow>().IsWindowOpen && isInitialized) return;

			if (WindowState == FormWindowState.Maximized)
				WindowState = FormWindowState.Normal;

			MinimumSize = SizeFromClientSize(CalculateRequiredClientSize(2));
			Size = SizeFromClientSize(CalculateRequiredClientSize(Program.Configuration.Video.ScreenSize));

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
			if (emulatorHandler == null || graphicsHandler == null)
				return ClientSize;

			var statusIconsOnRight = emulatorHandler.Machine.Metadata.StatusIconsLocation.X > emulatorHandler.Machine.Metadata.StatusIconsLocation.Y;

			int screenWidth, screenHeight;

			if (!isVerticalOrientation)
			{
				screenWidth = emulatorHandler.Machine.Metadata.ScreenSize.X;
				screenHeight = emulatorHandler.Machine.Metadata.ScreenSize.Y;
				if (statusIconsOnRight) screenWidth += emulatorHandler.Machine.Metadata.StatusIconSize;
				if (!statusIconsOnRight) screenHeight += emulatorHandler.Machine.Metadata.StatusIconSize;
			}
			else
			{
				screenWidth = emulatorHandler.Machine.Metadata.ScreenSize.Y;
				screenHeight = emulatorHandler.Machine.Metadata.ScreenSize.X;
				if (!statusIconsOnRight) screenWidth += emulatorHandler.Machine.Metadata.StatusIconSize;
				if (statusIconsOnRight) screenHeight += emulatorHandler.Machine.Metadata.StatusIconSize;
			}

			return new(screenWidth * screenSize, (screenHeight * screenSize) + menuStrip.Height + statusStrip.Height);
		}

		private void SetWindowTitleAndStatus()
		{
			var titleStringBuilder = new StringBuilder();

			titleStringBuilder.Append($"{Application.ProductName} {Program.GetVersionString(false)}");

			if (emulatorHandler.Machine.Cartridge.IsLoaded)
			{
				titleStringBuilder.Append($" - [{Path.GetFileName(Program.Configuration.General.RecentFiles.First())}]");

				var statusStringBuilder = new StringBuilder();
				statusStringBuilder.Append($"Emulating {emulatorHandler.Machine.Metadata.Manufacturer} {emulatorHandler.Machine.Metadata.Model}, ");
				statusStringBuilder.Append($"playing {databaseHandler.GetGameTitle(emulatorHandler.Machine.Cartridge.Crc32, emulatorHandler.Machine.Cartridge.SizeInBytes)} ({emulatorHandler.Machine.Cartridge.Metadata.GameIdString})");

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

		private static void AddToRecentFiles(string filename)
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

			CreateDataBinding(enableCheatsToolStripMenuItem.DataBindings, nameof(enableCheatsToolStripMenuItem.Checked), Program.Configuration.General, nameof(Program.Configuration.General.EnableCheats));

			CreateDataBinding(enableBreakpointsToolStripMenuItem.DataBindings, nameof(enableBreakpointsToolStripMenuItem.Checked), Program.Configuration.Debugging, nameof(Program.Configuration.Debugging.EnableBreakpoints));

			ofdOpenRom.Filter = $"{emulatorHandler.Machine.Metadata.RomFileFilter}|All Files (*.*)|*.*";
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
				SaveAllData();
				emulatorHandler.Shutdown();
			}

			using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			emulatorHandler.Machine.LoadRom(data);

			graphicsHandler.IsVerticalOrientation = inputHandler.IsVerticalOrientation = isVerticalOrientation =
				emulatorHandler.Machine.Cartridge.Metadata.Orientation == CartridgeMetadata.Orientations.Vertical;

			AddToRecentFiles(filename);
			CreateRecentFilesMenu();

			LoadRam();

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

		private void SaveAllData()
		{
			SaveInternalEeprom();
			SaveRam();
		}

		private void SaveRam()
		{
			var data = emulatorHandler.Machine.GetSaveData();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.SaveDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.sav");

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
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
			SaveAllData();
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

		private void saveWAVToolStripMenuItem_Click(object sender, EventArgs e)
		{
			soundRecorderForm.Show();
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
				soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);
			}

			UnpauseEmulation();
		}

		private void cheatListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//
		}

		private void disassemblerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiDisassemblerWindow>().IsWindowOpen = true;
		}

		private void screenWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiScreenWindow>().IsWindowOpen = true;
		}

		private void memoryEditorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiMemoryWindow>().IsWindowOpen = true;
		}

		private void systemRegistersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiMachineStatusWindow>().IsWindowOpen = true;
		}

		private void displayRegistersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiDisplayStatusWindow>().IsWindowOpen = true;
		}

		private void soundRegistersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			imGuiHandler.GetWindow<ImGuiSoundStatusWindow>().IsWindowOpen = true;
		}

		private void breakpointsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//
		}

		private void logWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			logWindow.IsWindowOpen = true;
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

			var builder = new StringBuilder();
			builder.AppendLine($"{Application.ProductName} {Program.GetVersionString(true)}");
			builder.AppendLine($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}");
			builder.AppendLine();
			builder.AppendLine($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}");
			builder.AppendLine($"{ThisAssembly.Git.RepositoryUrl}");
			if (GlobalVariables.IsDebugBuild)
			{
				builder.AppendLine();
				builder.AppendLine("This is a HONK!ing debug build! 🔪🦢🖥, y'all!");
			}
			MessageBox.Show(builder.ToString(), $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);

			UnpauseEmulation();
		}
	}
}

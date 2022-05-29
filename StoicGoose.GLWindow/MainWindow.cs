using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using StoicGoose.Common.Console;
using StoicGoose.Common.Extensions;
using StoicGoose.Common.OpenGL;
using StoicGoose.Core.Interfaces;
using StoicGoose.GLWindow.Debugging;
using StoicGoose.GLWindow.Interface.Handlers;
using StoicGoose.GLWindow.Interface.Windows;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.GLWindow
{
	public partial class MainWindow : GameWindow
	{
		/* Constants */
		const int maxMemoryPatches = 256;
		const int maxBreakpoints = 256;

		/* UI */
		readonly LogWindow logWindow = default;
		ImGuiHandler imGuiHandler = default;

		/* Graphics */
		readonly State renderState = new();
		byte[] initialScreenImage = default;

		/* Sound */
		SoundHandler soundHandler = default;

		/* Input */
		InputHandler inputHandler = default;

		/* Emulation */
		IMachine machine = default;
		Texture displayTexture = default;
		double frameTimeElapsed = 0.0;
		readonly MemoryPatch[] memoryPatches = new MemoryPatch[maxMemoryPatches];
		readonly Breakpoint[] breakpoints = new Breakpoint[maxBreakpoints];
		BreakpointVariables breakpointVariables = default;
		Breakpoint lastBreakpointHit = default;

		/* Misc. runtime variables */
		string bootstrapFilename = default, cartridgeFilename = default, cartSaveFilename = default, cartPatchFilename = default, cartBreakpointFilename = default;
		bool isRunning = false, isPaused = false, isVerticalOrientation = false;
		double framesPerSecond = 0.0;

		/* Easter egg variables */
		readonly Random random = new(Guid.NewGuid().GetHashCode());

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
			if ((logWindow = new()) != null)
			{
				Console.SetOut(logWindow.TextWriter);

				PrintStartupMessage();
				PrintAssemblies();
			}
		}

		protected override void OnLoad()
		{
			InitializeUI();

			soundHandler = new(44100, 2);
			inputHandler = new();

			imGuiHandler = new(this);
			imGuiHandler.RegisterWindow(logWindow, () => null);
			imGuiHandler.RegisterWindow(displayWindow, () => (displayTexture, isVerticalOrientation));
			imGuiHandler.RegisterWindow(disassemblerWindow, () => (machine, isRunning, isPaused));
			imGuiHandler.RegisterWindow(breakpointWindow, () => (breakpoints, isRunning));
			imGuiHandler.RegisterWindow(memoryEditorWindow, () => (machine, isRunning));
			imGuiHandler.RegisterWindow(systemControllerStatusWindow, () => machine);
			imGuiHandler.RegisterWindow(displayControllerStatusWindow, () => machine.DisplayController);
			imGuiHandler.RegisterWindow(soundControllerStatusWindow, () => machine.SoundController);
			imGuiHandler.RegisterWindow(memoryPatchWindow, () => (memoryPatches, isRunning));
			imGuiHandler.RegisterWindow(inputSettingsWindow, () => (Program.Configuration.GameControls, Program.Configuration.SystemControls));

			foreach (var windowTypeName in Program.Configuration.WindowsToRestore)
			{
				var type = Type.GetType(windowTypeName);
				if (type == null || imGuiHandler.GetWindow(type) is not WindowBase window) continue;
				window.IsWindowOpen = true;
			}

			renderState.SetClearColor(System.Drawing.Color.FromArgb(0x3E, 0x4F, 0x65)); // 🧲
			renderState.Enable(EnableCap.Blend);
			renderState.SetBlending(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

			initialScreenImage = new byte[244 * 144 * 4];
			for (var i = 0; i < initialScreenImage.Length; i += 4) initialScreenImage[i + 3] = 0xFF;

			soundHandler.SetMute(Program.Configuration.Mute);

			inputHandler.SetGameWindow(this);
			inputHandler.SetKeyMapping(Program.Configuration.GameControls, Program.Configuration.SystemControls);

			CreateMachine(Program.Configuration.PreferredSystem);

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, $"{nameof(OnLoad)} override finished.");

			base.OnLoad();
		}

		protected override void OnUnload()
		{
			DestroyMachine();

			Program.Configuration.WindowsToRestore = imGuiHandler.OpenWindows.Select(x => x.GetType().FullName).ToList();

			Program.SaveConfiguration();

			/* Ensure imgui.ini gets written */
			ImGuiNET.ImGui.SaveIniSettingsToDisk(ImGuiNET.ImGui.GetIO().IniFilename);

			soundHandler.Dispose();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, $"{nameof(OnUnload)} override finished.");

			base.OnUnload();
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			renderState.SetViewport(0, 0, e.Width, e.Height);

			imGuiHandler.Resize(e.Width, e.Height);

			base.OnResize(e);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			var keyState = KeyboardState.GetSnapshot();
			if (keyState.IsKeyDown(Keys.Escape)) Close();

			frameTimeElapsed += args.Time;

			if (frameTimeElapsed >= 1.0 / machine.RefreshRate || !Program.Configuration.LimitFps)
			{
				if (isRunning && !isPaused &&
					!fileDialogHandler.IsAnyDialogOpen && !messageBoxHandler.IsAnyMessageBoxOpen)
				{
					lastBreakpointHit = null;

					machine.RunFrame();
					soundHandler.Update();

					framesPerSecond = 1.0 / frameTimeElapsed;
				}
				else if (!isRunning)
					framesPerSecond = 0.0;

				frameTimeElapsed = 0.0;
			}

			statusRunningItem.Label = isPaused ? "Paused" : (isRunning ? "Running" : "Stopped");
			statusRunningItem.IsEnabled = isRunning && !isPaused;

			statusFpsItem.Label = $"{framesPerSecond:0} fps";
			statusFpsItem.IsEnabled = isRunning && !isPaused;

			base.OnUpdateFrame(args);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			renderState.Submit();

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			imGuiHandler.BeginFrame((float)args.Time);
			{
				backgroundGoose.Draw();

				menuHandler.Draw();
				messageBoxHandler.Draw();
				StatusBarHandler.Draw(statusMessageItem, statusRunningItem, statusFpsItem);
				fileDialogHandler.Draw();

				displayTexture?.Bind();
			}
			imGuiHandler.EndFrame();

			SwapBuffers();

			base.OnRenderFrame(args);
		}

		private void PrintStartupMessage()
		{
			var message = $"{Program.ProductName} {Program.GetVersionString(true)}";
			void printDefaultMessage() => Console.WriteLine($"{Ansi.Green}{message}");

			if (!GlobalVariables.EnableEasterEggs)
				printDefaultMessage();
			else
			{
				bool isDateToday(int day, int month) => DateTime.Today.Day == day && DateTime.Today.Month == month;
				void printMagnetMessage() => ConsoleHelpers.WriteGradientLine(message, true, (0x3E, 0x4F, 0x65), (0xDA, 0xE1, 0xEA), (0xEE, 0x70, 0x7D));

				if (isDateToday(31, 3)) /* 🏳️‍⚧️ */ ConsoleHelpers.WriteGradientLine(message, false, (91, 207, 250), (245, 171, 185), (255, 255, 255), (245, 171, 185), (17, 168, 205));
				else if (isDateToday(19, 3) || isDateToday(12, 5)) /* 🧲 */ printMagnetMessage();
				else if (isDateToday(24, 12)) /* 🎄 */ ConsoleHelpers.WriteGradientLine(message, false, (0xFF, 0x40, 0x40), (0x40, 0xFF, 0x40), (0xFF, 0x40, 0x40), (0x40, 0xFF, 0x40), (0xFF, 0x40, 0x40), (0x40, 0xFF, 0x40), (0xFF, 0x40, 0x40));
				else
				{
					var randomTime = DateTime.Today.AddMinutes(random.Next(24 * 60));
					if (randomTime >= DateTime.Now && randomTime.AddMinutes(30) < DateTime.Now) /* 🧲 */ printMagnetMessage();
					else printDefaultMessage();
				}
			}

			Console.WriteLine(new string('-', message.Length));
		}

		private void PrintAssemblies()
		{
			foreach (var assemblyName in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName.StartsWith(nameof(StoicGoose)) && x.FullName != Assembly.GetEntryAssembly().FullName).Select(x => x.GetName()))
				ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Using Assembly {assemblyName.Name} v{assemblyName.Version.Major:D3}{(assemblyName.Version.Minor != 0 ? $".{assemblyName.Version.Minor}" : string.Empty)}.");
		}

		private void CreateMachine(string typeName)
		{
			if (machine != null && isRunning)
				DestroyMachine();

			machine = Activator.CreateInstance(Type.GetType($"{typeName}, {Assembly.GetAssembly(typeof(IMachine))}")) as IMachine;
			machine.Initialize();
			machine.DisplayController.SendFramebuffer = (fb) => { displayTexture?.Update(fb); };
			machine.SoundController.SendSamples = (s) => { soundHandler?.EnqueueSamples(s); };
			machine.ReceiveInput = () =>
			{
				var buttonsPressed = new List<string>();
				var buttonsHeld = new List<string>();

				var screenWindow = imGuiHandler.GetWindow<DisplayWindow>();
				if (screenWindow.IsWindowOpen && screenWindow.IsFocused)
					inputHandler.PollInput(ref buttonsPressed, ref buttonsHeld);

				if (buttonsPressed.Contains("Volume"))
					machine.SoundController.ChangeMasterVolume();

				return (buttonsPressed, buttonsHeld);
			};

			ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks);
			ApplyMachineBreakpointHandlers(Program.Configuration.EnableBreakpoints);

			breakpointVariables = new(machine);
			lastBreakpointHit = null;

			inputHandler.SetVerticalRemapping(machine.VerticalControlRemap
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.ToDictionary(x => x[0], x => x[1]));

			displayTexture = new Texture(machine.ScreenWidth, machine.ScreenHeight);
			displayTexture.Update(initialScreenImage);

			bootstrapFilename = Program.Configuration.BootstrapFiles[typeName];

			systemControllerStatusWindow.SetComponentType(machine.GetType());
			displayControllerStatusWindow.SetComponentType(machine.DisplayController.GetType());
			soundControllerStatusWindow.SetComponentType(machine.SoundController.GetType());
		}

		private void ApplyMachinePatchHandlers(bool enabled)
		{
			if (enabled)
			{
				machine.ReadMemoryCallback = MachineReadMemoryCallback;
				machine.WriteMemoryCallback = MachineWriteMemoryCallback;
				machine.ReadPortCallback = MachineReadPortCallback;
				machine.WritePortCallback = MachineWritePortCallback;
			}
			else
			{
				machine.ReadMemoryCallback = default;
				machine.WriteMemoryCallback = default;
				machine.ReadPortCallback = default;
				machine.WritePortCallback = default;
			}
		}

		private void ApplyMachineBreakpointHandlers(bool enabled)
		{
			if (enabled)
				machine.RunStepCallback = MachineRunStepCallback;
			else
				machine.RunStepCallback = default;
		}

		private void DestroyMachine()
		{
			SaveVolatileData();

			machine.Shutdown();

			displayTexture?.Dispose();
			displayTexture = null;

			isRunning = false;
		}

		private void SaveVolatileData()
		{
			SaveCartridgeRam();
			SaveInternalEeprom();

			SaveMemoryPatches();
			SaveBreakpoints();
		}

		private byte MachineReadMemoryCallback(uint address, byte value)
		{
			/* Critical for performance b/c called on every memory read; do not use "heavy" functionality (ex. LINQ) */

			if (memoryPatches == null || memoryPatches.Length == 0 || (memoryPatches.Length > 0 && memoryPatches[0] == null))
				return value;

			foreach (var patch in memoryPatches)
			{
				if (patch == null)
					break;

				if (patch.Address != address || !patch.IsEnabled)
					continue;

				if (patch.Condition == MemoryPatchCondition.Always)
					return patch.PatchedValue;
				else if (patch.Condition == MemoryPatchCondition.LessThan && value < patch.CompareValue)
					return patch.PatchedValue;
				else if (patch.Condition == MemoryPatchCondition.LessThanOrEqual && value <= patch.CompareValue)
					return patch.PatchedValue;
				else if (patch.Condition == MemoryPatchCondition.GreaterThanOrEqual && value >= patch.CompareValue)
					return patch.PatchedValue;
				else if (patch.Condition == MemoryPatchCondition.GreaterThan && value > patch.CompareValue)
					return patch.PatchedValue;
			}

			return value;
		}

		private void MachineWriteMemoryCallback(uint address, byte value)
		{
			// TODO? -- remove callback?
		}

		private byte MachineReadPortCallback(ushort port, byte value)
		{
			// TODO? -- remove callback?
			return value;
		}

		private void MachineWritePortCallback(ushort port, byte value)
		{
			// TODO? -- remove callback?
		}

		private bool MachineRunStepCallback()
		{
			/* EXTREMELY critical for performance b/c called on every machine step; do not use "heavy" functionality (ex. LINQ) */

			if (breakpoints == null || breakpoints.Length == 0 || (breakpoints.Length > 0 && breakpoints[0] == null))
				return false;

			foreach (var bp in breakpoints)
			{
				if (bp == null)
					break;

				if (!bp.Enabled || bp.Runner == null || lastBreakpointHit == bp)
					continue;

				if (bp.Runner(breakpointVariables).Result)
				{
					ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Breakpoint hit: ({bp.Expression})");

					breakpointHitMessageBox.Message = $"Breakpoint with condition ({bp.Expression}) was hit.\n\nDisassembler window has been opened.";
					breakpointHitMessageBox.IsOpen = true;

					isPaused = true;
					lastBreakpointHit = bp;

					imGuiHandler.GetWindow<DisassemblerWindow>().IsWindowOpen = true;

					return true;
				}
			}

			return false;
		}

		private void LoadAndRunCartridge(string filename)
		{
			if (machine == null || string.IsNullOrEmpty(filename)) return;

			if (isRunning)
			{
				isRunning = false;
				SaveVolatileData();
			}

			cartridgeFilename = filename;
			cartSaveFilename = $"{Path.GetFileNameWithoutExtension(cartridgeFilename)}.sav";
			cartPatchFilename = $"{Path.GetFileNameWithoutExtension(cartridgeFilename)}_patches.json";
			cartBreakpointFilename = $"{Path.GetFileNameWithoutExtension(cartridgeFilename)}_breakpoints.json";

			using var stream = new FileStream(cartridgeFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			machine.LoadRom(data);

			isVerticalOrientation = machine.Cartridge.Metadata.Orientation == CartridgeMetadata.Orientations.Vertical;

			LoadCartridgeRam();
			LoadBootstrap();
			LoadInternalEeprom();

			LoadMemoryPatches();
			LoadBreakpoints();

			displayWindow.IsWindowOpen = true;

			machine.Reset();

			statusMessageItem.Label = $"Emulating {machine.Manufacturer} {machine.Model}, running '{cartridgeFilename}' ({machine.Cartridge.Metadata.GameIdString})";

			Program.Configuration.LastRomLoaded = cartridgeFilename;

			Program.SaveConfiguration();

			isRunning = true;
		}

		private void LoadBootstrap()
		{
			if (machine == null) return;

			if (!isRunning && Program.Configuration.UseBootstrap && File.Exists(bootstrapFilename) && !machine.IsBootstrapLoaded)
			{
				using var stream = new FileStream(bootstrapFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				machine.LoadBootstrap(data);
			}
		}

		private void LoadCartridgeRam()
		{
			if (machine == null) return;

			var path = Path.Combine(Program.SaveDataPath, cartSaveFilename);
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0) machine.LoadSaveData(data);
		}

		private void LoadInternalEeprom()
		{
			if (machine == null) return;

			var path = Path.Combine(Program.InternalDataPath, Program.InternalEepromFilenames[machine.GetType()]);
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0) machine.LoadInternalEeprom(data);
		}

		private void LoadMemoryPatches()
		{
			for (var i = 0; i < memoryPatches.Length; i++) memoryPatches[i] = null;

			var path = Path.Combine(Program.DebuggingDataPath, cartPatchFilename);
			if (File.Exists(path))
			{
				var memoryPatchList = path.DeserializeFromFile<List<MemoryPatch>>();
				if (memoryPatchList == null) return;

				memoryPatchList.RemoveAll(x => x == null);

				for (var i = 0; i < Math.Min(memoryPatchList.Count, memoryPatches.Length); i++)
					memoryPatches[i] = memoryPatchList[i];
			}
		}

		private void LoadBreakpoints()
		{
			for (var i = 0; i < breakpoints.Length; i++) breakpoints[i] = null;

			var path = Path.Combine(Program.DebuggingDataPath, cartBreakpointFilename);
			if (File.Exists(path))
			{
				var breakpointList = path.DeserializeFromFile<List<Breakpoint>>();
				if (breakpointList == null) return;

				breakpointList.RemoveAll(x => x == null || !x.UpdateDelegate());

				for (var i = 0; i < Math.Min(breakpointList.Count, breakpoints.Length); i++)
					breakpoints[i] = breakpointList[i];
			}
		}

		private void SaveCartridgeRam()
		{
			if (machine == null) return;

			var data = machine.GetSaveData();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.SaveDataPath, cartSaveFilename);

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void SaveInternalEeprom()
		{
			if (machine == null) return;

			var data = machine.GetInternalEeprom();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.InternalDataPath, Program.InternalEepromFilenames[machine.GetType()]);

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void SaveMemoryPatches()
		{
			if (memoryPatches?.Any(x => x != null) == true)
			{
				var path = Path.Combine(Program.DebuggingDataPath, cartPatchFilename);
				memoryPatches.Where(x => x != null).SerializeToFile(path);
			}
		}

		private void SaveBreakpoints()
		{
			if (breakpoints?.Any(x => x != null) == true)
			{
				var path = Path.Combine(Program.DebuggingDataPath, cartBreakpointFilename);
				breakpoints.Where(x => x != null).SerializeToFile(path);
			}
		}
	}
}

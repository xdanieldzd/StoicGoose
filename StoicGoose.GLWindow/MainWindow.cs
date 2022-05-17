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
using StoicGoose.Common.OpenGL;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.GLWindow
{
	public partial class MainWindow : GameWindow
	{
		/* UI */
		readonly LogWindow logWindow = default;

		ImGuiHandler imGuiHandler = default;

		BackgroundLogo backgroundGoose = default;

		MenuHandler menuHandler = default;
		MessageBoxHandler messageBoxHandler = default;
		StatusBarHandler statusBarHandler = default;
		FileDialogHandler fileDialogHandler = default;

		/* Graphics */
		readonly State renderState = new();
		byte[] initialScreenImage = default;

		/* Sound */
		readonly SoundHandler soundHandler = new(44100, 2);

		/* Input */
		readonly InputHandler inputHandler = new();

		/* Emulation */
		IMachine machine = default;
		Texture displayTexture = default;
		double frameTimeElapsed = 0.0;

		/* Misc. runtime variables */
		string bootstrapFilename = default, cartridgeFilename = default, cartSaveFilename = default;
		bool isRunning = false, isPaused = false, isVerticalOrientation = false;
		double framesPerSecond = 0.0;

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
			if ((logWindow = new()) != null)
			{
				Console.SetOut(logWindow.TextWriter);
				var message = $"{Program.ProductName} {Program.GetVersionString(true)}";
				Console.WriteLine($"{Ansi.Green}{message}");
				Console.WriteLine(new string('-', message.Length));
			}

			// TODO
			Program.Configuration.UseBootstrap = true;
			Program.Configuration.BootstrapFiles[typeof(WonderSwan).FullName] = @"D:\Temp\Goose\WonderSwan Boot ROM.rom";
			Program.Configuration.BootstrapFiles[typeof(WonderSwanColor).FullName] = @"D:\Temp\Goose\WonderSwan Color Boot ROM.rom";
		}

		protected override void OnLoad()
		{
			InitializeUI();

			imGuiHandler = new(this);
			imGuiHandler.RegisterWindow(logWindow, () => null);
			imGuiHandler.RegisterWindow(displayWindow, () => (displayTexture, isVerticalOrientation));
			imGuiHandler.RegisterWindow(disassemblerWindow, () => (machine, isRunning, isPaused));
			imGuiHandler.RegisterWindow(systemControllerStatusWindow, () => machine);
			imGuiHandler.RegisterWindow(displayControllerStatusWindow, () => machine.DisplayController);
			imGuiHandler.RegisterWindow(soundControllerStatusWindow, () => machine.SoundController);

			foreach (var windowTypeName in Program.Configuration.WindowsToRestore)
			{
				var type = Type.GetType(windowTypeName);
				if (type == null || imGuiHandler.GetWindow(type) is not WindowBase window) continue;
				window.IsWindowOpen = true;
			}

			backgroundGoose = new()
			{
				Texture = new(Resources.GetEmbeddedRgbaFile("Assets.Goose-Logo.rgba")),
				Positioning = BackgroundLogoPositioning.BottomRight,
				Offset = new(-32f),
				Scale = new(0.5f),
				Alpha = 32
			};

			menuHandler = new(fileMenu, emulationMenu, windowsMenu, optionsMenu, helpMenu);
			messageBoxHandler = new(aboutBox);
			statusBarHandler = new();
			fileDialogHandler = new(openRomDialog);

			statusMessageItem.Label = $"{Program.ProductName} {Program.GetVersionString(true)} ready!";

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

			using var stream = new FileStream(cartridgeFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			machine.LoadRom(data);

			isVerticalOrientation = machine.Cartridge.Metadata.Orientation == CartridgeMetadata.Orientations.Vertical;

			LoadCartridgeRam();
			LoadBootstrap();
			LoadInternalEeprom();

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
	}
}

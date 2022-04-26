using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

using StoicGoose.Common.Console;
using StoicGoose.Common.OpenGL;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Debugging;
using StoicGoose.GLWindow.Interface;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.GLWindow
{
	public class MainWindow : GameWindow
	{
		/* UI */
		readonly MenuItem fileMenu = default, emulationMenu = default, optionsMenu = default, helpMenu = default;
		readonly MessageBox aboutBox = default;
		readonly StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;
		ImGuiLogWindow logWindow = default;
		ImGuiHandler imGuiHandler = default;
		ImGuiMenuHandler imGuiMenuHandler = default;
		ImGuiMessageBoxHandler imGuiMessageBoxHandler = default;
		ImGuiStatusBarHandler imGuiStatusBarHandler = default;

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
		string bootstrapFilename = default, internalEepromFilename = default, cartridgeFilename = default, cartSaveFilename = default;
		bool isRunning = false, isPaused = false, isVerticalOrientation = false;
		double framesPerSecond = 0.0;

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
			if ((logWindow = new() { IsWindowOpen = true }) != null)
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

			fileMenu = new("File")
			{
				SubItems = new MenuItem[]
				{
					new("Open", (_) =>
					{
						// TODO: imgui file dialog
						LoadAndRunCartridge(@"D:\Temp\Goose\Digimon Adventure 02 - D1 Tamers (Japan).wsc");
					}),
					new("-"),
					new("Exit", (_) => { Close(); })
				}
			};

			emulationMenu = new("Emulation")
			{
				SubItems = new MenuItem[]
				{
					new("Pause",
					(_) => { isPaused = !isPaused; },
					(s) => { s.IsEnabled = isRunning; s.IsChecked = isPaused; }),
					new("Reset",
					(_) => { if (isRunning) { SaveEepromAndCartridgeRam(); machine?.Reset(); } },
					(s) => { s.IsEnabled = isRunning; }),
					new("-"),
					new("Shutdown",
					(_) => { if (isRunning) { SaveEepromAndCartridgeRam(); machine?.Shutdown(); displayTexture.Update(initialScreenImage); isRunning = false; } },
					(s) => { s.IsEnabled = isRunning; })
				}
			};

			optionsMenu = new("Options")
			{
				SubItems = new MenuItem[]
				{
					new("Preferred System")
					{
						SubItems = new MenuItem[]
						{
							new("WonderSwan",
							(_) => { Program.Configuration.PreferredSystem = typeof(WonderSwan).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwan).FullName; }),
							new("WonderSwan Color",
							(_) => { Program.Configuration.PreferredSystem = typeof(WonderSwanColor).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwanColor).FullName; })
						}
					},
					new("-"),
					new("Limit FPS",
					(_) => { Program.Configuration.LimitFps = !Program.Configuration.LimitFps; },
					(s) => { s.IsChecked = Program.Configuration.LimitFps; }),
					new("Mute",
					(_) => { soundHandler.SetMute(Program.Configuration.Mute = !Program.Configuration.Mute); },
					(s) => { s.IsChecked = Program.Configuration.Mute; }),
					new("-"),
					new("Show Log",
					(_) => { logWindow.IsWindowOpen = !logWindow.IsWindowOpen; },
					(s) => { s.IsChecked = logWindow.IsWindowOpen; })
				}
			};

			helpMenu = new("Help")
			{
				SubItems = new MenuItem[]
				{
					new("About...", (_) => { aboutBox.IsOpen = true; })
				}
			};

			aboutBox = new(
				"About",
				$"{Program.ProductName} {Program.GetVersionString(true)}\r\n" +
				$"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}\r\n" +
				"\r\n" +
				$"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}\r\n" +
				$"{ThisAssembly.Git.RepositoryUrl}",
				"Okay");

			statusMessageItem = new(string.Empty) { ShowSeparator = false };
			statusRunningItem = new(string.Empty) { Width = 100f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
			statusFpsItem = new(string.Empty) { Width = 75f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
		}

		protected override void OnLoad()
		{
			var disassemblerWindow = new ImGuiDisassemblerWindow() { IsWindowOpen = true };
			disassemblerWindow.PauseEmulation += (s, e) => isPaused = true;
			disassemblerWindow.UnpauseEmulation += (s, e) => isPaused = false;

			imGuiHandler = new(this);
			imGuiHandler.RegisterWindow(logWindow, () => null);
			imGuiHandler.RegisterWindow(new ImGuiScreenWindow() { IsWindowOpen = true, WindowScale = Program.Configuration.ScreenSize }, () => (displayTexture, isVerticalOrientation));
			imGuiHandler.RegisterWindow(disassemblerWindow, () => (machine, isRunning, isPaused));
			imGuiMenuHandler = new(fileMenu, emulationMenu, optionsMenu, helpMenu);
			imGuiMessageBoxHandler = new(aboutBox);
			imGuiStatusBarHandler = new();

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

			if (frameTimeElapsed >= 1.0 / machine.Metadata.RefreshRate || !Program.Configuration.LimitFps)
			{
				if (isRunning && !isPaused)
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
				imGuiMenuHandler.Draw();
				imGuiMessageBoxHandler.Draw();
				imGuiStatusBarHandler.Draw(statusMessageItem, statusRunningItem, statusFpsItem);

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

				var screenWindow = imGuiHandler.GetWindow<ImGuiScreenWindow>();
				if (screenWindow.IsWindowOpen && screenWindow.IsFocused)
					inputHandler.PollInput(ref buttonsPressed, ref buttonsHeld);

				if (buttonsPressed.Contains("Volume"))
					machine.SoundController.ChangeMasterVolume();

				return (buttonsPressed, buttonsHeld);
			};

			inputHandler.SetVerticalRemapping(machine.Metadata.VerticalControlRemap
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.ToDictionary(x => x[0], x => x[1]));

			displayTexture = new Texture(machine.Metadata.ScreenSize.X, machine.Metadata.ScreenSize.Y);
			displayTexture.Update(initialScreenImage);

			bootstrapFilename = Program.Configuration.BootstrapFiles[typeName];
			internalEepromFilename = Path.Combine(Program.InternalDataPath, machine.Metadata.InternalEepromFilename);

			Disassembler.Instance.ReadDelegate = machine.ReadMemory;
		}

		private void DestroyMachine()
		{
			SaveEepromAndCartridgeRam();

			machine.Shutdown();

			displayTexture?.Dispose();
			displayTexture = null;

			isRunning = false;
		}

		private void SaveEepromAndCartridgeRam()
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
				SaveEepromAndCartridgeRam();
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

			machine.Reset();

			statusMessageItem.Label = $"Emulating {machine.Metadata.Manufacturer} {machine.Metadata.Model}, running '{cartridgeFilename}' ({machine.Cartridge.Metadata.GameIdString})";

			Program.SaveConfiguration();

			isRunning = true;
		}

		private void LoadBootstrap()
		{
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
			var path = Path.Combine(Program.SaveDataPath, cartSaveFilename);
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0) machine.LoadSaveData(data);
		}

		private void LoadInternalEeprom()
		{
			if (!isRunning && File.Exists(internalEepromFilename))
			{
				using var stream = new FileStream(internalEepromFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				machine.LoadInternalEeprom(data);
			}
		}

		private void SaveCartridgeRam()
		{
			var data = machine.GetSaveData();
			if (data.Length == 0) return;

			var path = Path.Combine(Program.SaveDataPath, cartSaveFilename);

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void SaveInternalEeprom()
		{
			var data = machine.GetInternalEeprom();
			if (data.Length == 0) return;

			using var stream = new FileStream(internalEepromFilename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}
	}
}

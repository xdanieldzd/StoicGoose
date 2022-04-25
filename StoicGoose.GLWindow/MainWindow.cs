using System;
using System.IO;
using System.Reflection;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

using StoicGoose.Common.OpenGL;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.GLWindow
{
	public class MainWindow : GameWindow
	{
		readonly MenuItem fileMenu = default, emulationMenu = default, optionsMenu = default, helpMenu = default;
		readonly MessageBox aboutBox = default;
		readonly StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;

		readonly State renderState = new();

		IMachine machine = default;
		Texture displayTexture = default;

		string bootstrapFilename = default, internalEepromFilename = default, cartridgeFilename = default, cartSaveFilename = default;

		ImGuiHandler imGuiHandler = default;

		ImGuiMenuHandler imGuiMenuHandler = default;
		ImGuiMessageBoxHandler imGuiMessageBoxHandler = default;
		ImGuiStatusBarHandler imGuiStatusBarHandler = default;

		bool isRunning = false, isVerticalOrientation = false;
		float framesPerSecond = 0f;

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
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
					new("-", null),
					new("Exit", (_) => { Close(); })
				}
			};

			emulationMenu = new("Emulation")
			{
				SubItems = new MenuItem[]
				{
					new("Reset", (_) => { if (isRunning) { machine?.Reset(); } })
				}
			};

			optionsMenu = new("Option")
			{
				SubItems = new MenuItem[]
				{
					new("Preferred System")
					{
						SubItems = new MenuItem[]
						{
							new("WonderSwan",
							(s) => { Program.Configuration.PreferredSystem = typeof(WonderSwan).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwan).FullName; }),
							new("WonderSwan Color",
							(s) => { Program.Configuration.PreferredSystem = typeof(WonderSwanColor).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwanColor).FullName; })
						}
					}
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
			renderState.SetClearColor(System.Drawing.Color.FromArgb(0x3E, 0x4F, 0x65)); // 🧲

			CreateMachine(Program.Configuration.PreferredSystem);

			imGuiHandler = new(this);
			imGuiHandler.RegisterWindow(new ImGuiScreenWindow() { IsWindowOpen = true, WindowScale = Program.Configuration.ScreenSize },
				() => (displayTexture, isVerticalOrientation));

			imGuiMenuHandler = new(fileMenu, emulationMenu, optionsMenu, helpMenu);
			imGuiMessageBoxHandler = new(aboutBox);
			imGuiStatusBarHandler = new();

			statusMessageItem.Label = $"{Program.ProductName} {Program.GetVersionString(true)} ready!";

			base.OnLoad();
		}

		protected override void OnUnload()
		{
			Program.SaveConfiguration();

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

			Title = $"{Program.ProductName} {Program.GetVersionString(false)}";

			statusRunningItem.Label = isRunning ? "Running" : "Stopped";
			statusFpsItem.Label = $"{framesPerSecond:0} fps";

			machine.RunFrame();

			base.OnUpdateFrame(args);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			renderState.Submit();

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

			framesPerSecond = 1f / (float)args.Time;

			imGuiHandler.BeginFrame();
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

			displayTexture = new Texture(0, 0, 0, 255, machine.Metadata.ScreenSize.X, machine.Metadata.ScreenSize.Y);

			bootstrapFilename = Program.Configuration.BootstrapFiles[typeName];
			internalEepromFilename = Path.Combine(Program.InternalDataPath, machine.Metadata.InternalEepromFilename);
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

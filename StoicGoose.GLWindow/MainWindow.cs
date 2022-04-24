using System.IO;
using System.Reflection;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.GLWindow
{
	public class MainWindow : GameWindow
	{
		readonly MenuItem fileMenu = default;
		readonly MenuItem helpMenu = default;

		readonly MessageBox aboutBox = default;

		IMachine machine = default;

		GraphicsHandler graphicsHandler = default;
		ImGuiHandler imGuiHandler = default;

		ImGuiMenuHandler imGuiMenuHandler = default;
		ImGuiMessageBoxHandler imGuiMessageBoxHandler = default;

		bool isRunning = false, isVerticalOrientation = false;
		float framesPerSecond = 0f;

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
		{
			fileMenu = new("File")
			{
				SubItems = new MenuItem[]
				{
					new("Open", () =>
					{
						// TODO: imgui file dialog
						LoadAndRunCartridge(@"D:\Temp\Goose\Digimon Adventure 02 - D1 Tamers (Japan).wsc");
					}),
					new("-", null),
					new("Exit", Close)
				}
			};

			helpMenu = new("Help")
			{
				SubItems = new MenuItem[]
				{
					new("About...", () => { aboutBox.IsOpen = true; })
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
		}

		protected override void OnLoad()
		{
			machine = new WonderSwanColor();
			machine.Initialize();

			graphicsHandler = new(machine.Metadata, Program.Configuration.Video.Shader);
			imGuiHandler = new(this);
			imGuiHandler.RegisterWindow(new ImGuiScreenWindow() { IsWindowOpen = true, WindowScale = Program.Configuration.Video.ScreenSize },
				() => (graphicsHandler.DisplayTexture, isVerticalOrientation, framesPerSecond));

			imGuiMenuHandler = new(fileMenu, helpMenu);
			imGuiMessageBoxHandler = new(aboutBox);

			machine.DisplayController.SendFramebuffer = graphicsHandler.UpdateScreen;
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			//TODO do better?

			imGuiHandler.Resize(e.Width, e.Height);
			graphicsHandler.Resize(new System.Drawing.Rectangle(0, 0, e.Width, e.Height));

			base.OnResize(e);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			var keyState = KeyboardState.GetSnapshot();
			if (keyState.IsKeyDown(Keys.Escape)) Close();

			Title = $"{Program.ProductName} {Program.GetVersionString(false)}";

			machine.RunFrame();

			base.OnUpdateFrame(args);
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			framesPerSecond = 1f / (float)args.Time;

			imGuiHandler.BeginFrame();

			imGuiMenuHandler.Draw();
			imGuiMessageBoxHandler.Draw();

			graphicsHandler.SetClearColor(System.Drawing.Color.FromArgb(0x3E, 0x4F, 0x65)); // 🧲
			graphicsHandler.ClearFrame();

			graphicsHandler.BindTextures();

			imGuiHandler.EndFrame();

			SwapBuffers();

			base.OnRenderFrame(args);
		}

		private void LoadBootstrap(string filename)
		{
			if (!isRunning && Program.Configuration.General.UseBootstrap && File.Exists(filename) && !machine.IsBootstrapLoaded)
			{
				using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				machine.LoadBootstrap(data);
			}
		}

		private void LoadInternalEeprom(string filename)
		{
			if (!isRunning && File.Exists(filename))
			{
				using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var data = new byte[stream.Length];
				stream.Read(data, 0, data.Length);
				machine.LoadInternalEeprom(data);
			}
		}

		private void LoadAndRunCartridge(string filename)
		{
			if (isRunning)
			{
				// TODO save
				machine.Shutdown();
			}

			using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			machine.LoadRom(data);

			graphicsHandler.IsVerticalOrientation = isVerticalOrientation = machine.Cartridge.Metadata.Orientation == CartridgeMetadata.Orientations.Vertical;

			LoadRam($"{Path.GetFileNameWithoutExtension(filename)}.sav");

			LoadBootstrap(Program.Configuration.General.BootstrapFileWSC);
			LoadInternalEeprom(Path.Combine(Program.InternalDataPath, machine.Metadata.InternalEepromFilename));

			machine.Reset();

			Program.SaveConfiguration();

			isRunning = true;
		}

		private void LoadRam(string filename)
		{
			var path = Path.Combine(Program.SaveDataPath, filename);
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0) machine.LoadSaveData(data);
		}
	}
}

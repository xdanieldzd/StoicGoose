using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

using OpenTK.Mathematics;
using OpenTK.Wpf;

using StoicGoose.Core.Display;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.Machines;

using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.WPF
{
	// TODO: basically everything!

	public partial class MainWindow : Window
	{
		/* Constants */
		readonly static int statusIconSize = 12;

		/* Graphics */
		GraphicsHandler graphicsHandler = default;

		/* Sound */
		SoundHandler soundHandler = default;

		/* Input */
		InputHandler inputHandler = default;

		/* Emulation */
		IMachine machine = default;
		double frameTimeElapsed = 0.0;

		/* Misc. runtime variables */
		string bootstrapFilename = default, cartridgeFilename = default, cartSaveFilename = default;
		bool isRunning = false, isPaused = false, isVerticalOrientation = false;
		Vector2i statusIconsLocation = Vector2i.Zero;
		double framesPerSecond = 0.0;

		public MainWindow()
		{
			InitializeComponent();

			Title = $"{App.ProductName} {App.GetVersionString(true)}";

			OpenTkControl.Start(new GLWpfControlSettings { MajorVersion = App.RequiredGLVersion.Major, MinorVersion = App.RequiredGLVersion.Minor });

			var machineType = typeof(WonderSwanColor);
			statusIconsLocation = machineType == typeof(WonderSwan) ? new(0, DisplayControllerCommon.ScreenHeight) : new(DisplayControllerCommon.ScreenWidth, 0);
			graphicsHandler = new GraphicsHandler(machineType, new(DisplayControllerCommon.ScreenWidth, DisplayControllerCommon.ScreenHeight), statusIconsLocation, statusIconSize, machineType != typeof(WonderSwan), "Basic")
			{
				IsVerticalOrientation = isVerticalOrientation
			};

			soundHandler = new(44100, 2);
			inputHandler = new();

			soundHandler.SetMute(true /*App.Configuration.Mute*/);

			OpenTkControl.KeyDown += inputHandler.KeyDownEventHandler;
			OpenTkControl.KeyUp += inputHandler.KeyUpEventHandler;
			inputHandler.SetKeyMapping(App.Configuration.GameControls, App.Configuration.SystemControls);

			CreateMachine(App.Configuration.PreferredSystem);

			// TODO: kinda sucks? either change to different event, or make threaded/EmulationHandler like Winforms UI?
			CompositionTarget.Rendering += (s, e) =>
			{
				var args = e as RenderingEventArgs;
				frameTimeElapsed += args.RenderingTime.TotalSeconds;

				if (frameTimeElapsed >= 1.0 / machine.RefreshRate || !App.Configuration.LimitFps)
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

				//Title = $"{framesPerSecond:0} fps";
			};

			Closing += (s, e) =>
			{
				DestroyMachine();

				App.SaveConfiguration();

				soundHandler.Dispose();
			};
		}

		private void CreateMachine(string typeName)
		{
			if (machine != null && isRunning)
				DestroyMachine();

			machine = Activator.CreateInstance(Type.GetType($"{typeName}, {Assembly.GetAssembly(typeof(IMachine))}")) as IMachine;
			machine.Initialize();
			machine.DisplayController.SendFramebuffer = graphicsHandler.UpdateScreen;
			machine.SoundController.SendSamples = soundHandler.EnqueueSamples;
			machine.ReceiveInput = () =>
			{
				var buttonsPressed = new List<string>();
				var buttonsHeld = new List<string>();

				inputHandler.PollInput(ref buttonsPressed, ref buttonsHeld);

				if (buttonsPressed.Contains("Volume"))
					machine.SoundController.ChangeMasterVolume();

				return (buttonsPressed, buttonsHeld);
			};

			foreach (var button in machine.GameControls.Replace(" ", "").Split(','))
			{
				if (!App.Configuration.GameControls.ContainsKey(button))
					App.Configuration.GameControls[button] = string.Empty;
			}

			foreach (var button in machine.HardwareControls.Replace(" ", "").Split(','))
			{
				if (!App.Configuration.SystemControls.ContainsKey(button))
					App.Configuration.SystemControls[button] = string.Empty;
			}

			inputHandler.SetVerticalRemapping(machine.VerticalControlRemap
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				.ToDictionary(x => x[0], x => x[1]));

			if (App.Configuration.BootstrapFiles.ContainsKey(typeName))
				bootstrapFilename = App.Configuration.BootstrapFiles[typeName];
		}

		private void DestroyMachine()
		{
			SaveVolatileData();

			machine.Shutdown();

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
			inputHandler.IsVerticalOrientation = isVerticalOrientation;

			LoadCartridgeRam();
			LoadBootstrap();
			LoadInternalEeprom();

			machine.Reset();

			App.SaveConfiguration();

			isRunning = true;
		}

		private void LoadBootstrap()
		{
			if (machine == null) return;

			if (!isRunning && App.Configuration.UseBootstrap && File.Exists(bootstrapFilename) && !machine.IsBootstrapLoaded)
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

			var path = Path.Combine(App.SaveDataPath, cartSaveFilename);
			if (!File.Exists(path)) return;

			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			var data = new byte[stream.Length];
			stream.Read(data, 0, data.Length);
			if (data.Length != 0) machine.LoadSaveData(data);
		}

		private void LoadInternalEeprom()
		{
			if (machine == null) return;

			var path = Path.Combine(App.InternalDataPath, App.InternalEepromFilenames[machine.GetType()]);
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

			var path = Path.Combine(App.SaveDataPath, cartSaveFilename);

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void SaveInternalEeprom()
		{
			if (machine == null) return;

			var data = machine.GetInternalEeprom();
			if (data.Length == 0) return;

			var path = Path.Combine(App.InternalDataPath, App.InternalEepromFilenames[machine.GetType()]);

			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			stream.Write(data, 0, data.Length);
		}

		private void OpenTkControl_Render(TimeSpan delta)
		{
			graphicsHandler.ClearFrame();

			if (machine is MachineCommon machineCommon)
			{
				var activeIcons = new List<string>() { "Power" };

				if (machineCommon.BuiltInSelfTestOk) activeIcons.Add("Initialized");

				if (machineCommon.DisplayController.IconSleep) activeIcons.Add("Sleep");
				if (machineCommon.DisplayController.IconVertical) activeIcons.Add("Vertical");
				if (machineCommon.DisplayController.IconHorizontal) activeIcons.Add("Horizontal");
				if (machineCommon.DisplayController.IconAux1) activeIcons.Add("Aux1");
				if (machineCommon.DisplayController.IconAux2) activeIcons.Add("Aux2");
				if (machineCommon.DisplayController.IconAux3) activeIcons.Add("Aux3");

				if (machineCommon.SoundController.HeadphonesConnected) activeIcons.Add("Headphones");
				if (machineCommon.SoundController.MasterVolume == 0) activeIcons.Add("Volume0");
				if (machineCommon.SoundController.MasterVolume == 1) activeIcons.Add("Volume1");
				if (machineCommon.SoundController.MasterVolume == 2) activeIcons.Add("Volume2");
				if (machineCommon.SoundController.MasterVolume == 3 && machineCommon is WonderSwanColor) activeIcons.Add("Volume3");

				graphicsHandler.UpdateStatusIcons(activeIcons);
			}
			graphicsHandler.DrawFrame();
		}

		private void OpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			graphicsHandler.Resize(new System.Drawing.Rectangle(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height));
		}

		private void OpenRomMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// TEMP
			LoadAndRunCartridge(@"C:\Emulation\Games\WonderSwan\Digimon Adventure 02 - D1 Tamers (Japan).wsc");
		}

		private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void PauseMenuItem_Click(object sender, RoutedEventArgs e)
		{
			isPaused = !isPaused;
		}

		private void ResetMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (isRunning)
			{
				SaveVolatileData();
				machine?.Reset();
			}
		}
	}
}

using System.Reflection;

using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface;

namespace StoicGoose.GLWindow
{
	partial class MainWindow
	{
		MenuItem fileMenu = default, emulationMenu = default, optionsMenu = default, helpMenu = default;
		MessageBox aboutBox = default;
		StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;

		private void InitializeUI()
		{
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
					(_) => { if (isRunning) { SaveVolatileData(); machine?.Reset(); } },
					(s) => { s.IsEnabled = isRunning; }),
					new("-"),
					new("Shutdown",
					(_) => { if (isRunning) { SaveVolatileData(); machine?.Shutdown(); displayTexture.Update(initialScreenImage); isRunning = false; } },
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
	}
}

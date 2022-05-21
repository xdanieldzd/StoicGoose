using System;
using System.IO;
using System.Reflection;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface;

namespace StoicGoose.GLWindow
{
	partial class MainWindow
	{
		DisplayWindow displayWindow = default;
		DisassemblerWindow disassemblerWindow = default;
		SystemControllerStatusWindow systemControllerStatusWindow = default;
		DisplayControllerStatusWindow displayControllerStatusWindow = default;
		SoundControllerStatusWindow soundControllerStatusWindow = default;
		MemoryPatchWindow memoryPatchWindow = default;
		BreakpointWindow breakpointWindow = default;

		MenuItem fileMenu = default, emulationMenu = default, windowsMenu = default, optionsMenu = default, helpMenu = default;
		MessageBox aboutBox = default;
		StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;
		FileDialog openRomDialog = default, selectBootstrapRomDialog = default;

		BackgroundLogo backgroundGoose = default;

		MenuHandler menuHandler = default;
		MessageBoxHandler messageBoxHandler = default;
		StatusBarHandler statusBarHandler = default;
		FileDialogHandler fileDialogHandler = default;

		private void InitializeUI()
		{
			displayWindow = new DisplayWindow() { WindowScale = Program.Configuration.DisplaySize };

			disassemblerWindow = new DisassemblerWindow();
			disassemblerWindow.PauseEmulation += (s, e) => isPaused = true;
			disassemblerWindow.UnpauseEmulation += (s, e) => isPaused = false;

			systemControllerStatusWindow = new();
			displayControllerStatusWindow = new();
			soundControllerStatusWindow = new();

			memoryPatchWindow = new();
			breakpointWindow = new();

			void reinitMachineIfRunning()
			{
				if (machine != null && isRunning)
				{
					CreateMachine(Program.Configuration.PreferredSystem);
					LoadAndRunCartridge(Program.Configuration.LastRomLoaded);
				}
			}

			void initBootstrapRomDialog(Type machineType)
			{
				selectBootstrapRomDialog.InitialDirectory = Path.GetDirectoryName(Program.Configuration.BootstrapFiles[machineType.FullName]);
				selectBootstrapRomDialog.InitialFilename = Path.GetFileName(Program.Configuration.BootstrapFiles[machineType.FullName]);
				selectBootstrapRomDialog.Callback = (res, fn) =>
				{
					if (res == ImGuiFileDialogResult.Okay)
					{
						Program.Configuration.BootstrapFiles[machineType.FullName] = fn;
						reinitMachineIfRunning();
					}
				};
			}

			fileMenu = new("File")
			{
				SubItems = new MenuItem[]
				{
					new("Open", (_) =>
					{
						openRomDialog.InitialDirectory = Path.GetDirectoryName(Program.Configuration.LastRomLoaded);
						openRomDialog.InitialFilename = Path.GetFileName(Program.Configuration.LastRomLoaded);
						openRomDialog.IsOpen = true;
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
					(_) => { if (isRunning) { SaveVolatileData(); machine?.Shutdown(); displayTexture.Update(initialScreenImage); statusMessageItem.Label = "Machine shutdown."; isRunning = false; } },
					(s) => { s.IsEnabled = isRunning; })
				}
			};

			windowsMenu = new("Windows")
			{
				SubItems = new MenuItem[]
				{
					new("Display",
					(_) => { displayWindow.IsWindowOpen = !displayWindow.IsWindowOpen; },
					(s) => { s.IsChecked = displayWindow.IsWindowOpen; }),
					new("-"),
					new("Disassembler",
					(_) => { disassemblerWindow.IsWindowOpen = !disassemblerWindow.IsWindowOpen; },
					(s) => { s.IsChecked = disassemblerWindow.IsWindowOpen; }),
					new("Controller Status")
					{
						SubItems = new MenuItem[]
						{
							new(systemControllerStatusWindow.WindowTitle,
							(_) => { systemControllerStatusWindow.IsWindowOpen = !systemControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = systemControllerStatusWindow.IsWindowOpen; }),
							new(displayControllerStatusWindow.WindowTitle,
							(_) => { displayControllerStatusWindow.IsWindowOpen = !displayControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = displayControllerStatusWindow.IsWindowOpen; }),
							new(soundControllerStatusWindow.WindowTitle,
							(_) => { soundControllerStatusWindow.IsWindowOpen = !soundControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = soundControllerStatusWindow.IsWindowOpen; })
						}
					},
					new("-"),
					new("Memory Patches",
					(_) => { memoryPatchWindow.IsWindowOpen = !memoryPatchWindow.IsWindowOpen; },
					(s) => { s.IsChecked = memoryPatchWindow.IsWindowOpen; }),
					new("Breakpoints",
					(_) => { breakpointWindow.IsWindowOpen = !breakpointWindow.IsWindowOpen; },
					(s) => { s.IsChecked = breakpointWindow.IsWindowOpen; }),
					new("-"),
					new("Show Log",
					(_) => { logWindow.IsWindowOpen = !logWindow.IsWindowOpen; },
					(s) => { s.IsChecked = logWindow.IsWindowOpen; })
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
					new("Use Bootstrap ROMs",
					(_) => { Program.Configuration.UseBootstrap = !Program.Configuration.UseBootstrap; reinitMachineIfRunning(); },
					(s) => { s.IsChecked = Program.Configuration.UseBootstrap; }),
					new("Select Bootstrap ROM")
					{
						SubItems = new MenuItem[]
						{
							new("WonderSwan", (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwan));
								selectBootstrapRomDialog.IsOpen = true;
							}),
							new("WonderSwan Color", (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwanColor));
								selectBootstrapRomDialog.IsOpen = true;
							})
						}
					},
					new("-"),
					new("Enable Breakpoints",
					(_) => { ApplyMachineBreakpointHandlers(Program.Configuration.EnableBreakpoints = !Program.Configuration.EnableBreakpoints); },
					(s) => { s.IsChecked = Program.Configuration.EnableBreakpoints; }),
					new("Enable Memory Patches",
					(_) => { ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks = !Program.Configuration.EnablePatchCallbacks); },
					(s) => { s.IsChecked = Program.Configuration.EnablePatchCallbacks; })
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

			statusMessageItem = new($"{Program.ProductName} {Program.GetVersionString(true)} ready!") { ShowSeparator = false };
			statusRunningItem = new(string.Empty) { Width = 100f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
			statusFpsItem = new(string.Empty) { Width = 75f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };

			openRomDialog = new(ImGuiFileDialogType.Open, "Open ROM")
			{
				Filter = "WonderSwan & Color ROMs (*.ws;*.wsc)|*.ws;*.wsc",
				Callback = (res, fn) =>
				{
					if (res == ImGuiFileDialogResult.Okay)
						LoadAndRunCartridge(fn);
				}
			};

			selectBootstrapRomDialog = new(ImGuiFileDialogType.Open, "Select Bootstrap ROM") { Filter = "WonderSwan & Color ROMs (*.ws;*.wsc)|*.ws;*.wsc" };

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
			fileDialogHandler = new(openRomDialog, selectBootstrapRomDialog);
		}
	}
}

using System;
using System.IO;
using System.Reflection;

using StoicGoose.Common.Localization;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Interface.Handlers;
using StoicGoose.GLWindow.Interface.Widgets;
using StoicGoose.GLWindow.Interface.Windows;

namespace StoicGoose.GLWindow
{
	partial class MainWindow
	{
		InputSettingsWindow inputSettingsWindow = default;
		DisplayWindow displayWindow = default;
		DisassemblerWindow disassemblerWindow = default;
		SystemControllerStatusWindow systemControllerStatusWindow = default;
		DisplayControllerStatusWindow displayControllerStatusWindow = default;
		SoundControllerStatusWindow soundControllerStatusWindow = default;
		MemoryPatchWindow memoryPatchWindow = default;
		BreakpointWindow breakpointWindow = default;
		MemoryEditorWindow memoryEditorWindow = default;
		TilemapViewerWindow tilemapViewerWindow = default;

		MenuItem fileMenu = default, emulationMenu = default, windowsMenu = default, optionsMenu = default, helpMenu = default;
		MessageBox aboutMessageBox = default, breakpointHitMessageBox = default;
		StatusBarItem statusMessageItem = default, statusRunningItem = default, statusFpsItem = default;
		FileDialog openRomDialog = default, selectBootstrapRomDialog = default;

		BackgroundLogo backgroundGoose = default;

		MenuHandler menuHandler = default;
		MessageBoxHandler messageBoxHandler = default;
		StatusBarHandler statusBarHandler = default;
		FileDialogHandler fileDialogHandler = default;

		private void InitializeUI()
		{
			inputSettingsWindow = new();

			displayWindow = new DisplayWindow() { WindowScale = Program.Configuration.DisplaySize };

			disassemblerWindow = new DisassemblerWindow();
			disassemblerWindow.PauseEmulation += (s, e) => isPaused = true;
			disassemblerWindow.UnpauseEmulation += (s, e) => isPaused = false;

			systemControllerStatusWindow = new();
			displayControllerStatusWindow = new();
			soundControllerStatusWindow = new();

			memoryPatchWindow = new();
			breakpointWindow = new();
			memoryEditorWindow = new();

			tilemapViewerWindow = new();

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

			fileMenu = new(Localizer.GetString("MainWindow.Menus.File"))
			{
				SubItems = new MenuItem[]
				{
					new(Localizer.GetString("MainWindow.Menus.Open"), (_) =>
					{
						openRomDialog.InitialDirectory = Path.GetDirectoryName(Program.Configuration.LastRomLoaded);
						openRomDialog.InitialFilename = Path.GetFileName(Program.Configuration.LastRomLoaded);
						openRomDialog.IsOpen = true;
					}),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.Exit"), (_) => { Close(); })
				}
			};

			emulationMenu = new(Localizer.GetString("MainWindow.Menus.Emulation"))
			{
				SubItems = new MenuItem[]
				{
					new(Localizer.GetString("MainWindow.Menus.Pause"),
					(_) => { isPaused = !isPaused; },
					(s) => { s.IsEnabled = isRunning; s.IsChecked = isPaused; }),
					new(Localizer.GetString("MainWindow.Menus.Reset"),
					(_) => { if (isRunning) { SaveVolatileData(); machine?.Reset(); } },
					(s) => { s.IsEnabled = isRunning; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.Shutdown"),
					(_) => { if (isRunning) { SaveVolatileData(); machine?.Shutdown(); displayTexture.Update(initialScreenImage); statusMessageItem.Label = "Machine shutdown."; isRunning = false; } },
					(s) => { s.IsEnabled = isRunning; })
				}
			};

			windowsMenu = new(Localizer.GetString("MainWindow.Menus.Windows"))
			{
				SubItems = new MenuItem[]
				{
					new(Localizer.GetString("MainWindow.Menus.Display"),
					(_) => { displayWindow.IsWindowOpen = !displayWindow.IsWindowOpen; },
					(s) => { s.IsChecked = displayWindow.IsWindowOpen; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.Disassembler"),
					(_) => { disassemblerWindow.IsWindowOpen = !disassemblerWindow.IsWindowOpen; },
					(s) => { s.IsChecked = disassemblerWindow.IsWindowOpen; }),
					new(Localizer.GetString("MainWindow.Menus.MemoryEditor"),
					(_) => { memoryEditorWindow.IsWindowOpen = !memoryEditorWindow.IsWindowOpen; },
					(s) => { s.IsChecked = memoryEditorWindow.IsWindowOpen; }),
					new(Localizer.GetString("MainWindow.Menus.SystemControllers"))
					{
						SubItems = new MenuItem[]
						{
							new(Localizer.GetString("MainWindow.Menus.SystemController"),
							(_) => { systemControllerStatusWindow.IsWindowOpen = !systemControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = systemControllerStatusWindow.IsWindowOpen; }),
							new(Localizer.GetString("MainWindow.Menus.DisplayController"),
							(_) => { displayControllerStatusWindow.IsWindowOpen = !displayControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = displayControllerStatusWindow.IsWindowOpen; }),
							new(Localizer.GetString("MainWindow.Menus.SoundController"),
							(_) => { soundControllerStatusWindow.IsWindowOpen = !soundControllerStatusWindow.IsWindowOpen; },
							(s) => { s.IsChecked = soundControllerStatusWindow.IsWindowOpen; })
						}
					},
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.TilemapViewer"),
					(_) => { tilemapViewerWindow.IsWindowOpen = !tilemapViewerWindow.IsWindowOpen; },
					(s) => { s.IsChecked = tilemapViewerWindow.IsWindowOpen; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.Breakpoints"),
					(_) => { breakpointWindow.IsWindowOpen = !breakpointWindow.IsWindowOpen; },
					(s) => { s.IsChecked = breakpointWindow.IsWindowOpen; }),
					new(Localizer.GetString("MainWindow.Menus.MemoryPatches"),
					(_) => { memoryPatchWindow.IsWindowOpen = !memoryPatchWindow.IsWindowOpen; },
					(s) => { s.IsChecked = memoryPatchWindow.IsWindowOpen; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.Log"),
					(_) => { logWindow.IsWindowOpen = !logWindow.IsWindowOpen; },
					(s) => { s.IsChecked = logWindow.IsWindowOpen; })
				}
			};

			optionsMenu = new(Localizer.GetString("MainWindow.Menus.Options"))
			{
				SubItems = new MenuItem[]
				{
					new(Localizer.GetString("MainWindow.Menus.PreferredSystem"))
					{
						SubItems = new MenuItem[]
						{
							new(Localizer.GetString("MainWindow.Menus.WonderSwan"),
							(_) => { Program.Configuration.PreferredSystem = typeof(WonderSwan).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwan).FullName; }),
							new(Localizer.GetString("MainWindow.Menus.WonderSwanColor"),
							(_) => { Program.Configuration.PreferredSystem = typeof(WonderSwanColor).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							(s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwanColor).FullName; })
						}
					},
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.LimitFPS"),
					(_) => { Program.Configuration.LimitFps = !Program.Configuration.LimitFps; },
					(s) => { s.IsChecked = Program.Configuration.LimitFps; }),
					new(Localizer.GetString("MainWindow.Menus.Mute"),
					(_) => { soundHandler.SetMute(Program.Configuration.Mute = !Program.Configuration.Mute); },
					(s) => { s.IsChecked = Program.Configuration.Mute; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.UseBootstrapROMs"),
					(_) => { Program.Configuration.UseBootstrap = !Program.Configuration.UseBootstrap; reinitMachineIfRunning(); },
					(s) => { s.IsChecked = Program.Configuration.UseBootstrap; }),
					new(Localizer.GetString("MainWindow.Menus.SelectBootstrapROM"))
					{
						SubItems = new MenuItem[]
						{
							new(Localizer.GetString("MainWindow.Menus.WonderSwan"), (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwan));
								selectBootstrapRomDialog.IsOpen = true;
							}),
							new(Localizer.GetString("MainWindow.Menus.WonderSwanColor"), (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwanColor));
								selectBootstrapRomDialog.IsOpen = true;
							})
						}
					},
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.EnableBreakpoints"),
					(_) => { ApplyMachineBreakpointHandlers(Program.Configuration.EnableBreakpoints = !Program.Configuration.EnableBreakpoints); },
					(s) => { s.IsChecked = Program.Configuration.EnableBreakpoints; }),
					new(Localizer.GetString("MainWindow.Menus.EnableMemoryPatches"),
					(_) => { ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks = !Program.Configuration.EnablePatchCallbacks); },
					(s) => { s.IsChecked = Program.Configuration.EnablePatchCallbacks; }),
					new("-"),
					new(Localizer.GetString("MainWindow.Menus.InputSettings"),
					(_) => { inputSettingsWindow.IsWindowOpen = !inputSettingsWindow.IsWindowOpen; },
					(s) => { s.IsChecked = inputSettingsWindow.IsWindowOpen; })
				}
			};

			helpMenu = new(Localizer.GetString("MainWindow.Menus.Help"))
			{
				SubItems = new MenuItem[]
				{
					new(Localizer.GetString("MainWindow.Menus.About"), (_) => { aboutMessageBox.IsOpen = true; })
				}
			};

			aboutMessageBox = new(
				"About",
				$"{Program.ProductName} {Program.GetVersionString(true)}\r\n" +
				$"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}\r\n" +
				"\r\n" +
				$"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}\r\n" +
				$"{ThisAssembly.Git.RepositoryUrl}",
				"Okay");

			breakpointHitMessageBox = new("Breakpoint Hit", string.Empty, "Okay");

			statusMessageItem = new(Localizer.GetString("MainWindow.StatusMessageReady", new { Program.ProductName, ProductVersion = Program.GetVersionString(true) })) { ShowSeparator = false };
			statusRunningItem = new(string.Empty) { Width = 100f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
			statusFpsItem = new(string.Empty) { Width = 75f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };

			openRomDialog = new(ImGuiFileDialogType.Open, Localizer.GetString("MainWindow.Dialogs.OpenROMTitle"))
			{
				Filter = "WonderSwan & Color ROMs (*.ws;*.wsc)|*.ws;*.wsc",
				Callback = (res, fn) =>
				{
					if (res == ImGuiFileDialogResult.Okay)
						LoadAndRunCartridge(fn);
				}
			};

			selectBootstrapRomDialog = new(ImGuiFileDialogType.Open, Localizer.GetString("MainWindow.Dialogs.SelectBootstrapROMTitle")) { Filter = "WonderSwan & Color ROMs (*.ws;*.wsc)|*.ws;*.wsc" };

			backgroundGoose = new()
			{
				Texture = new(Resources.GetEmbeddedRgbaFile("Assets.Goose-Logo.rgba")),
				Positioning = BackgroundLogoPositioning.BottomRight,
				Offset = new(-32f),
				Scale = new(0.5f),
				Alpha = 32
			};

			menuHandler = new(fileMenu, emulationMenu, windowsMenu, optionsMenu, helpMenu);
			messageBoxHandler = new(aboutMessageBox, breakpointHitMessageBox);
			statusBarHandler = new();
			fileDialogHandler = new(openRomDialog, selectBootstrapRomDialog);
		}
	}
}

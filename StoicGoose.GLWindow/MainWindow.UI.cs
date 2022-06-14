using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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

			fileMenu = new(localization: "MainWindow.Menus.File")
			{
				SubItems = new MenuItem[]
				{
					new(localization: "MainWindow.Menus.Open", clickAction: (_) =>
					{
						openRomDialog.InitialDirectory = Path.GetDirectoryName(Program.Configuration.LastRomLoaded);
						openRomDialog.InitialFilename = Path.GetFileName(Program.Configuration.LastRomLoaded);
						openRomDialog.IsOpen = true;
					}),
					new("-"),
					new(localization: "MainWindow.Menus.Exit", clickAction: (_) => { Close(); })
				}
			};

			emulationMenu = new(localization: "MainWindow.Menus.Emulation")
			{
				SubItems = new MenuItem[]
				{
					new(localization: "MainWindow.Menus.Pause",
					clickAction: (_) => { isPaused = !isPaused; },
					updateAction: (s) => { s.IsEnabled = isRunning; s.IsChecked = isPaused; }),
					new(localization: "MainWindow.Menus.Reset",
					clickAction: (_) => { if (isRunning) { SaveVolatileData(); machine?.Reset(); } },
					updateAction: (s) => { s.IsEnabled = isRunning; }),
					new("-"),
					new(localization: "MainWindow.Menus.Shutdown",
					clickAction: (_) => { if (isRunning) { SaveVolatileData(); machine?.Shutdown(); displayTexture.Update(initialScreenImage); statusMessageItem.Label = "Machine shutdown."; isRunning = false; } },
					updateAction: (s) => { s.IsEnabled = isRunning; })
				}
			};

			windowsMenu = new(localization: "MainWindow.Menus.Windows")
			{
				SubItems = new MenuItem[]
				{
					new(localization: "MainWindow.Menus.Display",
					clickAction: (_) => { displayWindow.IsWindowOpen = !displayWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = displayWindow.IsWindowOpen; }),
					new("-"),
					new(localization: "MainWindow.Menus.Disassembler",
					clickAction: (_) => { disassemblerWindow.IsWindowOpen = !disassemblerWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = disassemblerWindow.IsWindowOpen; }),
					new(localization: "MainWindow.Menus.MemoryEditor",
					clickAction : (_) => { memoryEditorWindow.IsWindowOpen = !memoryEditorWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = memoryEditorWindow.IsWindowOpen; }),
					new(localization: "MainWindow.Menus.SystemControllers")
					{
						SubItems = new MenuItem[]
						{
							new(localization: "MainWindow.Menus.SystemController",
							clickAction : (_) => { systemControllerStatusWindow.IsWindowOpen = !systemControllerStatusWindow.IsWindowOpen; },
							updateAction: (s) => { s.IsChecked = systemControllerStatusWindow.IsWindowOpen; }),
							new(localization: "MainWindow.Menus.DisplayController",
							clickAction : (_) => { displayControllerStatusWindow.IsWindowOpen = !displayControllerStatusWindow.IsWindowOpen; },
							updateAction: (s) => { s.IsChecked = displayControllerStatusWindow.IsWindowOpen; }),
							new(localization: "MainWindow.Menus.SoundController",
							clickAction : (_) => { soundControllerStatusWindow.IsWindowOpen = !soundControllerStatusWindow.IsWindowOpen; },
							updateAction: (s) => { s.IsChecked = soundControllerStatusWindow.IsWindowOpen; })
						}
					},
					new("-"),
					new(localization: "MainWindow.Menus.TilemapViewer",
					clickAction : (_) => { tilemapViewerWindow.IsWindowOpen = !tilemapViewerWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = tilemapViewerWindow.IsWindowOpen; }),
					new("-"),
					new(localization: "MainWindow.Menus.Breakpoints",
					clickAction : (_) => { breakpointWindow.IsWindowOpen = !breakpointWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = breakpointWindow.IsWindowOpen; }),
					new(localization: "MainWindow.Menus.MemoryPatches",
					clickAction : (_) => { memoryPatchWindow.IsWindowOpen = !memoryPatchWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = memoryPatchWindow.IsWindowOpen; }),
					new("-"),
					new(localization: "MainWindow.Menus.Log",
					clickAction: (_) => { logWindow.IsWindowOpen = !logWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = logWindow.IsWindowOpen; })
				}
			};

			optionsMenu = new(localization: "MainWindow.Menus.Options")
			{
				SubItems = new MenuItem[]
				{
					new(localization: "MainWindow.Menus.Language")
					{
						SubItems = Localizer.GetSupportedLanguages().Select(x =>
							new MenuItem(label: x.NativeName,
							clickAction: (_) => { Thread.CurrentThread.CurrentUICulture = new(Program.Configuration.Language = x.TwoLetterISOLanguageName); LocalizeUI(); },
							updateAction: (s) => { s.IsChecked = Program.Configuration.Language == x.TwoLetterISOLanguageName; })
						).ToArray()
					},
					new("-"),
					new(localization: "MainWindow.Menus.PreferredSystem")
					{
						SubItems = new MenuItem[]
						{
							new(localization: "MainWindow.Menus.WonderSwan",
							clickAction: (_) => { Program.Configuration.PreferredSystem = typeof(WonderSwan).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							updateAction: (s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwan).FullName; }),
							new(localization: "MainWindow.Menus.WonderSwanColor",
							clickAction: (_) => { Program.Configuration.PreferredSystem = typeof(WonderSwanColor).FullName; CreateMachine(Program.Configuration.PreferredSystem); LoadAndRunCartridge(cartridgeFilename); },
							updateAction: (s) => { s.IsChecked = Program.Configuration.PreferredSystem == typeof(WonderSwanColor).FullName; })
						}
					},
					new("-"),
					new(localization: "MainWindow.Menus.LimitFPS",
					clickAction: (_) => { Program.Configuration.LimitFps = !Program.Configuration.LimitFps; },
					updateAction: (s) => { s.IsChecked = Program.Configuration.LimitFps; }),
					new(localization: "MainWindow.Menus.Mute",
					clickAction: (_) => { soundHandler.SetMute(Program.Configuration.Mute = !Program.Configuration.Mute); },
					updateAction: (s) => { s.IsChecked = Program.Configuration.Mute; }),
					new("-"),
					new(localization: "MainWindow.Menus.UseBootstrapROMs",
					clickAction: (_) => { Program.Configuration.UseBootstrap = !Program.Configuration.UseBootstrap; reinitMachineIfRunning(); },
					updateAction: (s) => { s.IsChecked = Program.Configuration.UseBootstrap; }),
					new(localization: "MainWindow.Menus.SelectBootstrapROM")
					{
						SubItems = new MenuItem[]
						{
							new(localization: "MainWindow.Menus.WonderSwan", clickAction: (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwan));
								selectBootstrapRomDialog.IsOpen = true;
							}),
							new(localization: "MainWindow.Menus.WonderSwanColor", clickAction: (_) =>
							{
								initBootstrapRomDialog(typeof(WonderSwanColor));
								selectBootstrapRomDialog.IsOpen = true;
							})
						}
					},
					new("-"),
					new(localization: "MainWindow.Menus.EnableBreakpoints",
					clickAction: (_) => { ApplyMachineBreakpointHandlers(Program.Configuration.EnableBreakpoints = !Program.Configuration.EnableBreakpoints); },
					updateAction: (s) => { s.IsChecked = Program.Configuration.EnableBreakpoints; }),
					new(localization: "MainWindow.Menus.EnableMemoryPatches",
					clickAction: (_) => { ApplyMachinePatchHandlers(Program.Configuration.EnablePatchCallbacks = !Program.Configuration.EnablePatchCallbacks); },
					updateAction: (s) => { s.IsChecked = Program.Configuration.EnablePatchCallbacks; }),
					new("-"),
					new(localization: "MainWindow.Menus.InputSettings",
					clickAction: (_) => { inputSettingsWindow.IsWindowOpen = !inputSettingsWindow.IsWindowOpen; },
					updateAction: (s) => { s.IsChecked = inputSettingsWindow.IsWindowOpen; })
				}
			};

			helpMenu = new(localization: "MainWindow.Menus.Help")
			{
				SubItems = new MenuItem[]
				{
					new(localization: "MainWindow.Menus.About", clickAction: (_) => { aboutMessageBox.IsOpen = true; })
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

			statusMessageItem = new() { ShowSeparator = false };
			statusRunningItem = new() { Width = 100f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };
			statusFpsItem = new() { Width = 75f, ItemAlignment = StatusBarItemAlign.Right, TextAlignment = StatusBarItemTextAlign.Center };

			openRomDialog = new(ImGuiFileDialogType.Open)
			{
				Callback = (res, fn) =>
				{
					if (res == ImGuiFileDialogResult.Okay)
						LoadAndRunCartridge(fn);
				}
			};

			selectBootstrapRomDialog = new(ImGuiFileDialogType.Open);

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

			Log.WriteEvent(LogSeverity.Information, this, "User interface initialized.");
		}

		private void LocalizeUI()
		{
			static void localizeMenus(params MenuItem[] menuItems)
			{
				foreach (var menuItem in menuItems.Where(x => !string.IsNullOrEmpty(x.Localization)))
				{
					menuItem.Label = Localizer.GetString(menuItem.Localization);
					localizeMenus(menuItem.SubItems.Where(x => !string.IsNullOrEmpty(x.Localization)).ToArray());
				}
			}

			localizeMenus(fileMenu, emulationMenu, windowsMenu, optionsMenu, helpMenu);

			statusMessageItem.Label = Localizer.GetString("MainWindow.LanguageChanged", new { Language = Thread.CurrentThread.CurrentUICulture.NativeName });

			openRomDialog.Title = Localizer.GetString("MainWindow.Dialogs.OpenROMTitle");
			openRomDialog.Filter = Localizer.GetString("MainWindow.Dialogs.ROMFilter");

			selectBootstrapRomDialog.Title = Localizer.GetString("MainWindow.Dialogs.SelectBootstrapROMTitle");
			selectBootstrapRomDialog.Filter = Localizer.GetString("MainWindow.Dialogs.ROMFilter");

			FileDialogHandler.OpenButtonLabel = Localizer.GetString("FileDialogHandler.OpenButton");
			FileDialogHandler.CancelButtonLabel = Localizer.GetString("FileDialogHandler.CancelButton");

			Log.WriteEvent(LogSeverity.Information, this, $"UI localization to {Thread.CurrentThread.CurrentUICulture.DisplayName} applied.");
		}
	}
}

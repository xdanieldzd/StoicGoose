using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StoicGoose.Common.Extensions;
using StoicGoose.Common.OpenGL;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Display;
using StoicGoose.Core.Machines;
using StoicGoose.WinForms.Handlers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CartridgeMetadata = StoicGoose.Core.Cartridges.Metadata;

namespace StoicGoose.WinForms
{
    public partial class MainForm : Form
    {
        /* Constants */
        readonly static int maxScreenSizeFactor = 5;
        readonly static int maxRecentFiles = 15;
        readonly static int statusIconSize = 12;

        readonly static List<(string description, string extension, Func<string, Stream> streamReadFunc)> supportedFileInformation =
        [
            ("WonderSwan ROMs", ".ws", GetStreamFromFile),
            ("WonderSwan Color ROMs", ".wsc", GetStreamFromFile),
            ("Zip Archives", ".zip", GetStreamFromFirstZippedFile)
        ];

        /* Various handlers */
        DatabaseHandler databaseHandler = default;
        GraphicsHandler graphicsHandler = default;
        SoundHandler soundHandler = default;
        InputHandler inputHandler = default;
        EmulatorHandler emulatorHandler = default;

        /* Misc. windows */
        SoundRecorderForm soundRecorderForm = default;
        CheatsForm cheatsForm = default;

        /* Misc. runtime variables */
        Type machineType = default;
        readonly List<Binding> uiDataBindings = [];
        bool isVerticalOrientation = false;
        string internalEepromPath = string.Empty;
        Vector2i statusIconsLocation = Vector2i.Zero;
        Cheat[] cheats = default;

        public MainForm()
        {
            InitializeComponent();

            renderControl.APIVersion = Program.RequiredGLVersion;

            if (InitializeConsole())
            {
                var message = $"{Ansi.Green}{Application.ProductName} {Program.GetVersionString(true)}";
                Log.WriteLine(message);
                Log.WriteLine("HONK, HONK, pork cheek!");
                Log.WriteLine($"{Ansi.Reset}{new string('-', message.Length - Ansi.Green.Length)}");
            }

            if (GlobalVariables.EnableOpenGLDebug)
            {
                GLFW.WindowHint(WindowHintBool.OpenGLDebugContext, true);
                renderControl.Flags |= ContextFlags.Debug;

                Log.WriteEvent(LogSeverity.Information, this, "Enabled OpenGL debugging.");
            }

            Log.WriteEvent(LogSeverity.Information, this, "Constructor done.");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Log.WriteEvent(LogSeverity.Information, this, "Initializing emulator and UI...");

            ContextInfo.WriteToLog(this, false);

            machineType = Program.Configuration.General.PreferOriginalWS ? typeof(WonderSwan) : typeof(WonderSwanColor);

            InitializeEmulatorHandler();
            VerifyConfiguration();
            InitializeOtherHandlers();
            InitializeWindows();

            SizeAndPositionWindow();
            SetWindowTitleAndStatus();

            CreateRecentFilesMenu();
            CreateScreenSizeMenu();
            CreateShaderMenu();

            SetFileFilters();

            InitializeUIMiscellanea();

            if (GlobalVariables.IsDebugBuild && GlobalVariables.EnableSuperVerbosity)
            {
                Log.WriteLine($"~ {Ansi.Cyan}Global variables{Ansi.Reset} ~");
                foreach (var var in GlobalVariables.Dump()) Log.WriteLine($" {var}");
            }

            if (GlobalVariables.EnableOpenGLDebug)
            {
                Log.WriteLine($"~ {Ansi.Yellow}OpenGL debugging enabled{Ansi.Reset} ~");
                if (GlobalVariables.EnableSuperVerbosity)
                {
                    Log.WriteLine($"~ {Ansi.Cyan}GL context info{Ansi.Reset} ~");
                    Log.WriteLine($" Version: {ContextInfo.GLVersion}");
                    Log.WriteLine($" Vendor: {ContextInfo.GLVendor}");
                    Log.WriteLine($" Renderer: {ContextInfo.GLRenderer}");
                    Log.WriteLine($" GLSL version: {ContextInfo.GLShadingLanguageVersion}");
                    Log.WriteLine($" {ContextInfo.GLExtensions.Length} extensions supported");
                }
            }

            if (GlobalVariables.IsAuthorsMachine && GlobalVariables.EnableSuperVerbosity)
            {
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.White}########################################");
                Log.WriteLine($"{Ansi.White}########################################");
                Log.WriteLine($"{Ansi.White}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.Magenta}########################################");
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"{Ansi.Cyan}########################################");
                Log.WriteLine($"Ze goose sez: {Ansi.Cyan}TRANS {Ansi.Magenta}RIGHTS {Ansi.White}ARE {Ansi.Magenta}HUMAN {Ansi.Cyan}RIGHTS{Ansi.Reset}!");
            }

            HandleCommandLineArguments(Environment.GetCommandLineArgs().Skip(1));

            Log.WriteEvent(LogSeverity.Information, this, "Initialization done!");
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            renderControl.Focus();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] filenames) return;

            if (TryLoadAndRunCartridge(filenames.First()))
                Log.WriteEvent(LogSeverity.Information, this, "Loaded ROM via file drop.");
            else
                Log.WriteEvent(LogSeverity.Warning, this, "File drop contained unrecognized file.");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAllData();
            emulatorHandler.Shutdown();

            Program.SaveConfiguration();
        }

        private static bool InitializeConsole()
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AllocConsole();
            [DllImport("kernel32.dll")]
            static extern IntPtr GetStdHandle(uint nStdHandle);
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

            bool result = AllocConsole();
            if (result)
            {
                var consoleHandle = GetStdHandle(0xFFFFFFF5); // STD_OUTPUT_HANDLE
                if (GetConsoleMode(consoleHandle, out uint lpMode))
                    SetConsoleMode(consoleHandle, lpMode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
            }
            return result;
        }

        private void InitializeEmulatorHandler()
        {
            emulatorHandler = new EmulatorHandler(machineType);
            emulatorHandler.SetFpsLimiter(Program.Configuration.General.LimitFps);
        }

        private void VerifyConfiguration()
        {
            foreach (var button in emulatorHandler.Machine.GameControls.Replace(" ", "").Split(','))
            {
                if (!Program.Configuration.Input.GameControls.ContainsKey(button))
                    Program.Configuration.Input.GameControls[button] = [];
            }

            foreach (var button in emulatorHandler.Machine.HardwareControls.Replace(" ", "").Split(','))
            {
                if (!Program.Configuration.Input.SystemControls.ContainsKey(button))
                    Program.Configuration.Input.SystemControls[button] = [];
            }

            if (Program.Configuration.Video.ScreenSize < 2 || Program.Configuration.Video.ScreenSize > maxScreenSizeFactor)
                Program.Configuration.Video.ResetToDefault(nameof(Program.Configuration.Video.ScreenSize));

            if (string.IsNullOrEmpty(Program.Configuration.Video.Shader) || (graphicsHandler != null && !graphicsHandler.AvailableShaders.Contains(Program.Configuration.Video.Shader)))
                Program.Configuration.Video.Shader = GraphicsHandler.DefaultShaderName;
        }

        private void InitializeOtherHandlers()
        {
            databaseHandler = new DatabaseHandler(Program.NoIntroDatPath);

            statusIconsLocation = machineType == typeof(WonderSwan) ? new(0, DisplayControllerCommon.ScreenHeight) : new(DisplayControllerCommon.ScreenWidth, 0);
            graphicsHandler = new GraphicsHandler(machineType, new(emulatorHandler.Machine.ScreenWidth, emulatorHandler.Machine.ScreenHeight), statusIconsLocation, statusIconSize, machineType != typeof(WonderSwan), Program.Configuration.Video.Shader)
            {
                IsVerticalOrientation = isVerticalOrientation
            };

            soundHandler = new SoundHandler(44100, 2);
            soundHandler.SetVolume(1.0f);
            soundHandler.SetMute(Program.Configuration.Sound.Mute);
            soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);

            inputHandler = new InputHandler(renderControl);
            inputHandler.SetKeyMapping(Program.Configuration.Input.GameControls, Program.Configuration.Input.SystemControls);
            inputHandler.SetVerticalOrientation(isVerticalOrientation);
            inputHandler.SetEnableRemapping(Program.Configuration.Input.AutoRemap);
            inputHandler.SetVerticalRemapping(emulatorHandler.Machine.VerticalControlRemap
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToDictionary(x => x[0], x => x[1]));

            emulatorHandler.Machine.DisplayController.SendFramebuffer = graphicsHandler.UpdateScreen;
            emulatorHandler.Machine.SoundController.SendSamples = (s) =>
            {
                soundHandler.EnqueueSamples(s);
                soundRecorderForm.EnqueueSamples(s);
            };

            emulatorHandler.Machine.ReceiveInput += () =>
            {
                var buttonsPressed = new List<string>();
                var buttonsHeld = new List<string>();

                inputHandler.PollInput(ref buttonsPressed, ref buttonsHeld);

                if (buttonsPressed.Contains("Volume"))
                    emulatorHandler.Machine.SoundController.ChangeMasterVolume();

                return (buttonsPressed, buttonsHeld);
            };

            renderControl.Resize += (s, e) => { if (s is Control control) graphicsHandler.Resize(control.ClientRectangle); };
            renderControl.Paint += (s, e) =>
            {
                graphicsHandler.SetClearColor(Color.Black);

                graphicsHandler.ClearFrame();

                if (emulatorHandler.Machine is MachineCommon machine)
                {
                    var activeIcons = new List<string>() { "Power" };

                    if (machine.BuiltInSelfTestOk) activeIcons.Add("Initialized");

                    if (machine.DisplayController.IconSleep) activeIcons.Add("Sleep");
                    if (machine.DisplayController.IconVertical) activeIcons.Add("Vertical");
                    if (machine.DisplayController.IconHorizontal) activeIcons.Add("Horizontal");
                    if (machine.DisplayController.IconAux1) activeIcons.Add("Aux1");
                    if (machine.DisplayController.IconAux2) activeIcons.Add("Aux2");
                    if (machine.DisplayController.IconAux3) activeIcons.Add("Aux3");

                    if (machine.SoundController.HeadphonesConnected) activeIcons.Add("Headphones");
                    if (machine.SoundController.MasterVolume == 0) activeIcons.Add("Volume0");
                    if (machine.SoundController.MasterVolume == 1) activeIcons.Add("Volume1");
                    if (machine.SoundController.MasterVolume == 2) activeIcons.Add("Volume2");
                    if (machine.SoundController.MasterVolume == 3 && machine is WonderSwanColor) activeIcons.Add("Volume3");

                    graphicsHandler.UpdateStatusIcons(activeIcons);
                }
                graphicsHandler.DrawFrame();
            };

            internalEepromPath = Path.Combine(Program.InternalDataPath, $"{machineType.Name}.eep");
        }

        private void InitializeWindows()
        {
            soundRecorderForm = new(soundHandler.SampleRate, soundHandler.NumChannels);
            cheatsForm = new()
            {
                Callback = (c) =>
                {
                    if (emulatorHandler.IsRunning)
                        cheats = (Cheat[])c.Clone();
                }
            };
        }

        private void SizeAndPositionWindow()
        {
            if (WindowState == FormWindowState.Maximized)
                WindowState = FormWindowState.Normal;

            MinimumSize = SizeFromClientSize(CalculateRequiredClientSize(2));
            Size = SizeFromClientSize(CalculateRequiredClientSize(Program.Configuration.Video.ScreenSize));

            var screen = Screen.FromControl(this);
            var workingArea = screen.WorkingArea;
            Location = new Point()
            {
                X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - Width) / 2),
                Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - Height) / 2)
            };
        }

        private Size CalculateRequiredClientSize(int screenSize)
        {
            if (emulatorHandler == null || graphicsHandler == null)
                return ClientSize;

            var statusIconsOnRight = statusIconsLocation.X > statusIconsLocation.Y;

            int screenWidth, screenHeight;

            if (!isVerticalOrientation)
            {
                screenWidth = emulatorHandler.Machine.ScreenWidth;
                screenHeight = emulatorHandler.Machine.ScreenHeight;
                if (statusIconsOnRight) screenWidth += statusIconSize;
                if (!statusIconsOnRight) screenHeight += statusIconSize;
            }
            else
            {
                screenWidth = emulatorHandler.Machine.ScreenHeight;
                screenHeight = emulatorHandler.Machine.ScreenWidth;
                if (!statusIconsOnRight) screenWidth += statusIconSize;
                if (statusIconsOnRight) screenHeight += statusIconSize;
            }

            return new(screenWidth * screenSize, (screenHeight * screenSize) + menuStrip.Height + statusStrip.Height);
        }

        private void SetWindowTitleAndStatus()
        {
            var titleStringBuilder = new StringBuilder();

            titleStringBuilder.Append($"{Application.ProductName} {Program.GetVersionString(false)}");

            if (emulatorHandler.Machine.Cartridge.IsLoaded)
            {
                titleStringBuilder.Append($" - [{Path.GetFileName(Program.Configuration.General.RecentFiles.First())}]");

                var statusStringBuilder = new StringBuilder();
                statusStringBuilder.Append($"Emulating {emulatorHandler.Machine.Manufacturer} {emulatorHandler.Machine.Model}, ");
                statusStringBuilder.Append($"playing {databaseHandler.GetGameTitle(emulatorHandler.Machine.Cartridge.Crc32, emulatorHandler.Machine.Cartridge.SizeInBytes)} ({emulatorHandler.Machine.Cartridge.Metadata.GameIdString})");

                tsslStatus.Text = statusStringBuilder.ToString();
                tsslEmulationStatus.Text = emulatorHandler.IsRunning ? (emulatorHandler.IsPaused ? "Paused" : "Running") : "Stopped";
            }
            else
            {
                tsslStatus.Text = "Ready";
                tsslEmulationStatus.Text = "Stopped";
            }

            Text = titleStringBuilder.ToString();
        }

        private void CreateRecentFilesMenu()
        {
            recentFilesToolStripMenuItem.DropDownItems.Clear();

            var clearMenuItem = new ToolStripMenuItem("&Clear List...");
            clearMenuItem.Click += (s, e) =>
            {
                Program.Configuration.General.RecentFiles.Clear();
                CreateRecentFilesMenu();
            };
            recentFilesToolStripMenuItem.DropDownItems.Add(clearMenuItem);
            recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());

            for (int i = 0; i < maxRecentFiles; i++)
            {
                var file = i < Program.Configuration.General.RecentFiles.Count ? Program.Configuration.General.RecentFiles[i] : null;
                var menuItem = new ToolStripMenuItem(file != null ? file.Replace("&", "&&") : "-")
                {
                    Enabled = file != null,
                    Tag = file
                };
                menuItem.Click += (s, e) =>
                {
                    if ((s as ToolStripMenuItem).Tag is string filename)
                        LoadAndRunCartridge(filename);
                };
                recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        private static void AddToRecentFiles(string filename)
        {
            if (Program.Configuration.General.RecentFiles.Contains(filename))
            {
                var index = Program.Configuration.General.RecentFiles.IndexOf(filename);
                var newList = new List<string>(maxRecentFiles) { filename };
                newList.AddRange(Program.Configuration.General.RecentFiles.Where(x => x != filename));
                Program.Configuration.General.RecentFiles = newList;
            }
            else
            {
                Program.Configuration.General.RecentFiles.Insert(0, filename);
                if (Program.Configuration.General.RecentFiles.Count > maxRecentFiles)
                    Program.Configuration.General.RecentFiles.RemoveAt(Program.Configuration.General.RecentFiles.Count - 1);
            }
        }

        private void CreateScreenSizeMenu()
        {
            screenSizeToolStripMenuItem.DropDownItems.Clear();

            for (int i = 2; i <= maxScreenSizeFactor; i++)
            {
                var menuItem = new ToolStripMenuItem($"{i}x")
                {
                    Checked = Program.Configuration.Video.ScreenSize == i,
                    Tag = i
                };
                menuItem.Click += (s, e) =>
                {
                    if ((s as ToolStripMenuItem).Tag is int screenSizeFactor)
                    {
                        Program.Configuration.Video.ScreenSize = screenSizeFactor;
                        SizeAndPositionWindow();

                        foreach (ToolStripMenuItem screenSizeMenuItem in screenSizeToolStripMenuItem.DropDownItems)
                            screenSizeMenuItem.Checked = (int)screenSizeMenuItem.Tag == Program.Configuration.Video.ScreenSize;
                    }
                };
                screenSizeToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        private void CreateShaderMenu()
        {
            shadersToolStripMenuItem.DropDownItems.Clear();

            foreach (var shaderName in graphicsHandler.AvailableShaders)
            {
                var menuItem = new ToolStripMenuItem(shaderName)
                {
                    Checked = shaderName == Program.Configuration.Video.Shader,
                    Tag = shaderName
                };
                menuItem.Click += (s, e) =>
                {
                    if ((s as ToolStripMenuItem).Tag is string shader)
                    {
                        graphicsHandler?.ChangeShader(shader);

                        Program.Configuration.Video.Shader = shader;
                        foreach (ToolStripMenuItem shaderMenuItem in shadersToolStripMenuItem.DropDownItems)
                            shaderMenuItem.Checked = (shaderMenuItem.Tag as string) == Program.Configuration.Video.Shader;
                    }
                };
                shadersToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        private void SetFileFilters()
        {
            var filters = new List<string>();
            var extensionsList = new List<string>();

            foreach (var (description, extension, _) in supportedFileInformation)
            {
                var currentExtension = $"*{extension}";
                extensionsList.Add(currentExtension);
                filters.Add($"{description} ({currentExtension})|{currentExtension}");
            }

            var allExtensionsFilter = string.Join(";", extensionsList);
            filters.Insert(0, $"All Supported Files ({allExtensionsFilter})|{allExtensionsFilter}");
            filters.Add("All Files (*.*)|*.*");

            ofdOpenRom.Filter = string.Join("|", filters);
        }

        private void CreateDataBinding(ControlBindingsCollection bindings, string propertyName, object dataSource, string dataMember)
        {
            var binding = new Binding(propertyName, dataSource, dataMember, false, DataSourceUpdateMode.OnPropertyChanged);
            bindings.Add(binding);
            uiDataBindings.Add(binding);
        }

        private void InitializeUIMiscellanea()
        {
            // ... aka all the minor stuff I didn't want directly in the Load event, but also doesn't merit a separate function

            uiDataBindings.Clear();

            CreateDataBinding(limitFPSToolStripMenuItem.DataBindings, nameof(limitFPSToolStripMenuItem.Checked), Program.Configuration.General, nameof(Program.Configuration.General.LimitFps));
            limitFPSToolStripMenuItem.CheckedChanged += (s, e) => { emulatorHandler.SetFpsLimiter(Program.Configuration.General.LimitFps); };

            CreateDataBinding(muteSoundToolStripMenuItem.DataBindings, nameof(muteSoundToolStripMenuItem.Checked), Program.Configuration.Sound, nameof(Program.Configuration.Sound.Mute));
            muteSoundToolStripMenuItem.CheckedChanged += (s, e) => { soundHandler.SetMute(Program.Configuration.Sound.Mute); };

            CreateDataBinding(enableCheatsToolStripMenuItem.DataBindings, nameof(enableCheatsToolStripMenuItem.Checked), Program.Configuration.General, nameof(Program.Configuration.General.EnableCheats));
            enableCheatsToolStripMenuItem.CheckedChanged += (s, e) => { emulatorHandler.Machine.ReadMemoryCallback = Program.Configuration.General.EnableCheats ? MachineReadMemoryCallback : default; };

            cheatListToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = false;
        }

        private byte MachineReadMemoryCallback(uint address, byte value)
        {
            /* Critical for performance b/c called on every memory read; do not use "heavy" functionality (ex. LINQ) */

            if (cheats == null || cheats.Length == 0)
                return value;

            foreach (var cheat in cheats)
            {
                if (cheat.Address != address || !cheat.IsEnabled)
                    continue;

                if (cheat.Condition == CheatCondition.Always)
                    return cheat.PatchedValue;
                else if (cheat.Condition == CheatCondition.LessThan && value < cheat.CompareValue)
                    return cheat.PatchedValue;
                else if (cheat.Condition == CheatCondition.LessThanOrEqual && value <= cheat.CompareValue)
                    return cheat.PatchedValue;
                else if (cheat.Condition == CheatCondition.GreaterThanOrEqual && value >= cheat.CompareValue)
                    return cheat.PatchedValue;
                else if (cheat.Condition == CheatCondition.GreaterThan && value > cheat.CompareValue)
                    return cheat.PatchedValue;
            }

            return value;
        }

        private void HandleCommandLineArguments(IEnumerable<string> args)
        {
            var arguments = args.ToArray();

            /* Assume single argument means ROM filepath */
            if (arguments.Length == 1)
            {
                if (TryLoadAndRunCartridge(arguments[0]))
                    Log.WriteEvent(LogSeverity.Information, this, "Loaded ROM via command line.");
                else
                    Log.WriteEvent(LogSeverity.Warning, this, "Command line contained unrecognized file.");
            }
        }

        private void LoadBootstrap(string filename)
        {
            if (GlobalVariables.EnableSkipBootstrapIfFound) return;

            if (!emulatorHandler.IsRunning)
            {
                if (File.Exists(filename))
                {
                    using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var data = new byte[stream.Length];
                    stream.ReadExactly(data);
                    emulatorHandler.Machine.LoadBootstrap(data);
                }
                emulatorHandler.Machine.UseBootstrap = Program.Configuration.General.UseBootstrap;
            }
        }

        private void LoadInternalEeprom()
        {
            if (!emulatorHandler.IsRunning && File.Exists(internalEepromPath))
            {
                using var stream = new FileStream(internalEepromPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var data = new byte[stream.Length];
                stream.ReadExactly(data);
                emulatorHandler.Machine.LoadInternalEeprom(data);
            }
        }

        private bool TryLoadAndRunCartridge(string filename)
        {
            string[] getFilterExtensions(int index) => [.. ExtensionRegex().Matches(ofdOpenRom.Filter)[index].Value.Split('|')[1].Split(';').Select(x => x[x.LastIndexOf('.')..])];

            var result = getFilterExtensions(0).Contains(Path.GetExtension(filename)) && File.Exists(filename);
            if (result) LoadAndRunCartridge(filename);
            return result;
        }

        private static FileStream GetStreamFromFile(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        private static Stream GetStreamFromFirstZippedFile(string filename)
        {
            return new ZipArchive(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).Entries.FirstOrDefault()?.Open();
        }

        private void LoadAndRunCartridge(string filename)
        {
            if (emulatorHandler.IsRunning)
            {
                SaveAllData();
                emulatorHandler.Shutdown();
            }

            using var inputStream = supportedFileInformation.FirstOrDefault(x => x.extension == Path.GetExtension(filename)).streamReadFunc(filename) ?? GetStreamFromFile(filename);
            using var stream = new MemoryStream();
            inputStream.CopyTo(stream);
            stream.Position = 0;

            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            emulatorHandler.Machine.LoadRom(data);

            graphicsHandler.IsVerticalOrientation = isVerticalOrientation = emulatorHandler.Machine.Cartridge.Metadata.Orientation == CartridgeMetadata.Orientations.Vertical;
            inputHandler.SetVerticalOrientation(isVerticalOrientation);

            AddToRecentFiles(filename);
            CreateRecentFilesMenu();

            LoadRam();
            LoadCheats();

            LoadBootstrap(emulatorHandler.Machine is WonderSwan ? Program.Configuration.General.BootstrapFile : Program.Configuration.General.BootstrapFileWSC);
            LoadInternalEeprom();

            emulatorHandler.Startup();

            SizeAndPositionWindow();
            SetWindowTitleAndStatus();

            cheatListToolStripMenuItem.Enabled = pauseToolStripMenuItem.Enabled = resetToolStripMenuItem.Enabled = true;

            Program.SaveConfiguration();
        }

        private void LoadRam()
        {
            var path = Path.Combine(Program.SaveDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.sav");
            if (!File.Exists(path)) return;

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var data = new byte[stream.Length];
            stream.ReadExactly(data);
            if (data.Length != 0)
                emulatorHandler.Machine.LoadSaveData(data);
        }

        private void LoadCheats()
        {
            var path = Path.Combine(Program.CheatsDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.json");
            if (!File.Exists(path)) return;

            cheats = path.DeserializeFromFile<Cheat[]>();
            cheatsForm.SetCheatList(cheats);
        }

        private void SaveAllData()
        {
            SaveInternalEeprom();
            SaveRam();
            SaveCheats();
        }

        private void SaveInternalEeprom()
        {
            var data = emulatorHandler.Machine.GetInternalEeprom();
            if (data.Length == 0) return;

            using var stream = new FileStream(internalEepromPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            stream.Write(data, 0, data.Length);
        }

        private void SaveRam()
        {
            var data = emulatorHandler.Machine.GetSaveData();
            if (data.Length == 0) return;

            var path = Path.Combine(Program.SaveDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.sav");

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            stream.Write(data, 0, data.Length);
        }

        private void SaveCheats()
        {
            if (cheats != null && cheats.Length != 0)
            {
                var path = Path.Combine(Program.CheatsDataPath, $"{Path.GetFileNameWithoutExtension(Program.Configuration.General.RecentFiles.First())}.json");
                cheats.SerializeToFile(path);
            }
        }

        private void PauseEmulation()
        {
            if (!emulatorHandler.IsRunning) return;

            emulatorHandler.Pause();
            soundHandler.Pause();

            SetWindowTitleAndStatus();
        }

        private void UnpauseEmulation()
        {
            if (!emulatorHandler.IsRunning) return;

            emulatorHandler.Unpause();
            soundHandler.Unpause();

            SetWindowTitleAndStatus();
        }

        private void ResetEmulation()
        {
            SaveAllData();
            emulatorHandler.Reset();

            Program.SaveConfiguration();
        }

        private void loadROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseEmulation();

            var lastFile = Program.Configuration.General.RecentFiles.FirstOrDefault();
            if (lastFile != string.Empty)
            {
                ofdOpenRom.FileName = Path.GetFileName(lastFile);
                ofdOpenRom.InitialDirectory = Path.GetDirectoryName(lastFile);
            }

            if (ofdOpenRom.ShowDialog() == DialogResult.OK)
            {
                LoadAndRunCartridge(ofdOpenRom.FileName);
            }

            UnpauseEmulation();
        }

        private void saveWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            soundRecorderForm.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetEmulation();
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!emulatorHandler.IsRunning) return;

            if (sender is ToolStripMenuItem pauseMenuItem)
            {
                if (!pauseMenuItem.Checked)
                {
                    PauseEmulation();
                    pauseMenuItem.Checked = true;
                }
                else
                {
                    UnpauseEmulation();
                    pauseMenuItem.Checked = false;
                }
            }
        }

        private void rotateScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            graphicsHandler.IsVerticalOrientation = isVerticalOrientation = !isVerticalOrientation;
            inputHandler.SetVerticalOrientation(isVerticalOrientation);

            SizeAndPositionWindow();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseEmulation();

            using var dialog = new SettingsForm(Program.Configuration.Clone());
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var requiresRestart = dialog.Configuration.General.PreferOriginalWS != Program.Configuration.General.PreferOriginalWS;

                Program.ReplaceConfiguration(dialog.Configuration);
                VerifyConfiguration();

                foreach (var binding in uiDataBindings) binding.ReadValue();

                if (requiresRestart)
                {
                    if (MessageBox.Show("Changing the system preference setting requires a restart of the emulator to take effect.\n\nDo you want to restart the emulator now?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Application.Restart();
                        Environment.Exit(0);
                    }
                }

                inputHandler.SetKeyMapping(Program.Configuration.Input.GameControls, Program.Configuration.Input.SystemControls);
                inputHandler.SetEnableRemapping(Program.Configuration.Input.AutoRemap);
                soundHandler.SetLowPassFilter(Program.Configuration.Sound.LowPassFilter);

                emulatorHandler.Machine.UseBootstrap = Program.Configuration.General.UseBootstrap;
            }

            UnpauseEmulation();
        }

        private void cheatListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cheatsForm.Show();
        }

        private void openDataFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = $"{Program.DataPath.TrimEnd(Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}",
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseEmulation();

            var builder = new StringBuilder();
            builder.AppendLine($"{Application.ProductName} {Program.GetVersionString(true)}");
            builder.AppendLine($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}");
            builder.AppendLine();
            builder.AppendLine($"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright}");
            builder.AppendLine($"{ThisAssembly.Git.RepositoryUrl}");
            if (GlobalVariables.IsDebugBuild)
            {
                builder.AppendLine();
                builder.AppendLine("This is a HONK!ing debug build! 🔪🦢🖥, y'all!");
            }
            MessageBox.Show(builder.ToString(), $"About {Application.ProductName}", MessageBoxButtons.OK, MessageBoxIcon.Information);

            UnpauseEmulation();
        }

        [GeneratedRegex(@"[^|]+\|[^|]+")]
        private static partial Regex ExtensionRegex();
    }
}

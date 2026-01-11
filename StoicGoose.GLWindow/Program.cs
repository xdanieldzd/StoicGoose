using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using StoicGoose.Common.Extensions;
using StoicGoose.Common.Localization;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace StoicGoose.GLWindow
{
    static class Program
    {
        public readonly static Version RequiredGLVersion = new(4, 0, 0);

        const string jsonConfigFileName = "Config.json";
        const string logFileName = "Log.txt";

        const string internalDataDirectoryName = "Internal";
        const string saveDataDirectoryName = "Saves";
        const string debuggingDataDirectoryName = "Debugging";

        readonly static FileVersionInfo assemblyVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        public static string ProductName => assemblyVersionInfo.ProductName;

        readonly static string mutexName = $"{assemblyVersionInfo.ProductName}_{GetVersionDetails()}";

        readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), assemblyVersionInfo.ProductName);
        readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

        public static Configuration Configuration { get; private set; } = LoadConfiguration(programConfigPath);

        public static string InternalDataPath { get; } = string.Empty;
        public static string SaveDataPath { get; } = string.Empty;
        public static string DebuggingDataPath { get; } = string.Empty;

        public static Dictionary<Type, string> InternalEepromFilenames { get; } = new();

        static Program()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = new(Configuration.Language);

            Localizer.Initialize(Resources.GetEmbeddedText("Assets.Localization.json"));

            Log.Initialize(Path.Combine(programDataDirectory, logFileName));

            Directory.CreateDirectory(InternalDataPath = Path.Combine(programDataDirectory, internalDataDirectoryName));
            Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));
            Directory.CreateDirectory(DebuggingDataPath = Path.Combine(programDataDirectory, debuggingDataDirectoryName));

            IMachine.GetMachineTypes().ToList().ForEach(x => InternalEepromFilenames.Add(x, $"{x.Name}.eep"));
        }

        static void Main()
        {
            using var mutex = new Mutex(true, mutexName, out bool newInstance);
            if (!newInstance) Environment.Exit(-1);

#if !DEBUG
            try
#endif
            {
                var windowIcon = Resources.GetEmbeddedRgbaFile("Assets.WS-Icon.rgba");

                using var mainWindow = new MainWindow(new()
                {
                    UpdateFrequency = 0.0
                }, new()
                {
                    Size = new(1280, 720),
                    Title = $"{assemblyVersionInfo.ProductName} {GetVersionString(false)}",
                    Flags = ContextFlags.Default,
                    API = ContextAPI.OpenGL,
                    APIVersion = RequiredGLVersion,
                    Icon = new(new Image((int)windowIcon.Width, (int)windowIcon.Height, windowIcon.PixelData))
                })
                {
                    VSync = VSyncMode.Off
                };
                mainWindow.Run();
            }
#if !DEBUG
            catch (OpenTK.Windowing.GraphicsLibraryFramework.GLFWException ex)
            {
                ShutdownOnFatalError(ex, Localizer.GetString("Program.WrongOpenGLVersion", new { ProductName, MajorGLVersion = RequiredGLVersion.Major, MinorGLVersion = RequiredGLVersion.Minor }));
            }
            catch (Exception ex)
            {
                ShutdownOnFatalError(ex);
            }
#endif
        }

#if !DEBUG
        private static void ShutdownOnFatalError(Exception ex, string otherMessage = null)
        {
            Log.WriteFatal(ConstructFatalErrorMessage(ex));
            if (otherMessage != null) Log.WriteFatal(otherMessage);
            Log.WriteFatal(Localizer.GetString("Program.ShuttingDown"));
            Process.Start(new ProcessStartInfo(Log.LogPath) { UseShellExecute = true });
            Environment.Exit(-1);
        }

        private static string ConstructFatalErrorMessage(Exception ex)
        {
            var stackFrame = new StackTrace(ex, true).GetFrame(0);
            if (stackFrame.HasMethod() && stackFrame.HasSource())
                return $"{ex.GetType().Name} in {stackFrame.GetMethod().DeclaringType.FullName} {stackFrame.GetMethod()} ({Path.GetFileName(stackFrame.GetFileName())}:{stackFrame.GetFileLineNumber()}): {ex.Message}";
            else if (stackFrame.HasMethod())
                return $"{ex.GetType().Name} in {stackFrame.GetMethod().DeclaringType.FullName} {stackFrame.GetMethod()}: {ex.Message}";
            else
                return $"{ex.GetType().Name}: {ex.Message}";
        }
#endif

        private static Configuration LoadConfiguration(string filename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filename));

            Configuration configuration;
            if (!File.Exists(filename) || (configuration = filename.DeserializeFromFile<Configuration>()) == null)
            {
                configuration = new Configuration();
                configuration.SerializeToFile(filename);
            }

            return configuration;
        }

        public static void ReplaceConfiguration(Configuration newConfig)
        {
            ConfigurationBase.CopyConfiguration(newConfig, Configuration);
            SaveConfiguration();
        }

        public static void SaveConfiguration()
        {
            Configuration?.SerializeToFile(programConfigPath);
        }

        private static string GetVersionDetails()
        {
            return $"{ThisAssembly.Git.Branch}-{ThisAssembly.Git.Commit}{(ThisAssembly.Git.IsDirty ? "-dirty" : string.Empty)}{(GlobalVariables.IsDebugBuild ? "+debug" : string.Empty)}";
        }

        public static string GetVersionString(bool detailed)
        {
            var version = new Version(assemblyVersionInfo.FileVersion);
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"v{version.Major:D3}{(version.Minor != 0 ? $".{version.Minor}" : string.Empty)}");
            if (detailed) stringBuilder.Append($" ({GetVersionDetails()})");
            return stringBuilder.ToString();
        }
    }
}

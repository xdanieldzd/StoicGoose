using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;

using StoicGoose.Common.Extensions;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.GLWindow
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";

		const string internalDataDirectoryName = "Internal";
		const string saveDataDirectoryName = "Saves";
		const string debuggingDataDirectoryName = "Debugging";

		readonly static FileVersionInfo assemblyVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

		public static string ProductName => assemblyVersionInfo.ProductName;

		readonly static string mutexName = $"{assemblyVersionInfo.ProductName}/{GetVersionDetails()}";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), assemblyVersionInfo.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; private set; } = LoadConfiguration(programConfigPath);

		public static string InternalDataPath { get; } = string.Empty;
		public static string SaveDataPath { get; } = string.Empty;
		public static string DebuggingDataPath { get; } = string.Empty;

		public static Dictionary<Type, string> InternalEepromFilenames { get; } = new();

		static Program()
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Directory.CreateDirectory(InternalDataPath = Path.Combine(programDataDirectory, internalDataDirectoryName));
			Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));
			Directory.CreateDirectory(DebuggingDataPath = Path.Combine(programDataDirectory, debuggingDataDirectoryName));

			IMachine.GetMachineTypes().ToList().ForEach(x => InternalEepromFilenames.Add(x, $"{x.Name}.eep"));
		}

		static void Main(string[] _)
		{
			using var mutex = new Mutex(true, mutexName, out bool newInstance);
			if (!newInstance) Environment.Exit(-1);

			var windowIcon = Resources.GetEmbeddedRgbaFile("Assets.WS-Icon.rgba");

			using var mainWindow = new MainWindow(new()
			{
				RenderFrequency = 0.0,
				UpdateFrequency = 0.0
			}, new()
			{
				Size = new(1280, 720),
				Title = $"{assemblyVersionInfo.ProductName} {GetVersionString(false)}",
				Flags = ContextFlags.Default,
				API = ContextAPI.OpenGL,
				APIVersion = new(4, 6, 0),
				Icon = new(new Image((int)windowIcon.Width, (int)windowIcon.Height, windowIcon.PixelData))
			})
			{
				VSync = VSyncMode.Off
			};
			mainWindow.Run();
		}

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
			if (Configuration != null)
				Configuration.SerializeToFile(programConfigPath);
		}

		private static string GetVersionDetails()
		{
			return $"{ThisAssembly.Git.Branch}-{ThisAssembly.Git.Commit}{(ThisAssembly.Git.IsDirty ? "-dirty" : string.Empty)}{(GlobalVariables.IsDebugBuild ? "+debug" : string.Empty)}";
		}

		public static string GetVersionString(bool detailed)
		{
			var version = new Version(assemblyVersionInfo.ProductVersion);
			var stringBuilder = new StringBuilder();
			stringBuilder.Append($"v{version.Major:D3}{(version.Minor != 0 ? $".{version.Minor}" : string.Empty)}");
			if (detailed) stringBuilder.Append($" ({GetVersionDetails()})");
			return stringBuilder.ToString();
		}
	}
}

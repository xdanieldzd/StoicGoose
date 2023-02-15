using StoicGoose.Common.Extensions;
using StoicGoose.Common.Localization;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StoicGoose.WPF
{
	public partial class App : Application
	{
		public readonly static Version RequiredGLVersion = new(4, 0, 0);

		const string jsonConfigFileName = "Config.json";

		const string internalDataDirectoryName = "Internal";
		const string saveDataDirectoryName = "Saves";

		const string assetsDirectoryName = "Assets";
		const string shaderDirectoryName = "Shaders";

		readonly static FileVersionInfo assemblyVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

		public static string ProductName => assemblyVersionInfo.ProductName;

		readonly static string mutexName = $"{assemblyVersionInfo.ProductName}_{GetVersionDetails()}";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), assemblyVersionInfo.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; private set; } = LoadConfiguration(programConfigPath);

		public static string InternalDataPath { get; } = string.Empty;
		public static string SaveDataPath { get; } = string.Empty;

		public static Dictionary<Type, string> InternalEepromFilenames { get; } = new();

		readonly static string programApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
		readonly static string programAssetsDirectory = Path.Combine(programApplicationDirectory, assetsDirectoryName);

		public static string ShaderPath { get; } = string.Empty;

		static App()
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

			Directory.CreateDirectory(InternalDataPath = Path.Combine(programDataDirectory, internalDataDirectoryName));
			Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));

			Directory.CreateDirectory(ShaderPath = Path.Combine(programAssetsDirectory, shaderDirectoryName));

			IMachine.GetMachineTypes().ToList().ForEach(x => InternalEepromFilenames.Add(x, $"{x.Name}.eep"));
		}

		App()
		{
			using var mutex = new Mutex(true, mutexName, out bool newInstance);
			if (!newInstance) Environment.Exit(-1);
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
			Configuration?.SerializeToFile(programConfigPath);
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

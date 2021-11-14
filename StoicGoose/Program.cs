using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Text;

using StoicGoose.Extensions;
using StoicGoose.Interface;

namespace StoicGoose
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";
		const string saveDataDirectoryName = "Saves";

		const string assetsDirectoryName = "Assets";
		const string shaderDirectoryName = "Shaders";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; } = LoadConfiguration(programConfigPath);
		public static string SaveDataPath { get; } = string.Empty;

		readonly static string programApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
		readonly static string programAssetsDirectory = Path.Combine(programApplicationDirectory, assetsDirectoryName);

		public static string ShaderPath { get; } = string.Empty;

		static Program()
		{
			Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));

			if (!Directory.Exists(ShaderPath = Path.Combine(programAssetsDirectory, shaderDirectoryName)))
				throw new DirectoryNotFoundException("Shader directory missing");
		}

		[STAThread]
		static void Main()
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
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

		public static void SaveConfiguration()
		{
			Configuration.SerializeToFile(programConfigPath);
		}

		public static string GetVersionString(bool detailed)
		{
			var version = new Version(Application.ProductVersion);
			var stringBuilder = new StringBuilder();
			stringBuilder.Append($"v{version.Major:D3}{(version.Minor != 0 ? $".{version.Minor}" : string.Empty)}");
			if (detailed) stringBuilder.Append($" ({ThisAssembly.Git.Branch}-{ThisAssembly.Git.Commit}{(ThisAssembly.Git.IsDirty ? "-dirty" : string.Empty)}{(GlobalVariables.IsDebugBuild ? "+debug" : string.Empty)})");
			return stringBuilder.ToString();
		}
	}
}

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using StoicGoose.Common.Extensions;
using StoicGoose.Common.Utilities;
using StoicGoose.DataStorage;

namespace StoicGoose
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";

		const string internalDataDirectoryName = "Internal";
		const string saveDataDirectoryName = "Saves";
		const string cheatDataDirectoryName = "Cheats";
		const string debuggingDataDirectoryName = "Debugging";

		const string assetsDirectoryName = "Assets";
		const string shaderDirectoryName = "Shaders";
		const string noIntroDatDirectoryName = "No-Intro";

		readonly static string mutexName = $"{Application.ProductName}/{GetVersionDetails()}";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; private set; } = LoadConfiguration(programConfigPath);

		public static string InternalDataPath { get; } = string.Empty;
		public static string SaveDataPath { get; } = string.Empty;
		public static string CheatsDataPath { get; } = string.Empty;
		public static string DebuggingDataPath { get; } = string.Empty;

		readonly static string programApplicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
		readonly static string programAssetsDirectory = Path.Combine(programApplicationDirectory, assetsDirectoryName);

		public static string ShaderPath { get; } = string.Empty;
		public static string NoIntroDatPath { get; } = string.Empty;

		static Program()
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			Directory.CreateDirectory(InternalDataPath = Path.Combine(programDataDirectory, internalDataDirectoryName));
			Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));
			Directory.CreateDirectory(CheatsDataPath = Path.Combine(programDataDirectory, cheatDataDirectoryName));
			Directory.CreateDirectory(DebuggingDataPath = Path.Combine(programDataDirectory, debuggingDataDirectoryName));

			if (!Directory.Exists(ShaderPath = Path.Combine(programAssetsDirectory, shaderDirectoryName)))
				throw new DirectoryNotFoundException("Shader directory missing");

			if (!Directory.Exists(NoIntroDatPath = Path.Combine(programAssetsDirectory, noIntroDatDirectoryName)))
				throw new DirectoryNotFoundException("No-Intro .dat directory missing");
		}

		[STAThread]
		static void Main()
		{
			/*
			var bmp = new System.Drawing.Bitmap(@"D:\User Data\Pictures\WS\Goose-Logo.png");
			var b = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
			var data = new byte[bmp.Width * bmp.Height * 4];
			System.Runtime.InteropServices.Marshal.Copy(b.Scan0, data, 0, data.Length);
			bmp.UnlockBits(b);
			var tmp = new StoicGoose.Common.Drawing.RgbaFile((uint)bmp.Width, (uint)bmp.Height, data);
			tmp.Save(@"D:\User Data\Pictures\WS\Goose-Logo.rgba");

			return;
			*/

			using var mutex = new Mutex(true, mutexName, out bool newInstance);
			if (!newInstance)
			{
				MessageBox.Show($"Another instance of {Application.ProductName} is already running.\n\nThis instance will now shut down.", $"{Application.ProductName} Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

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
			var version = new Version(Application.ProductVersion);
			var stringBuilder = new StringBuilder();
			stringBuilder.Append($"v{version.Major:D3}{(version.Minor != 0 ? $".{version.Minor}" : string.Empty)}");
			if (detailed) stringBuilder.Append($" ({GetVersionDetails()})");
			return stringBuilder.ToString();
		}
	}
}

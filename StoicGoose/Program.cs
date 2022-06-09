using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using StoicGoose.Common.Extensions;
using StoicGoose.Common.Utilities;

using GLFWException = OpenTK.Windowing.GraphicsLibraryFramework.GLFWException;

namespace StoicGoose
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";
		const string logFileName = "Log.txt";

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

		static MainForm mainForm = default;

		static Program()
		{
			try
			{
				Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

				Log.Initialize(Path.Combine(programDataDirectory, logFileName));

				Directory.CreateDirectory(InternalDataPath = Path.Combine(programDataDirectory, internalDataDirectoryName));
				Directory.CreateDirectory(SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName));
				Directory.CreateDirectory(CheatsDataPath = Path.Combine(programDataDirectory, cheatDataDirectoryName));
				Directory.CreateDirectory(DebuggingDataPath = Path.Combine(programDataDirectory, debuggingDataDirectoryName));

				if (!Directory.Exists(ShaderPath = Path.Combine(programAssetsDirectory, shaderDirectoryName)))
					throw new DirectoryNotFoundException("Shader directory missing");

				if (!Directory.Exists(NoIntroDatPath = Path.Combine(programAssetsDirectory, noIntroDatDirectoryName)))
					throw new DirectoryNotFoundException("No-Intro .dat directory missing");
			}
			catch (DirectoryNotFoundException e)
			{
				MessageBox.Show($"Failed to start application: {e.Message}.\n\nPlease ensure that all files have been extracted.",
					$"{Application.ProductName} Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}
		}

		[STAThread]
		static void Main()
		{
			using var mutex = new Mutex(true, mutexName, out bool newInstance);
			if (!newInstance)
			{
				MessageBox.Show($"Another instance of {Application.ProductName} is already running.\n\nThis instance will now shut down.",
					$"{Application.ProductName} Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Environment.Exit(-1);
			}

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (!Debugger.IsAttached)
			{
				Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
				AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			}

			Application.Run(mainForm = new MainForm());
		}

		static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
			if (e.Exception is GLFWException glEx)
			{
				var renderControl = mainForm.Controls["renderControl"] as OpenGL.RenderControl;
				MessageBox.Show($"{glEx.Message.EnsureEndsWithPeriod()}\n\n{Application.ProductName} requires GPU and drivers supporting OpenGL {renderControl.APIVersion.Major}.{renderControl.APIVersion.Minor}.", $"{Application.ProductName} Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				MessageBox.Show(e.Exception.Message.EnsureEndsWithPeriod(), $"{Application.ProductName} Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			Environment.Exit(-1);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message, $"{Application.ProductName} Startup Error");
			Environment.Exit(-1);
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

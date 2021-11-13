using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

using StoicGoose.Extensions;
using StoicGoose.Interface;

namespace StoicGoose
{
	static class Program
	{
		const string jsonConfigFileName = "Config.json";
		const string saveDataDirectoryName = "Saves";

		readonly static string programDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Application.ProductName);
		readonly static string programConfigPath = Path.Combine(programDataDirectory, jsonConfigFileName);

		public static Configuration Configuration { get; set; }

		public static string SaveDataPath = Path.Combine(programDataDirectory, saveDataDirectoryName);

		[STAThread]
		static void Main()
		{
			Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			LoadConfiguration();

			if (!Directory.Exists(SaveDataPath))
				Directory.CreateDirectory(SaveDataPath);

			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}

		private static void LoadConfiguration()
		{
			Directory.CreateDirectory(programDataDirectory);

			if (!File.Exists(programConfigPath) || (Configuration = programConfigPath.DeserializeFromFile<Configuration>()) == null)
			{
				Configuration = new Configuration();
				Configuration.SerializeToFile(programConfigPath);
			}
		}

		public static void SaveConfiguration()
		{
			Configuration.SerializeToFile(programConfigPath);
		}
	}
}

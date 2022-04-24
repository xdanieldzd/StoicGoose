using System;
using System.IO;
using System.Linq;

using Microsoft.Win32;

namespace StoicGoose
{
	public static class Utilities
	{
		readonly static RegistryKey[] fontRegistryKeys = new RegistryKey[]
		{
			Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
			Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")
		};

		public static string GetSystemFontFilePath(string name)
		{
			foreach (var key in fontRegistryKeys.Where(x => x != null))
			{
				var fullName = key.GetValueNames().FirstOrDefault(x => x.StartsWith(name));
				if (key.GetValue(fullName) is string value)
				{
					var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), value);
					if (File.Exists(path)) return path;
				}
			}
			return null;
		}
	}
}

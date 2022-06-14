using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using Microsoft.Win32;

using StoicGoose.Common.Drawing;
using StoicGoose.Common.Utilities;

namespace StoicGoose.WinForms
{
	public static class Utilities
	{
		[SupportedOSPlatform("windows")]
		readonly static RegistryKey[] fontRegistryKeys = new RegistryKey[]
		{
			Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
			Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")
		};

		[SupportedOSPlatform("windows")]
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

		public static RgbaFile GetEmbeddedSystemIcon(string name) => Resources.GetEmbeddedRgbaFile($"Assets.Icons.{name}");
		public static string GetEmbeddedShaderFile(string name) => Resources.GetEmbeddedText($"Assets.Shaders.{name}");
	}
}

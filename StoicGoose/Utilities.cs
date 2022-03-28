﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.Win32;

using StoicGoose.Handlers;

namespace StoicGoose
{
	public static class Utilities
	{
		readonly static RegistryKey[] fontRegistryKeys = new RegistryKey[]
		{
			Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"),
			Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts")
		};

		public static void ChangeBit(ref byte value, int bit, bool state)
		{
			if (state)
				value |= (byte)(1 << bit);
			else
				value &= (byte)~(1 << bit);
		}

		public static bool IsBitSet(byte value, int bit) => (value & (1 << bit)) != 0;

		public static int GetBase(string value) => value.StartsWith("0x") ? 16 : 10;

		public static void Swap<T>(ref T a, ref T b)
		{
			var tmp = a;
			a = b;
			b = tmp;
		}

		private static Stream GetEmbeddedResourceStream(string name)
		{
			var assembly = Assembly.GetExecutingAssembly();
			name = $"{Application.ProductName}.{name}";
			return assembly.GetManifestResourceStream(name);
		}

		public static Bitmap GetEmbeddedBitmap(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return null;
			return new Bitmap(stream);
		}

		public static string GetEmbeddedText(string name)
		{
			using var stream = GetEmbeddedResourceStream(name);
			if (stream == null) return string.Empty;
			using var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		public static Bitmap GetEmbeddedSystemIcon(string name)
		{
			return GetEmbeddedBitmap($"Assets.Icons.{name}");
		}

		public static string GetEmbeddedShaderSource(string name)
		{
			return GetEmbeddedText($"Assets.Shaders.{name}");
		}

		public static string GetEmbeddedShaderBundleManifest(string name)
		{
			return GetEmbeddedText($"Assets.Shaders.{name}.{GraphicsHandler.DefaultManifestFilename}");
		}

		public static string GetEmbeddedShaderBundleSource(string name)
		{
			return GetEmbeddedText($"Assets.Shaders.{name}.{GraphicsHandler.DefaultSourceFilename}");
		}

		public static int DecimalToBcd(int value)
		{
			return ((value / 10) << 4) + (value % 10);
		}

		public static int BcdToDecimal(int value)
		{
			return ((value >> 4) * 10) + value % 16;
		}

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

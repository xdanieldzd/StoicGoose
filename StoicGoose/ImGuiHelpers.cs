using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose
{
	public static class ImGuiHelpers
	{
		readonly static List<Type> typeList = new()
		{
			typeof(byte),
			typeof(sbyte),
			typeof(ushort),
			typeof(short),
			typeof(uint),
			typeof(int),
			typeof(ulong),
			typeof(long),
		};

		readonly static Dictionary<Type, MethodInfo> typeParseMethodDict = new();

		static ImGuiHelpers()
		{
			foreach (var type in typeList)
			{
				var method = type.GetMethod("Parse", new[] { typeof(string), typeof(NumberStyles) });
				if (method != null) typeParseMethodDict.Add(type, method);
			}
		}

		public static (bool, T) InputHex<T>(string label, T value, int digits, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, ImGuiInputTextCallback callback = null) where T : unmanaged, IComparable, IEquatable<T>
		{
			var type = typeof(T);
			if (!typeParseMethodDict.ContainsKey(type)) throw new ArgumentException("Invalid type for input", nameof(T));

			var textFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.EnterReturnsTrue | flags;

			var stringValue = string.Format($"{{0:X{digits}}}", value);
			ImGui.SetNextItemWidth(ImGui.CalcTextSize(stringValue).X + ImGui.GetStyle().ItemSpacing.X);

			var result = ImGui.InputText(label, ref stringValue, (uint)digits, textFlags, callback);
			if (!string.IsNullOrEmpty(stringValue)) value = (T)typeParseMethodDict[type].Invoke(null, new object[] { stringValue, NumberStyles.HexNumber });
			return (result, value);
		}

		public static bool InputHex<T>(string label, ref T value, int digits, bool autoSize = true) where T : unmanaged, IComparable, IEquatable<T>
		{
			var type = typeof(T);
			if (!typeParseMethodDict.ContainsKey(type)) throw new ArgumentException("Invalid type for input", nameof(T));

			var textFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase | ImGuiInputTextFlags.EnterReturnsTrue;

			var stringValue = string.Format($"{{0:X{digits}}}", value);
			if (autoSize) ImGui.SetNextItemWidth(ImGui.CalcTextSize(stringValue).X + ImGui.GetStyle().ItemSpacing.X);

			var result = ImGui.InputText(label, ref stringValue, (uint)digits, textFlags);
			if (!string.IsNullOrEmpty(stringValue)) value = (T)typeParseMethodDict[type].Invoke(null, new object[] { stringValue, NumberStyles.HexNumber });
			return result;
		}
	}
}

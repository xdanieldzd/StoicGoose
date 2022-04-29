using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public static class ImGuiHelpers
	{
		const string helpMarker = "(?)";

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

		/* https://github.com/ocornut/imgui/blob/f5c5926fb91764c2ec0e995970818d79b5873d42/imgui_demo.cpp#L191 */
		public static void HelpMarker(string desc)
		{
			ImGui.TextDisabled(helpMarker);
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		public static float HelpMarkerWidth() => ImGui.CalcTextSize(helpMarker).X;

		public static bool IsPointInsideRectangle(NumericsVector2 point, NumericsVector2 rectPos, NumericsVector2 rectSize)
		{
			return point.X >= rectPos.X && point.X < rectPos.X + rectSize.X && point.Y >= rectPos.Y && point.Y < rectPos.Y + rectSize.Y;
		}
	}
}

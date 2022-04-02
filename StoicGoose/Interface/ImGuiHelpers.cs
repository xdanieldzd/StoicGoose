using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface
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
			ImGui.TextDisabled("(?)");
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}

		public static void OpenMessageBox(string title)
		{
			ImGui.OpenPopup(title);
		}

		public static int ProcessMessageBox(string message, string title, params string[] buttons)
		{
			var buttonIdx = -1;

			var viewportCenter = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

			var popupDummy = true;
			if (ImGui.BeginPopupModal($"{title}", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
			{
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));
				ImGui.Text(message);

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				var buttonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * (buttons.Length - 1))) / buttons.Length;
				for (var i = 0; i < buttons.Length; i++)
				{
					if (ImGui.Button(buttons[i], new NumericsVector2(buttonWidth, 0f)))
					{
						ImGui.CloseCurrentPopup();
						buttonIdx = i;
						break;
					}
					ImGui.SameLine();
				}

				ImGui.PopStyleVar();

				ImGui.EndPopup();
			}

			return buttonIdx;
		}

		public static bool IsPointInsideRectangle(NumericsVector2 point, NumericsVector2 rectPos, NumericsVector2 rectSize)
		{
			return point.X >= rectPos.X && point.X < rectPos.X + rectSize.X && point.Y >= rectPos.Y && point.Y < rectPos.Y + rectSize.Y;
		}
	}
}

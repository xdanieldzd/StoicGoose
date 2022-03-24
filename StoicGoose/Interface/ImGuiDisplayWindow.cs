﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ImGuiNET;

using StoicGoose.Emulation.Display;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface
{
	public class ImGuiDisplayWindow<T> : ImGuiWindowBase where T : DisplayControllerCommon
	{
		const BindingFlags getPropBindingFlags = BindingFlags.Public | BindingFlags.Instance;

		readonly static Dictionary<(ushort number, string name), List<RegisterParameterInformation>> displayRegisters = new();

		public ImGuiDisplayWindow() : base("Display Status", new NumericsVector2(500f, 300f), ImGuiCond.FirstUseEver) { }

		static ImGuiDisplayWindow()
		{
			foreach (var propInfo in typeof(T).GetProperties(getPropBindingFlags))
			{
				if (propInfo.GetCustomAttribute<ImGuiRegisterAttribute>() is ImGuiRegisterAttribute regAttrib)
				{
					var key = (regAttrib.Number, regAttrib.Name);
					if (!displayRegisters.ContainsKey(key))
						displayRegisters.Add(key, new());
				}
			}

			foreach (var ((number, name), list) in displayRegisters)
			{
				foreach (var propInfo in typeof(T).GetProperties(getPropBindingFlags)
					.Where(x => x.GetCustomAttribute<ImGuiRegisterAttribute>()?.Number == number && x.GetCustomAttribute<ImGuiRegisterAttribute>()?.Name == name)
					.GroupBy(x => x.Name)
					.Select(x => x.First()))
				{
					var descAttrib = propInfo.GetCustomAttribute<ImGuiBitDescriptionAttribute>();
					var formatAttrib = propInfo.GetCustomAttribute<ImGuiFormatAttribute>();

					list.Add(new RegisterParameterInformation()
					{
						Index = descAttrib.LowBit,
						Description = descAttrib != null ? $"{descAttrib.BitString}{descAttrib.Description}" : "<no description>",
						FormatString = formatAttrib?.Format ?? string.Empty,
						BitShift = formatAttrib?.Shift ?? 0,
						PropInfo = propInfo
					});
				}
			}
		}

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not T display) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				foreach (var ((number, name), list) in displayRegisters.OrderBy(x => x.Key.number))
				{
					if (ImGui.CollapsingHeader($"0x{number:X3} -- {name}", ImGuiTreeNodeFlags.DefaultOpen))
					{
						ImGui.BeginDisabled(true);
						{
							foreach (var entry in list.OrderBy(x => x.Index))
							{
								var val = entry.PropInfo.GetValue(display);
								if (val is bool valBool)
									ImGui.Checkbox(entry.Description, ref valBool);
								else if (!string.IsNullOrEmpty(entry.FormatString))
									ImGui.LabelText(string.Format($"{{0:{entry.FormatString}}}", Convert.ToUInt64(val) << entry.BitShift), entry.Description);
								else
									ImGui.LabelText($"{val}", entry.Description);
							}
						}
						ImGui.EndDisabled();
					}
				}
			}
			ImGui.End();
		}

		class RegisterParameterInformation
		{
			public int Index;
			public string Description;
			public string FormatString;
			public int BitShift;
			public PropertyInfo PropInfo;

			public RegisterParameterInformation()
			{
				Index = -1;
				Description = string.Empty;
				FormatString = string.Empty;
				BitShift = 0;
				PropInfo = default;
			}
		}
	}
}

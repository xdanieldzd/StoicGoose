using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ImGuiNET;

using StoicGoose.Common.Attributes;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class ComponentRegisterWindow : WindowBase
	{
		const BindingFlags getPropBindingFlags = BindingFlags.Public | BindingFlags.Instance;

		readonly Dictionary<string, (List<ushort> numbers, List<RegisterParameterInformation> regInfos)> registers = new();

		Type componentType = default;

		public ComponentRegisterWindow(string title) : base(title, new NumericsVector2(500f, 300f), ImGuiCond.FirstUseEver) { }

		public void SetComponentType(Type type)
		{
			if (componentType != type)
			{
				componentType = type;

				registers.Clear();

				foreach (var propInfo in componentType.GetProperties(getPropBindingFlags).Where(x => !x.GetGetMethod().IsAbstract))
				{
					if (propInfo.GetCustomAttribute<RegisterAttribute>() is RegisterAttribute regAttrib)
					{
						if (!registers.ContainsKey(regAttrib.Name))
							registers.Add(regAttrib.Name, (regAttrib.Numbers, new()));
					}
				}

				foreach (var (name, (numbers, list)) in registers)
				{
					foreach (var propInfo in componentType.GetProperties(getPropBindingFlags)
						.Where(x => x.GetCustomAttribute<RegisterAttribute>()?.Numbers.SequenceEqual(numbers) == true && x.GetCustomAttribute<RegisterAttribute>()?.Name == name)
						.GroupBy(x => x.Name)
						.Select(x => x.First()))
					{
						var descAttrib = propInfo.GetCustomAttribute<BitDescriptionAttribute>();
						var formatAttrib = propInfo.GetCustomAttribute<FormatAttribute>();

						list.Add(new RegisterParameterInformation()
						{
							Index = descAttrib?.LowBit ?? 0,
							Description = descAttrib != null ? $"{descAttrib.BitString}{descAttrib.Description}" : "<no description>",
							FormatString = formatAttrib?.Format ?? string.Empty,
							BitShift = formatAttrib?.Shift ?? 0,
							PropInfo = propInfo
						});
					}
				}
			}
		}

		protected override void DrawWindow(object userData)
		{
			if (userData.GetType() != componentType) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				foreach (var (name, (numbers, list)) in registers.OrderBy(x => x.Value.numbers.Min()))
				{
					if (ImGui.CollapsingHeader($"{string.Join(", ", numbers.Select(x => $"0x{x:X3}"))} -- {name}", ImGuiTreeNodeFlags.DefaultOpen))
					{
						ImGui.BeginDisabled(true);
						{
							foreach (var entry in list.OrderBy(x => x.Index))
							{
								var val = entry.PropInfo.GetValue(userData, null);
								if (val is bool valBool)
									ImGui.Checkbox(entry.Description, ref valBool);
								else if (!string.IsNullOrEmpty(entry.FormatString))
									ImGui.LabelText(string.Format($"{{0:{entry.FormatString}}}", Convert.ToUInt64(val) << entry.BitShift), entry.Description);
								else
									ImGui.LabelText($"{val}", entry.Description);
							}

							ImGui.EndDisabled();
						}
					}
				}

				ImGui.End();
			}
		}
	}

	class RegisterParameterInformation
	{
		public int Index { get; set; } = -1;
		public string Description { get; set; } = string.Empty;
		public string FormatString { get; set; } = string.Empty;
		public int BitShift { get; set; } = 0;
		public PropertyInfo PropInfo { get; set; } = default;
	}
}

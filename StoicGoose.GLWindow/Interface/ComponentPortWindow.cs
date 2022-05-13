using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ImGuiNET;

using StoicGoose.Common.Attributes;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class ComponentPortWindow : WindowBase
	{
		const BindingFlags getPropBindingFlags = BindingFlags.Public | BindingFlags.Instance;

		readonly Dictionary<string, (List<ushort> numbers, List<PortParameterInformation> portInfos)> ports = new();

		Type componentType = default;

		public ComponentPortWindow(string title) : base(title, new NumericsVector2(500f, 300f), ImGuiCond.FirstUseEver) { }

		public void SetComponentType(Type type)
		{
			if (componentType != type)
			{
				componentType = type;

				ports.Clear();

				foreach (var propInfo in componentType.GetProperties(getPropBindingFlags).Where(x => !x.GetGetMethod().IsAbstract))
				{
					if (propInfo.GetCustomAttribute<PortAttribute>() is PortAttribute regAttrib)
					{
						if (!ports.ContainsKey(regAttrib.Name))
							ports.Add(regAttrib.Name, (regAttrib.Numbers, new()));
					}
				}

				foreach (var (name, (numbers, list)) in ports)
				{
					foreach (var propInfo in componentType.GetProperties(getPropBindingFlags)
						.Where(x => x.GetCustomAttribute<PortAttribute>()?.Numbers.SequenceEqual(numbers) == true && x.GetCustomAttribute<PortAttribute>()?.Name == name)
						.GroupBy(x => x.Name)
						.Select(x => x.First()))
					{
						var descAttrib = propInfo.GetCustomAttribute<BitDescriptionAttribute>();
						var formatAttrib = propInfo.GetCustomAttribute<FormatAttribute>();

						list.Add(new PortParameterInformation()
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
				foreach (var (name, (numbers, list)) in ports.OrderBy(x => x.Value.numbers.Min()))
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

	class PortParameterInformation
	{
		public int Index { get; set; } = -1;
		public string Description { get; set; } = string.Empty;
		public string FormatString { get; set; } = string.Empty;
		public int BitShift { get; set; } = 0;
		public PropertyInfo PropInfo { get; set; } = default;
	}
}

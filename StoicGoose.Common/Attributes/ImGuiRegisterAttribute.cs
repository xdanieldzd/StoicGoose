using System;
using System.Collections.Generic;

namespace StoicGoose.Common.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ImGuiRegisterAttribute : Attribute
	{
		public string Name { get; set; } = string.Empty;
		public List<ushort> Numbers { get; set; } = new();

		public ImGuiRegisterAttribute(string name, params ushort[] numbers)
		{
			Name = name;
			Numbers.AddRange(numbers);
		}
	}
}

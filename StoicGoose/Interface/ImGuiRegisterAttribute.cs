using System;

namespace StoicGoose.Interface
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ImGuiRegisterAttribute : Attribute
	{
		public ushort Number { get; set; } = 0;
		public string Name { get; set; } = string.Empty;

		public ImGuiRegisterAttribute(ushort number, string name)
		{
			Number = number;
			Name = name;
		}
	}
}

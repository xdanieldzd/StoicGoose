using System;

namespace StoicGoose.Interface.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ImGuiFormatAttribute : Attribute
	{
		public string Format { get; set; } = string.Empty;
		public int Shift { get; set; } = 0;

		public ImGuiFormatAttribute(string format, int shift = 0)
		{
			Format = format;
			Shift = shift;
		}
	}
}

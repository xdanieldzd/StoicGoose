using System;

namespace StoicGoose.Common.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class FormatAttribute : Attribute
	{
		public string Format { get; set; } = string.Empty;
		public int Shift { get; set; } = 0;

		public FormatAttribute(string format, int shift = 0)
		{
			Format = format;
			Shift = shift;
		}
	}
}

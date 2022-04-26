using System;

namespace StoicGoose.Common.Attributes
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class BitDescriptionAttribute : Attribute
	{
		public string Description { get; set; } = string.Empty;
		public int LowBit { get; set; } = -1;
		public int HighBit { get; set; } = -1;

		public string BitString => LowBit != -1 ? $"B{LowBit}{(HighBit > LowBit ? $"-{HighBit}" : string.Empty)}: " : string.Empty;

		public BitDescriptionAttribute(string desc, int low = -1, int high = -1)
		{
			Description = desc;
			LowBit = low;
			HighBit = high;
		}
	}
}

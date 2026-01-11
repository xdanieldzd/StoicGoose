using System;

namespace StoicGoose.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BitDescriptionAttribute(string desc, int low = -1, int high = -1) : Attribute
    {
        public string Description { get; set; } = desc;
        public int LowBit { get; set; } = low;
        public int HighBit { get; set; } = high;

        public string BitString => LowBit != -1 ? $"B{LowBit}{(HighBit > LowBit ? $"-{HighBit}" : string.Empty)}: " : string.Empty;
    }
}

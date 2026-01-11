using System;

namespace StoicGoose.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FormatAttribute(string format, int shift = 0) : Attribute
    {
        public string Format { get; set; } = format;
        public int Shift { get; set; } = shift;
    }
}

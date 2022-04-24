using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		enum SegmentNumber : byte
		{
			ES = 0b00,
			CS = 0b01,
			SS = 0b10,
			DS = 0b11,
			Unset = 0xFF
		}

		private ushort GetSegment(SegmentNumber seg)
		{
			return seg switch
			{
				SegmentNumber.ES => es,
				SegmentNumber.CS => cs,
				SegmentNumber.SS => ss,
				SegmentNumber.DS => ds,
				_ => throw new ArgumentException("Invalid segment", nameof(seg)),
			};
		}

		private void SetSegment(SegmentNumber seg, ushort value)
		{
			switch (seg)
			{
				case SegmentNumber.ES: es = value; break;
				case SegmentNumber.CS: cs = value; break;
				case SegmentNumber.SS: ss = value; break;
				case SegmentNumber.DS: ds = value; break;
				default: throw new ArgumentException("Invalid segment", nameof(seg));
			}
		}

		private ushort GetSegmentViaOverride(SegmentNumber seg)
		{
			return GetSegment(prefixSegOverride != SegmentNumber.Unset ? prefixSegOverride : seg);
		}
	}
}

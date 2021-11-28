using System;
using System.Collections.Generic;
using System.Linq;

namespace StoicGoose.Disassembly
{
	public partial class Disassembler
	{
		public enum SegmentNumber { None = -1, ES = 0b00, CS = 0b01, SS = 0b10, DS = 0b11 }

		readonly static byte[] prefixBytes = { 0x26, 0x2E, 0x36, 0x3E, 0xF0, 0xF2, 0xF3 };
		readonly static byte[] compareScanOpcodes = { 0xA6, 0xA7, 0xAE, 0xAF };

		private List<byte> ReadPrefixesAndOpcode()
		{
			var bytes = new List<byte>();

			for (var i = 0; i < 32; i++)
			{
				var read = ReadMemory8(Segment, Offset);
				bytes.Add(read);

				IncrementAddress();

				if (!prefixBytes.Contains(read)) break;
			}
			return bytes;
		}

		private (SegmentNumber, bool, bool, bool, bool) AnalyzePrefixes(IEnumerable<byte> bytes)
		{
			var hasSegmentOverride = SegmentNumber.None;
			var hasLockPrefix = false;
			var hasRepeatPrefix = false;
			var hasRepeatCmpSca = false;
			var hasRepeatCmpScaNotEqual = false;

			foreach (var value in bytes)
			{
				switch (value)
				{
					case 0x26: hasSegmentOverride = SegmentNumber.ES; break;
					case 0x2E: hasSegmentOverride = SegmentNumber.CS; break;
					case 0x36: hasSegmentOverride = SegmentNumber.SS; break;
					case 0x3E: hasSegmentOverride = SegmentNumber.DS; break;
					case 0xF0: hasLockPrefix = true; break;
					case 0xF2:
						hasRepeatPrefix = true;
						hasRepeatCmpSca = true;
						hasRepeatCmpScaNotEqual = true;
						break;
					case 0xF3:
						hasRepeatPrefix = true;
						hasRepeatCmpSca = compareScanOpcodes.Contains(bytes.Last());
						hasRepeatCmpScaNotEqual = false;
						break;
				}
			}

			return (hasSegmentOverride, hasLockPrefix, hasRepeatPrefix, hasRepeatCmpSca, hasRepeatCmpScaNotEqual);
		}

		private string GetOverrideSegmentName(SegmentNumber segOverride)
		{
			return segOverride != SegmentNumber.None ? $"{segmentNames[(int)segOverride]}:" : string.Empty;
		}
	}
}

namespace StoicGoose.Disassembly
{
	public partial class Disassembler
	{
		public enum ModRmModes : byte
		{
			NoDisplacement = 0b00,
			OneByteDisplacement = 0b01,
			TwoByteDisplacement = 0b10,
			Register = 0b11
		}

		ModRm modRm = default;

		private void ReadModRm()
		{
			modRm = ReadMemory8(nextSegment, nextOffset);

			IncrementAddress();
		}

		private string GetModRmPointer(SegmentNumber segOverride)
		{
			var pointer = string.Empty;

			switch (modRm.Mod)
			{
				case ModRmModes.NoDisplacement:
					switch (modRm.Mem)
					{
						case 0b000: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+si]"; break;
						case 0b001: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+di]"; break;
						case 0b010: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+si]"; break;
						case 0b011: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+di]"; break;
						case 0b100: pointer = $"{GetOverrideSegmentName(segOverride)}[si]"; break;
						case 0b101: pointer = $"{GetOverrideSegmentName(segOverride)}[di]"; break;
						case 0b110: pointer = $"{GetOverrideSegmentName(segOverride)}[{ReadMemory16(nextSegment, nextOffset):X4}]"; IncrementAddress(2); break;
						case 0b111: pointer = $"{GetOverrideSegmentName(segOverride)}[bx]"; break;
					}
					break;

				case ModRmModes.OneByteDisplacement:
					{
						var dispValue = ReadMemory8(nextSegment, nextOffset);
						IncrementAddress();

						var displacement = dispValue == 0x80 ? $"-0x{dispValue:X2}" : dispValue >= 0x80 ? $"-0x{(0x80 - dispValue) & 0x7F:X2}" : $"+0x{dispValue & 0x7F:X2}";

						switch (modRm.Mem)
						{
							case 0b000: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+si{displacement}]"; break;
							case 0b001: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+di{displacement}]"; break;
							case 0b010: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+si{displacement}]"; break;
							case 0b011: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+di{displacement}]"; break;
							case 0b100: pointer = $"{GetOverrideSegmentName(segOverride)}[si{displacement}]"; break;
							case 0b101: pointer = $"{GetOverrideSegmentName(segOverride)}[di{displacement}]"; break;
							case 0b110: pointer = $"{GetOverrideSegmentName(segOverride)}[bp{displacement}]"; break;
							case 0b111: pointer = $"{GetOverrideSegmentName(segOverride)}[bx{displacement}]"; break;
						}
					}
					break;

				case ModRmModes.TwoByteDisplacement:
					{
						var dispValue = ReadMemory16(nextSegment, nextOffset);
						IncrementAddress(2);

						var displacement = dispValue == 0x8000 ? $"-0x{dispValue:X4}" : dispValue >= 0x8000 ? $"-0x{(0x8000 - dispValue) & 0x7FFF:X4}" : $"+0x{dispValue & 0x7FFF:X4}";

						switch (modRm.Mem)
						{
							case 0b000: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+si{displacement}]"; break;
							case 0b001: pointer = $"{GetOverrideSegmentName(segOverride)}[bx+di{displacement}]"; break;
							case 0b010: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+si{displacement}]"; break;
							case 0b011: pointer = $"{GetOverrideSegmentName(segOverride)}[bp+di{displacement}]"; break;
							case 0b100: pointer = $"{GetOverrideSegmentName(segOverride)}[si{displacement}]"; break;
							case 0b101: pointer = $"{GetOverrideSegmentName(segOverride)}[di{displacement}]"; break;
							case 0b110: pointer = $"{GetOverrideSegmentName(segOverride)}[bp{displacement}]"; break;
							case 0b111: pointer = $"{GetOverrideSegmentName(segOverride)}[bx{displacement}]"; break;
						}
					}
					break;
			}

			return pointer;
		}

		private struct ModRm
		{
			public static implicit operator ModRm(byte data) => new() { Data = data };

			public byte Data;

			public ModRmModes Mod => (ModRmModes)((Data >> 6) & 0b11);
			public byte Reg => (byte)((Data >> 3) & 0b111);
			public byte Mem => (byte)((Data >> 0) & 0b111);
		}
	}
}

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		ModRM modRm;

		private void ReadModRM()
		{
			if (modRm.IsSet) return;

			modRm.Set(ReadMemory8(cs, ip++));
			switch (modRm.Mod)
			{
				case ModRM.Modes.NoDisplacement:
					switch (modRm.Mem)
					{
						case 0b000: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + si); break;
						case 0b001: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + di); break;
						case 0b010: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + si); break;
						case 0b011: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + di); break;
						case 0b100: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = si; break;
						case 0b101: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = di; break;
						case 0b110: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = ReadMemory16(cs, ip); ip += 2; break;
						case 0b111: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = bx.Word; break;
					}
					break;

				case ModRM.Modes.OneByteDisplacement:
					{
						var displacement = (sbyte)ReadMemory8(cs, ip);
						ip++;
						switch (modRm.Mem)
						{
							case 0b000: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + si + displacement); break;
							case 0b001: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + di + displacement); break;
							case 0b010: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + si + displacement); break;
							case 0b011: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + di + displacement); break;
							case 0b100: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(si + displacement); break;
							case 0b101: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(di + displacement); break;
							case 0b110: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + displacement); break;
							case 0b111: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + displacement); break;
						}
					}
					break;

				case ModRM.Modes.TwoByteDisplacement:
					{
						var displacement = (short)ReadMemory16(cs, ip);
						ip += 2;
						switch (modRm.Mem)
						{
							case 0b000: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + si + displacement); break;
							case 0b001: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + di + displacement); break;
							case 0b010: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + si + displacement); break;
							case 0b011: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + di + displacement); break;
							case 0b100: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(si + displacement); break;
							case 0b101: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(di + displacement); break;
							case 0b110: modRm.Segment = GetSegmentViaOverride(SegmentNumber.SS); modRm.Offset = (ushort)(bp + displacement); break;
							case 0b111: modRm.Segment = GetSegmentViaOverride(SegmentNumber.DS); modRm.Offset = (ushort)(bx.Word + displacement); break;
						}
					}
					break;
			}
		}

		struct ModRM
		{
			public enum Modes : byte
			{
				NoDisplacement = 0b00,
				OneByteDisplacement = 0b01,
				TwoByteDisplacement = 0b10,
				Register = 0b11
			}

			byte data;

			public Modes Mod => (Modes)((data >> 6) & 0b11);
			public byte Reg => (byte)((data >> 3) & 0b111);
			public byte Mem => (byte)((data >> 0) & 0b111);

			public ushort Segment, Offset;

			public bool IsSet;

			public void Set(byte value)
			{
				data = value;

				IsSet = true;
			}

			public void Reset()
			{
				data = 0;
				Segment = 0;
				Offset = 0;

				IsSet = false;
			}
		}
	}
}

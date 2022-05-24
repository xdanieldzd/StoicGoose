namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		private byte ReadOpcodeEb()
		{
			ReadModRM();
			if (modRm.Mod == ModRM.Modes.Register)
				return GetRegister8((RegisterNumber8)modRm.Mem);
			else
				return ReadMemory8(modRm.Segment, modRm.Offset);
		}

		private ushort ReadOpcodeEw()
		{
			ReadModRM();
			if (modRm.Mod == ModRM.Modes.Register)
				return GetRegister16((RegisterNumber16)modRm.Mem);
			else
				return ReadMemory16(modRm.Segment, modRm.Offset);
		}

		private void WriteOpcodeEb(byte value)
		{
			ReadModRM();
			if (modRm.Mod == ModRM.Modes.Register)
				SetRegister8((RegisterNumber8)modRm.Mem, value);
			else
				WriteMemory8(modRm.Segment, modRm.Offset, value);
		}

		private void WriteOpcodeEw(ushort value)
		{
			ReadModRM();
			if (modRm.Mod == ModRM.Modes.Register)
				SetRegister16((RegisterNumber16)modRm.Mem, value);
			else
				WriteMemory16(modRm.Segment, modRm.Offset, value);
		}

		private byte ReadOpcodeGb()
		{
			ReadModRM();
			return GetRegister8((RegisterNumber8)modRm.Reg);
		}

		private ushort ReadOpcodeGw()
		{
			ReadModRM();
			return GetRegister16((RegisterNumber16)modRm.Reg);
		}

		private void WriteOpcodeGb(byte value)
		{
			ReadModRM();
			SetRegister8((RegisterNumber8)modRm.Reg, value);
		}

		private void WriteOpcodeGw(ushort value)
		{
			ReadModRM();
			SetRegister16((RegisterNumber16)modRm.Reg, value);
		}

		private ushort ReadOpcodeSw()
		{
			ReadModRM();
			return GetSegment((SegmentNumber)modRm.Reg);
		}

		private void WriteOpcodeSw(ushort value)
		{
			ReadModRM();
			SetSegment((SegmentNumber)modRm.Reg, value);
		}

		private byte ReadOpcodeIb()
		{
			return ReadMemory8(cs, ip++);
		}

		private ushort ReadOpcodeIw()
		{
			var value = ReadMemory16(cs, ip);
			ip += 2;
			return value;
		}

		private ushort ReadOpcodeJb()
		{
			var tmp1 = (ushort)(ip + 1);
			var tmp2 = (sbyte)ReadOpcodeIb();
			return (ushort)(tmp1 + tmp2);
		}
	}
}

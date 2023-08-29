using System;

namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		public class ModRM
		{
			internal static V30MZ cpu = default;

			public byte Mod, Reg, Mem;
			public ushort Segment, Address;

			public static implicit operator ModRM(byte value) => new(value);

			public ModRM(byte value)
			{
				Mod = (byte)((value >> 6) & 0b11);
				Reg = (byte)((value >> 3) & 0b111);
				Mem = (byte)((value >> 0) & 0b111);

				if (Mod == 0b00 && Mem == 0b110)
				{
					Segment = cpu.SegmentViaPrefix(cpu.ds0);
					Address = cpu.Fetch16();
				}
				else
				{
					switch (Mem)
					{
						case 0b000: Segment = cpu.SegmentViaPrefix(cpu.ds0); Address = cpu.bw + cpu.ix; break;
						case 0b001: Segment = cpu.SegmentViaPrefix(cpu.ds0); Address = cpu.bw + cpu.iy; break;
						case 0b010: Segment = cpu.SegmentViaPrefix(cpu.ss); Address = (ushort)(cpu.bp + cpu.ix); break;
						case 0b011: Segment = cpu.SegmentViaPrefix(cpu.ss); Address = (ushort)(cpu.bp + cpu.iy); break;
						case 0b100: Segment = cpu.SegmentViaPrefix(cpu.ds0); Address = cpu.ix; break;
						case 0b101: Segment = cpu.SegmentViaPrefix(cpu.ds0); Address = cpu.iy; break;
						case 0b110: Segment = cpu.SegmentViaPrefix(cpu.ss); Address = cpu.bp; break;
						case 0b111: Segment = cpu.SegmentViaPrefix(cpu.ds0); Address = cpu.bw; break;
					}

					if (Mod == 0b01) Address = (ushort)(Address + (sbyte)cpu.Fetch8());
					if (Mod == 0b10) Address = (ushort)(Address + (short)cpu.Fetch16());
				}
			}
		}

		public ushort GetSegment() => modRM.Reg switch
		{
			0b000 => ds1,
			0b001 => ps,
			0b010 => ss,
			0b011 => ds0,
			0b100 => ds1,
			0b101 => ps,
			0b110 => ss,
			0b111 => ds0,
			_ => throw new NotImplementedException()
		};

		public byte GetRegister8() => modRM.Reg switch
		{
			0b000 => aw.Low,
			0b001 => cw.Low,
			0b010 => dw.Low,
			0b011 => bw.Low,
			0b100 => aw.High,
			0b101 => cw.High,
			0b110 => dw.High,
			0b111 => bw.High,
			_ => throw new NotImplementedException()
		};

		public ushort GetRegister16() => modRM.Reg switch
		{
			0b000 => aw,
			0b001 => cw,
			0b010 => dw,
			0b011 => bw,
			0b100 => sp,
			0b101 => bp,
			0b110 => ix,
			0b111 => iy,
			_ => throw new NotImplementedException()
		};

		public byte GetMemory8(ushort offset = 0)
		{
			if (modRM.Mod != 0b11)
				return ReadMemory8(modRM.Segment, (ushort)(modRM.Address + offset));
			else
			{
				return modRM.Mem switch
				{
					0b000 => aw.Low,
					0b001 => cw.Low,
					0b010 => dw.Low,
					0b011 => bw.Low,
					0b100 => aw.High,
					0b101 => cw.High,
					0b110 => dw.High,
					0b111 => bw.High,
					_ => throw new NotImplementedException()
				};
			}
		}

		public ushort GetMemory16(ushort offset = 0)
		{
			if (modRM.Mod != 0b11)
				return ReadMemory16(modRM.Segment, (ushort)(modRM.Address + offset));
			else
			{
				return modRM.Mem switch
				{
					0b000 => aw,
					0b001 => cw,
					0b010 => dw,
					0b011 => bw,
					0b100 => sp,
					0b101 => bp,
					0b110 => ix,
					0b111 => iy,
					_ => throw new NotImplementedException()
				};
			}
		}

		public void SetSegment(ushort value)
		{
			switch (modRM.Reg)
			{
				case 0b000: ds1 = value; break;
				case 0b001: ps = value; break;
				case 0b010: ss = value; break;
				case 0b011: ds0 = value; break;
				case 0b100: ds1 = value; break;
				case 0b101: ps = value; break;
				case 0b110: ss = value; break;
				case 0b111: ds0 = value; break;
				default: throw new NotImplementedException();
			}
		}

		public void SetRegister8(byte value)
		{
			switch (modRM.Reg)
			{
				case 0b000: aw.Low = value; break;
				case 0b001: cw.Low = value; break;
				case 0b010: dw.Low = value; break;
				case 0b011: bw.Low = value; break;
				case 0b100: aw.High = value; break;
				case 0b101: cw.High = value; break;
				case 0b110: dw.High = value; break;
				case 0b111: bw.High = value; break;
				default: throw new NotImplementedException();
			}
		}

		public void SetRegister16(ushort value)
		{
			switch (modRM.Reg)
			{
				case 0b000: aw = value; break;
				case 0b001: cw = value; break;
				case 0b010: dw = value; break;
				case 0b011: bw = value; break;
				case 0b100: sp = value; break;
				case 0b101: bp = value; break;
				case 0b110: ix = value; break;
				case 0b111: iy = value; break;
				default: throw new NotImplementedException();
			}
		}

		public void SetMemory8(byte value)
		{
			if (modRM.Mod != 0b11)
				WriteMemory8(modRM.Segment, modRM.Address, value);
			else
			{
				switch (modRM.Mem)
				{
					case 0b000: aw.Low = value; break;
					case 0b001: cw.Low = value; break;
					case 0b010: dw.Low = value; break;
					case 0b011: bw.Low = value; break;
					case 0b100: aw.High = value; break;
					case 0b101: cw.High = value; break;
					case 0b110: dw.High = value; break;
					case 0b111: bw.High = value; break;
					default: throw new NotImplementedException();
				}
			}
		}

		public void SetMemory16(ushort value)
		{
			if (modRM.Mod != 0b11)
				WriteMemory16(modRM.Segment, modRM.Address, value);
			else
			{
				switch (modRM.Mem)
				{
					case 0b000: aw = value; break;
					case 0b001: cw = value; break;
					case 0b010: dw = value; break;
					case 0b011: bw = value; break;
					case 0b100: sp = value; break;
					case 0b101: bp = value; break;
					case 0b110: ix = value; break;
					case 0b111: iy = value; break;
					default: throw new NotImplementedException();
				}
			}
		}
	}
}

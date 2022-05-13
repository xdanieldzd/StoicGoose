using System;
using System.Runtime.InteropServices;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		enum RegisterNumber8 : byte
		{
			AL = 0b000,
			CL = 0b001,
			DL = 0b010,
			BL = 0b011,
			AH = 0b100,
			CH = 0b101,
			DH = 0b110,
			BH = 0b111
		}

		enum RegisterNumber16 : byte
		{
			AX = 0b000,
			CX = 0b001,
			DX = 0b010,
			BX = 0b011,
			SP = 0b100,
			BP = 0b101,
			SI = 0b110,
			DI = 0b111
		}

		private byte GetRegister8(RegisterNumber8 reg)
		{
			return reg switch
			{
				RegisterNumber8.AL => ax.Low,
				RegisterNumber8.CL => cx.Low,
				RegisterNumber8.DL => dx.Low,
				RegisterNumber8.BL => bx.Low,
				RegisterNumber8.AH => ax.High,
				RegisterNumber8.CH => cx.High,
				RegisterNumber8.DH => dx.High,
				RegisterNumber8.BH => bx.High,
				_ => throw new ArgumentException("Invalid register", nameof(reg)),
			};
		}

		private ushort GetRegister16(RegisterNumber16 reg)
		{
			return reg switch
			{
				RegisterNumber16.AX => ax.Word,
				RegisterNumber16.CX => cx.Word,
				RegisterNumber16.DX => dx.Word,
				RegisterNumber16.BX => bx.Word,
				RegisterNumber16.SP => sp,
				RegisterNumber16.BP => bp,
				RegisterNumber16.SI => si,
				RegisterNumber16.DI => di,
				_ => throw new ArgumentException("Invalid register", nameof(reg)),
			};
		}

		private void SetRegister8(RegisterNumber8 reg, byte value)
		{
			switch (reg)
			{
				case RegisterNumber8.AL: ax.Low = value; break;
				case RegisterNumber8.CL: cx.Low = value; break;
				case RegisterNumber8.DL: dx.Low = value; break;
				case RegisterNumber8.BL: bx.Low = value; break;
				case RegisterNumber8.AH: ax.High = value; break;
				case RegisterNumber8.CH: cx.High = value; break;
				case RegisterNumber8.DH: dx.High = value; break;
				case RegisterNumber8.BH: bx.High = value; break;
				default: throw new ArgumentException("Invalid register", nameof(reg));
			}
		}

		private void SetRegister16(RegisterNumber16 reg, ushort value)
		{
			switch (reg)
			{
				case RegisterNumber16.AX: ax.Word = value; break;
				case RegisterNumber16.CX: cx.Word = value; break;
				case RegisterNumber16.DX: dx.Word = value; break;
				case RegisterNumber16.BX: bx.Word = value; break;
				case RegisterNumber16.SP: sp = value; break;
				case RegisterNumber16.BP: bp = value; break;
				case RegisterNumber16.SI: si = value; break;
				case RegisterNumber16.DI: di = value; break;
				default: throw new ArgumentException("Invalid register", nameof(reg));
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct Register16
		{
			[FieldOffset(0)]
			public byte Low;
			[FieldOffset(1)]
			public byte High;

			[FieldOffset(0)]
			public ushort Word;

			public static implicit operator Register16(ushort value) => new() { Word = value };

		}
	}
}

using System;

namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		internal Byte ADD(Byte a, Byte b)
		{
			var result = a + b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x80) != 0;
			psw.Carry = result > 0xFF;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) + (b & 0x0F) >= 0x10;

			return (Byte)result;
		}
		internal Byte ADDC(Byte a, Byte b)
		{
			return ADD(a, (Byte)(b + (psw.Carry ? 1 : 0)));
		}
		internal Byte DEC(Byte a)
		{
			var result = a - 1;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = result == 0x7F;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x00;

			return (Byte)result;
		}
		internal UInt16 DIV(UInt16 a, Byte b)
		{
			if (b == 0) { Interrupt(0); return a; }

			var quotient = (Int16)a / (SByte)b;
			var remainder = (Int16)a % (SByte)b;

			if (quotient > 127 || quotient < -128) { Interrupt(0); return a; }

			return (UInt16)(((remainder & 0xFF) << 8) | (quotient & 0xFF));
		}
		internal UInt16 DIVU(UInt16 a, Byte b)
		{
			if (b == 0) { Interrupt(0); return a; }

			var quotient = (UInt32)(a / b);
			var remainder = (UInt32)(a % b);

			if (quotient > 255) { Interrupt(0); return a; }

			return (UInt16)(((remainder & 0xFF) << 8) | (quotient & 0xFF));
		}
		internal Byte INC(Byte a)
		{
			var result = a + 1;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = result == 0x80;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x0F;

			return (Byte)result;
		}
		internal UInt16 MUL(Byte a, Byte b)
		{
			var result = (SByte)a * (SByte)b;

			psw.Carry = (result >> 8) != 0;
			psw.Overflow = (result >> 8) != 0;

			return (UInt16)result;
		}
		internal UInt16 MULU(Byte a, Byte b)
		{
			var result = a * b;

			psw.Carry = (result >> 8) != 0;
			psw.Overflow = (result >> 8) != 0;

			return (UInt16)result;
		}
		internal Byte NEG(Byte a)
		{
			var result = -a & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = a == 0x80;
			psw.Carry = a != 0;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) != 0x00;

			return (Byte)result;
		}
		internal Byte SUB(Byte a, Byte b)
		{
			var result = a - b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = ((a ^ result) & (a ^ b) & 0x80) != 0;
			psw.Carry = b > a;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (b & 0x0F) > (a & 0x0F);

			return (Byte)result;
		}
		internal Byte SUBC(Byte a, Byte b)
		{
			return SUB(a, (Byte)(b + (psw.Carry ? 1 : 0)));
		}
		internal UInt16 ADD(UInt16 a, UInt16 b)
		{
			var result = a + b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x8000) != 0;
			psw.Carry = result > 0xFFFF;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) + (b & 0x0F) >= 0x10;

			return (UInt16)result;
		}
		internal UInt16 ADDC(UInt16 a, UInt16 b)
		{
			return ADD(a, (UInt16)(b + (psw.Carry ? 1 : 0)));
		}
		internal UInt16 DEC(UInt16 a)
		{
			var result = a - 1;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = result == 0x7FFF;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x00;

			return (UInt16)result;
		}
		internal UInt32 DIV(UInt32 a, UInt16 b)
		{
			if (b == 0) { Interrupt(0); return a; }

			var quotient = (Int32)a / (Int16)b;
			var remainder = (Int32)a % (Int16)b;

			if (quotient > 32767 || quotient < -32768) { Interrupt(0); return a; }

			return (UInt32)(((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF));
		}
		internal UInt32 DIVU(UInt32 a, UInt16 b)
		{
			if (b == 0) { Interrupt(0); return a; }

			var quotient = (UInt64)(a / b);
			var remainder = (UInt64)(a % b);

			if (quotient > 65535) { Interrupt(0); return a; }

			return (UInt32)(((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF));
		}
		internal UInt16 INC(UInt16 a)
		{
			var result = a + 1;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = result == 0x8000;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x0F;

			return (UInt16)result;
		}
		internal UInt32 MUL(UInt16 a, UInt16 b)
		{
			var result = (Int16)a * (Int16)b;

			psw.Carry = (result >> 16) != 0;
			psw.Overflow = (result >> 16) != 0;

			return (UInt32)result;
		}
		internal UInt32 MULU(UInt16 a, UInt16 b)
		{
			var result = a * b;

			psw.Carry = (result >> 16) != 0;
			psw.Overflow = (result >> 16) != 0;

			return (UInt32)result;
		}
		internal UInt16 NEG(UInt16 a)
		{
			var result = -a & 0xFFFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = a == 0x8000;
			psw.Carry = a != 0;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) != 0x00;

			return (UInt16)result;
		}
		internal UInt16 SUB(UInt16 a, UInt16 b)
		{
			var result = a - b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = ((a ^ result) & (a ^ b) & 0x8000) != 0;
			psw.Carry = b > a;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (b & 0x0F) > (a & 0x0F);

			return (UInt16)result;
		}
		internal UInt16 SUBC(UInt16 a, UInt16 b)
		{
			return SUB(a, (UInt16)(b + (psw.Carry ? 1 : 0)));
		}
		internal void ADJ4x(Boolean subtract)
		{
			var result = aw.Low;

			if ((aw.Low & 0x0F) > 0x09 || psw.AuxiliaryCarry)
			{
				result += (Byte)(subtract ? -0x06 : 0x06);
				psw.AuxiliaryCarry = true;
			}

			if (aw.Low > 0x9F || psw.Carry)
			{
				result += (Byte)(subtract ? -0x60 : 0x60);
				psw.Carry = true;
			}

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			aw.Low = (Byte)result;
		}
		internal void ADJBx(Boolean subtract)
		{
			if ((aw.Low & 0x0F) > 0x99 || psw.AuxiliaryCarry)
			{
				aw.Low += (Byte)(subtract ? -0x06 : 0x06);
				aw.High += (Byte)(subtract ? -0x01 : 0x01);

				psw.AuxiliaryCarry = true;
				psw.Carry = true;
			}
			else
			{
				psw.AuxiliaryCarry = false;
				psw.Carry = false;
			}

			aw.Low &= 0x0F;
		}
		internal void CVTBD(Byte a)
		{
			if (a == 0)
				Interrupt(0);
			else
			{
				aw.High = (Byte)(aw.Low / a);
				aw.Low %= a;

				psw.Zero = (aw.Word & 0xFFFF) == 0;
				psw.Sign = (aw.Word & 0x8000) == 0x8000;
				psw.Parity = Parity(aw.Low & 0xFF);
			}
		}
		internal void CVTDB(Byte a)
		{
			aw.Low += (Byte)(aw.High * a);
			aw.High = 0;

			psw.Zero = (aw.Word & 0xFFFF) == 0;
			psw.Sign = (aw.Word & 0x8000) == 0x8000;
			psw.Parity = Parity(aw.Low & 0xFF);
		}
	}
}

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		internal byte ADD(byte a, byte b)
		{
			var result = a + b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x80) != 0;
			psw.Carry = result >= 0x100;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) + (b & 0x0F) >= 0x10;

			return (byte)result;
		}

		internal ushort ADD(ushort a, ushort b)
		{
			var result = a + b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x8000) != 0;
			psw.Carry = result >= 0x10000;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) + (b & 0x0F) >= 0x10;

			return (ushort)result;
		}

		internal byte ADDC(byte a, byte b)
		{
			return ADD(a, (byte)(b + (psw.Carry ? 1 : 0)));
		}

		internal ushort ADDC(ushort a, ushort b)
		{
			return ADD(a, (ushort)(b + (psw.Carry ? 1 : 0)));
		}

		internal void ADJ4x(bool subtract)
		{
			var result = aw.Low;

			if ((aw.Low & 0x0F) > 0x09 || psw.AuxiliaryCarry)
			{
				result += (byte)(subtract ? -0x06 : 0x06);
				psw.AuxiliaryCarry = true;
			}

			if (aw.Low > 0x9F || psw.Carry)
			{
				result += (byte)(subtract ? -0x60 : 0x60);
				psw.Carry = true;
			}

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			aw.Low = (byte)result;
		}

		internal void ADJBx(bool subtract)
		{
			if ((aw.Low & 0x0F) > 0x99 || psw.AuxiliaryCarry)
			{
				aw.Low += (byte)(subtract ? -0x06 : 0x06);
				aw.High += (byte)(subtract ? -0x01 : 0x01);

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

		internal void CVTBD(byte a)
		{
			if (a == 0)
				Interrupt(0);
			else
			{
				aw.High = (byte)(aw.Low / a);
				aw.Low %= a;

				psw.Zero = (aw.Word & 0xFFFF) == 0;
				psw.Sign = (aw.Word & 0x8000) == 0x8000;
				psw.Parity = Parity(aw.Low & 0xFF);
			}
		}

		internal void CVTDB(byte a)
		{
			aw.Low += (byte)(aw.High * a);
			aw.High = 0;

			psw.Zero = (aw.Word & 0xFFFF) == 0;
			psw.Sign = (aw.Word & 0x8000) == 0x8000;
			psw.Parity = Parity(aw.Low & 0xFF);
		}

		internal byte DEC(byte a)
		{
			var result = a - 1;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = result == 0x7F;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x00;

			return (byte)result;
		}

		internal ushort DEC(ushort a)
		{
			var result = a - 1;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = result == 0x7FFF;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x00;

			return (ushort)result;
		}

		internal ushort DIV(short a, sbyte b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return (ushort)a;
			}

			var quotient = a / b;
			var remainder = a % b;

			if (quotient > 0x7F || quotient < -0x80)
			{
				Interrupt(0);
				return (ushort)a;
			}

			return (ushort)(((remainder & 0xFF) << 8) | (quotient & 0xFF));
		}

		internal uint DIV(int a, short b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return (uint)a;
			}

			var quotient = a / b;
			var remainder = a % b;

			if (quotient > 0x7FFF || quotient < -0x8000)
			{
				Interrupt(0);
				return (uint)a;
			}

			return (uint)(((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF));
		}

		internal ushort DIVU(ushort a, byte b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return a;
			}

			var quotient = a / b;
			var remainder = a % b;

			if (quotient > 0x80)
			{
				Interrupt(0);
				return a;
			}

			return (ushort)(((remainder & 0xFF) << 8) | (quotient & 0xFF));
		}

		internal uint DIVU(uint a, ushort b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return a;
			}

			var quotient = a / b;
			var remainder = a % b;

			if (quotient > 0x8000)
			{
				Interrupt(0);
				return a;
			}

			return ((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF);
		}

		internal byte INC(byte a)
		{
			var result = a + 1;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = result == 0x80;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x0F;

			return (byte)result;
		}

		internal ushort INC(ushort a)
		{
			var result = a + 1;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = result == 0x8000;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) == 0x0F;

			return (ushort)result;
		}

		internal ushort MUL(sbyte a, sbyte b)
		{
			var result = a * b;

			psw.Carry = (result >> 8) != 0;
			psw.Overflow = (result >> 8) != 0;

			return (ushort)result;
		}

		internal uint MUL(short a, short b)
		{
			var result = a * b;

			psw.Carry = (result >> 16) != 0;
			psw.Overflow = (result >> 16) != 0;

			return (uint)result;
		}

		internal ushort MULU(byte a, byte b)
		{
			var result = a * b;

			psw.Carry = (result >> 8) != 0;
			psw.Overflow = (result >> 8) != 0;

			return (ushort)result;
		}

		internal uint MULU(ushort a, ushort b)
		{
			var result = a * b;

			psw.Carry = (result >> 16) != 0;
			psw.Overflow = (result >> 16) != 0;

			return (uint)result;
		}

		internal byte NEG(byte a)
		{
			var result = -a & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = a == 0x80;
			psw.Carry = a != 0x00;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) != 0x00;

			return (byte)result;
		}

		internal ushort NEG(ushort a)
		{
			var result = -a & 0xFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = a == 0x8000;
			psw.Carry = a != 0x00;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (a & 0x0F) != 0x00;

			return (ushort)result;
		}

		internal byte SUB(byte a, byte b)
		{
			var result = a - b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x80) != 0;
			psw.Carry = b > a;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (b & 0x0F) > (a & 0x0F);

			return (byte)result;
		}

		internal ushort SUB(ushort a, ushort b)
		{
			var result = a - b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = ((result ^ a) & (result ^ b) & 0x8000) != 0;
			psw.Carry = b > a;
			psw.Parity = Parity(result & 0xFF);
			psw.AuxiliaryCarry = (b & 0x0F) > (a & 0x0F);

			return (ushort)result;
		}

		internal byte SUBC(byte a, byte b)
		{
			return SUB(a, (byte)(b + (psw.Carry ? 1 : 0)));
		}

		internal ushort SUBC(ushort a, ushort b)
		{
			return SUB(a, (ushort)(b + (psw.Carry ? 1 : 0)));
		}
	}
}

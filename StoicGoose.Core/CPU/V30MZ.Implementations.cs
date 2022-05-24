namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		private byte Add8(bool withCarry, byte a, byte b)
		{
			int result = a + b + (withCarry && IsFlagSet(Flags.Carry) ? 1 : 0);

			// CF, PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, (result & 0x100) != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ b) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Add16(bool withCarry, ushort a, ushort b)
		{
			int result = a + b + (withCarry && IsFlagSet(Flags.Carry) ? 1 : 0);

			// CF, PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, (result & 0x10000) != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ b) & 0x8000) != 0);

			return (ushort)result;
		}

		private byte Or8(byte a, byte b)
		{
			int result = a | b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			ClearFlags(Flags.Overflow);

			return (byte)result;
		}

		private ushort Or16(ushort a, ushort b)
		{
			int result = a | b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			ClearFlags(Flags.Overflow);

			return (ushort)result;
		}

		private byte Sub8(bool withBorrow, byte a, byte b)
		{
			int result = a - (b + (withBorrow && IsFlagSet(Flags.Carry) ? 1 : 0));

			// CF, PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, (result & 0x100) != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ b) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Sub16(bool withBorrow, ushort a, ushort b)
		{
			int result = a - (b + (withBorrow && IsFlagSet(Flags.Carry) ? 1 : 0));

			// CF, PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, (result & 0x10000) != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ b) & 0x8000) != 0);

			return (ushort)result;
		}

		private byte And8(byte a, byte b)
		{
			int result = a & b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			ClearFlags(Flags.Overflow);

			return (byte)result;
		}

		private ushort And16(ushort a, ushort b)
		{
			int result = a & b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			ClearFlags(Flags.Overflow);

			return (ushort)result;
		}

		private void Daa(bool isSubtract)
		{
			byte oldAl = ax.Low;

			if (((oldAl & 0x0F) > 0x09) || IsFlagSet(Flags.Auxiliary))
			{
				ax.Low += (byte)(isSubtract ? -0x06 : 0x06);
				SetFlags(Flags.Auxiliary);
			}
			else
				ClearFlags(Flags.Auxiliary);

			if ((oldAl > 0x99) || IsFlagSet(Flags.Carry))
			{
				ax.Low += (byte)(isSubtract ? -0x60 : 0x60);
				SetFlags(Flags.Carry);
			}
			else
				ClearFlags(Flags.Carry);

			SetClearFlagConditional(Flags.Parity, CalculateParity(ax.Low & 0xFF));
			SetClearFlagConditional(Flags.Zero, (ax.Low & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (ax.Low & 0x80) != 0);
		}

		private byte Xor8(byte a, byte b)
		{
			int result = a ^ b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			ClearFlags(Flags.Overflow);

			return (byte)result;
		}

		private ushort Xor16(ushort a, ushort b)
		{
			int result = a ^ b;

			// CF, OF = cleared; PF, ZF, SF = according to result; AF = undefined
			ClearFlags(Flags.Carry);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			ClearFlags(Flags.Overflow);

			return (ushort)result;
		}

		private void Aaa(bool isSubtract)
		{
			if (((ax.Low & 0x0F) > 0x09) || IsFlagSet(Flags.Auxiliary))
			{
				ax.Low = (byte)(ax.Low + (isSubtract ? -0x06 : 0x06));
				ax.High = (byte)(ax.High + (isSubtract ? -0x01 : 0x01));

				SetFlags(Flags.Auxiliary);
				SetFlags(Flags.Carry);
			}
			else
			{
				ClearFlags(Flags.Auxiliary);
				ClearFlags(Flags.Carry);
			}

			ax.Low &= 0x0F;
		}

		private byte Inc8(byte a)
		{
			int result = a + 1;

			// PF, AF, ZF, SF, OF = according to result, CF = undefined
			//Carry
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ 1) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Inc16(ushort a)
		{
			int result = a + 1;

			// PF, AF, ZF, SF, OF = according to result, CF = undefined
			//Carry
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (result ^ 1) & 0x8000) != 0);

			return (ushort)result;
		}

		private byte Dec8(byte a)
		{
			int result = a - 1;

			// PF, AF, ZF, SF, OF = according to result, CF = undefined
			//Carry
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ 1) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Dec16(ushort a)
		{
			int result = a - 1;

			// PF, AF, ZF, SF, OF = according to result, CF = undefined
			//Carry
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((a ^ 1 ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ a) & (a ^ 1) & 0x8000) != 0);

			return (ushort)result;
		}

		private byte Rol8(bool withCarry, byte a, byte b)
		{
			int result;

			if (withCarry)
			{
				result = a;
				for (var n = 0; n < b; n++)
				{
					var carry = result & 0x80;
					result = (result << 1) | (IsFlagSet(Flags.Carry) ? 0x01 : 0);
					SetClearFlagConditional(Flags.Carry, carry != 0);
				}
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x80) != 0);
				result &= 0xFF;
			}
			else
			{
				result = (a << b) | (a >> (8 - b));
				SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 8)) != 0);
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x80) != 0);
				result &= 0xFF;
			}

			return (byte)result;
		}

		private ushort Rol16(bool withCarry, ushort a, ushort b)
		{
			int result;

			if (withCarry)
			{
				result = a;
				for (var n = 0; n < b; n++)
				{
					var carry = result & 0x80;
					result = (result << 1) | (IsFlagSet(Flags.Carry) ? 0x0001 : 0);
					SetClearFlagConditional(Flags.Carry, carry != 0);
				}
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x8000) != 0);
				result &= 0xFFFF;
			}
			else
			{
				result = (a << b) | (a >> (16 - b));
				SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 16)) != 0);
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x8000) != 0);
				result &= 0xFFFF;
			}

			return (ushort)result;
		}

		private byte Ror8(bool withCarry, byte a, byte b)
		{
			int result;

			if (withCarry)
			{
				result = a;
				for (var n = 0; n < b; n++)
				{
					var carry = result & 0x01;
					result = (IsFlagSet(Flags.Carry) ? 0x80 : 0) | (result >> 1);
					SetClearFlagConditional(Flags.Carry, carry != 0);
				}
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x80) != 0);
				result &= 0xFF;
			}
			else
			{
				result = (a >> b) | (a << (8 - b));
				SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & 0x01) != 0);
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x80) != 0);
				result &= 0xFF;
			}

			return (byte)result;
		}

		private ushort Ror16(bool withCarry, ushort a, ushort b)
		{
			int result;

			if (withCarry)
			{
				result = a;
				for (var n = 0; n < b; n++)
				{
					var carry = result & 0x01;
					result = (IsFlagSet(Flags.Carry) ? 0x8000 : 0) | (result >> 1);
					SetClearFlagConditional(Flags.Carry, carry != 0);
				}
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x8000) != 0);
				result &= 0xFFFF;
			}
			else
			{
				result = (a >> b) | (a << (16 - b));
				SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & 0x01) != 0);
				SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x8000) != 0);
				result &= 0xFFFF;
			}

			return (ushort)result;
		}

		private byte Shl8(byte a, byte b)
		{
			int result = (a << b) & 0xFF;

			if (b != 0)
			{
				SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 8)) != 0);
				SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
				//Aux
				SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
				SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
				if (b == 1) SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x80) != 0);
			}

			return (byte)result;
		}

		private ushort Shl16(ushort a, ushort b)
		{
			int result = (a << b) & 0xFFFF;

			if (b != 0)
			{
				SetClearFlagConditional(Flags.Carry, ((a << b) & (1 << 16)) != 0);
				SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
				//Aux
				SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
				SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
				if (b == 1) SetClearFlagConditional(Flags.Overflow, ((a ^ result) & 0x8000) != 0);
			}

			return (ushort)result;
		}

		private byte Shr8(bool signed, byte a, byte b)
		{
			if (signed && (b & 16) != 0)
			{
				SetClearFlagConditional(Flags.Carry, (a & 0x80) != 0);
				return (byte)(0 - (IsFlagSet(Flags.Carry) ? 1 : 0));
			}

			int result = (a >> b) & 0xFF;

			SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & (1 << 0)) != 0);
			if (signed && (a & 0x80) != 0) result |= 0xFF << (8 - b);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, !signed && ((a ^ result) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Shr16(bool signed, ushort a, ushort b)
		{
			if (signed && (b & 16) != 0)
			{
				SetClearFlagConditional(Flags.Carry, (a & 0x8000) != 0);
				return (ushort)(0 - (IsFlagSet(Flags.Carry) ? 1 : 0));
			}

			int result = (a >> b) & 0xFFFF;

			SetClearFlagConditional(Flags.Carry, ((a >> (b - 1)) & (1 << 0)) != 0);
			if (signed && (a & 0x8000) != 0) result |= 0xFFFF << (16 - b);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			//Aux
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, !signed && ((a ^ result) & 0x8000) != 0);

			return (ushort)result;
		}

		private byte Neg8(byte b)
		{
			int result = -b & 0xFF;

			// CF = is operand non-zero?; PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, b != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((0 ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x80) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ 0) & (0 ^ b) & 0x80) != 0);

			return (byte)result;
		}

		private ushort Neg16(ushort b)
		{
			int result = -b & 0xFFFF;

			// CF = is operand non-zero?; PF, AF, ZF, SF, OF = according to result
			SetClearFlagConditional(Flags.Carry, b != 0);
			SetClearFlagConditional(Flags.Parity, CalculateParity(result & 0xFF));
			SetClearFlagConditional(Flags.Auxiliary, ((0 ^ b ^ result) & 0x10) != 0);
			SetClearFlagConditional(Flags.Zero, (result & 0xFFFF) == 0);
			SetClearFlagConditional(Flags.Sign, (result & 0x8000) != 0);
			SetClearFlagConditional(Flags.Overflow, ((result ^ 0) & (0 ^ b) & 0x8000) != 0);

			return (ushort)result;
		}

		private ushort Mul8(bool signed, byte a, byte b)
		{
			uint result = (uint)(signed ? ((sbyte)a * (sbyte)b) : (a * b));

			// CF, OF = is upper half of result non-zero?; PF, AF, ZF, SF = undefined
			SetClearFlagConditional(Flags.Overflow, (result >> 8) != 0);
			SetClearFlagConditional(Flags.Carry, (result >> 8) != 0);

			return (ushort)result;
		}

		private uint Mul16(bool signed, ushort a, ushort b)
		{
			uint result = (uint)(signed ? ((short)a * (short)b) : (a * b));

			// CF, OF = is upper half of result non-zero?; PF, AF, ZF, SF = undefined
			SetClearFlagConditional(Flags.Overflow, (result >> 16) != 0);
			SetClearFlagConditional(Flags.Carry, (result >> 16) != 0);

			return (uint)result;
		}

		private ushort Div8(bool signed, ushort a, byte b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return a;
			}

			int quotient = signed ? ((short)a / (sbyte)b) : (a / b);
			int remainder = signed ? ((short)a % (sbyte)b) : (a % b);

			// CF, PF, AF, ZF, SF, OF = undefined

			return (ushort)(((remainder & 0xFF) << 8) | (quotient & 0xFF));
		}

		private uint Div16(bool signed, uint a, ushort b)
		{
			if (b == 0)
			{
				Interrupt(0);
				return a;
			}

			int quotient = signed ? ((int)a / (short)b) : (int)(a / b);
			int remainder = signed ? ((int)a % (short)b) : (int)(a % b);

			// CF, PF, AF, ZF, SF, OF = undefined

			return (uint)(((remainder & 0xFFFF) << 16) | (quotient & 0xFFFF));
		}
	}
}

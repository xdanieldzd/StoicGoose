using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			psw.AuxiliaryCarry = ((a ^ b ^ result) & 0x10) != 0;

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
			psw.AuxiliaryCarry = ((a ^ b ^ result) & 0x1000) != 0;

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
	}
}

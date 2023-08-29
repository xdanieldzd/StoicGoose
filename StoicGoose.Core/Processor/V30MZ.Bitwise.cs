using System;

namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		internal Byte AND(Byte a, Byte b)
		{
			var result = a & b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal Byte OR(Byte a, Byte b)
		{
			var result = a | b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal Byte ROL(Byte a, Byte b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x80) == 0x80;
				result = (result << 1) | (psw.Carry ? 1 : 0);
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (Byte)result;
		}
		internal Byte ROLC(Byte a, Byte b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x80) == 0x80;
				result = (result << 1) | (psw.Carry ? 1 : 0);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (Byte)result;
		}
		internal Byte ROR(Byte a, Byte b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x80 : 0);
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (Byte)result;
		}
		internal Byte RORC(Byte a, Byte b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x80 : 0);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (Byte)result;
		}
		internal Byte SHL(Byte a, Byte b)
		{
			var result = (a << b) & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			if (b != 0) psw.Carry = ((a << b) & (1 << 8)) != 0;
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal Byte SHR(Byte a, Byte b)
		{
			var result = (a >> b) & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			if (b != 0) psw.Carry = ((a >> (b - 1)) & 0x01) != 0;
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal Byte SHRA(Byte a, Byte b)
		{
			var result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (Byte)((result >> 1) | (result & 0x80));
			}
			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal Byte XOR(Byte a, Byte b)
		{
			var result = a ^ b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (Byte)result;
		}
		internal UInt16 AND(UInt16 a, UInt16 b)
		{
			var result = a & b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
		internal UInt16 OR(UInt16 a, UInt16 b)
		{
			var result = a | b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
		internal UInt16 ROL(UInt16 a, UInt16 b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x8000) == 0x8000;
				result = (result << 1) | (psw.Carry ? 1 : 0);
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (UInt16)result;
		}
		internal UInt16 ROLC(UInt16 a, UInt16 b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x8000) == 0x8000;
				result = (result << 1) | (psw.Carry ? 1 : 0);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (UInt16)result;
		}
		internal UInt16 ROR(UInt16 a, UInt16 b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x8000 : 0);
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (UInt16)result;
		}
		internal UInt16 RORC(UInt16 a, UInt16 b)
		{
			Int32 result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x8000 : 0);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (UInt16)result;
		}
		internal UInt16 SHL(UInt16 a, UInt16 b)
		{
			var result = (a << b) & 0xFFFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			if (b != 0) psw.Carry = ((a << b) & (1 << 16)) != 0;
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
		internal UInt16 SHR(UInt16 a, UInt16 b)
		{
			var result = (a >> b) & 0xFFFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			if (b != 0) psw.Carry = ((a >> (b - 1)) & 0x01) != 0;
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
		internal UInt16 SHRA(UInt16 a, UInt16 b)
		{
			var result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (UInt16)((result >> 1) | (result & 0x8000));
			}
			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
		internal UInt16 XOR(UInt16 a, UInt16 b)
		{
			var result = a ^ b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (UInt16)result;
		}
	}
}

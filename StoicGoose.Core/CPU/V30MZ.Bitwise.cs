namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		internal byte AND(byte a, byte b)
		{
			var result = a & b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort AND(ushort a, ushort b)
		{
			var result = a & b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}

		internal byte OR(byte a, byte b)
		{
			var result = a | b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort OR(ushort a, ushort b)
		{
			var result = a | b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}

		internal byte ROL(byte a, byte b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x80) == 0x80;
				result = (result << 1) | (psw.Carry ? 0x01 : 0x00);
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (byte)result;
		}

		internal ushort ROL(ushort a, ushort b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x8000) == 0x8000;
				result = (result << 1) | (psw.Carry ? 0x0001 : 0x0000);
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (ushort)result;
		}

		internal byte ROLC(byte a, byte b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x80) == 0x80;
				result = (result << 1) | (psw.Carry ? 0x01 : 0x00);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (byte)result;
		}

		internal ushort ROLC(ushort a, ushort b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x8000) == 0x8000;
				result = (result << 1) | (psw.Carry ? 0x0001 : 0x0000);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (ushort)result;
		}

		internal byte ROR(byte a, byte b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x80 : 0x00);
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (byte)result;
		}

		internal ushort ROR(ushort a, ushort b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x0001) == 0x0001;
				result = (result >> 1) | (psw.Carry ? 0x8000 : 0x0000);
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (ushort)result;
		}

		internal byte RORC(byte a, byte b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x01) == 0x01;
				result = (result >> 1) | (psw.Carry ? 0x80 : 0x00);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			return (byte)result;
		}

		internal ushort RORC(ushort a, ushort b)
		{
			int result = a;
			for (var n = 0; n < b; n++)
			{
				var carry = (result & 0x0001) == 0x0001;
				result = (result >> 1) | (psw.Carry ? 0x8000 : 0x0000);
				psw.Carry = carry;
			}
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			return (ushort)result;
		}

		internal byte SHL(byte a, byte b)
		{
			var result = (a << b) & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			if (b != 0) psw.Carry = ((a << b) & (1 << 8)) != 0;
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort SHL(ushort a, ushort b)
		{
			var result = (a << b) & 0xFFFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			if (b != 0) psw.Carry = ((a << b) & (1 << 16)) != 0;
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}

		internal byte SHR(byte a, byte b)
		{
			var result = (a >> b) & 0xFF;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			if (b != 0) psw.Carry = ((a >> (b - 1)) & 0x01) != 0;
			psw.Overflow = ((a ^ result) & 0x80) == 0x80;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort SHR(ushort a, ushort b)
		{
			var result = (a >> b) & 0xFFFF;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			if (b != 0) psw.Carry = ((a >> (b - 1)) & 0x01) != 0;
			psw.Overflow = ((a ^ result) & 0x8000) == 0x8000;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}

		internal byte SHRA(byte a, byte b)
		{
			var result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x01) == 0x01;
				result = (byte)((result >> 1) | (result & 0x80));
			}
			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort SHRA(ushort a, ushort b)
		{
			var result = a;
			for (var n = 0; n < b; n++)
			{
				psw.Carry = (result & 0x0001) == 0x0001;
				result = (ushort)((result >> 1) | (result & 0x8000));
			}
			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}

		internal byte XOR(byte a, byte b)
		{
			var result = a ^ b;

			psw.Zero = (result & 0xFF) == 0;
			psw.Sign = (result & 0x80) == 0x80;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (byte)result;
		}

		internal ushort XOR(ushort a, ushort b)
		{
			var result = a ^ b;

			psw.Zero = (result & 0xFFFF) == 0;
			psw.Sign = (result & 0x8000) == 0x8000;
			psw.Overflow = false;
			psw.Carry = false;
			psw.Parity = Parity(result & 0xFF);

			return (ushort)result;
		}
	}
}

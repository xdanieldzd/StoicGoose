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

		//NOT

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

		//ROL
		//ROLC
		//ROR
		//RORC
		//SHL
		//SHR
		//SHRA

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

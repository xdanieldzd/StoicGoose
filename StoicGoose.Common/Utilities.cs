namespace StoicGoose.Common
{
	public static class Utilities
	{
		public static void ChangeBit(ref byte value, int bit, bool state)
		{
			if (state)
				value |= (byte)(1 << bit);
			else
				value &= (byte)~(1 << bit);
		}

		public static bool IsBitSet(byte value, int bit) => (value & (1 << bit)) != 0;

		public static int GetBase(string value) => value.StartsWith("0x") ? 16 : 10;

		public static void Swap<T>(ref T a, ref T b)
		{
			var tmp = a;
			a = b;
			b = tmp;
		}

		public static int DecimalToBcd(int value)
		{
			return ((value / 10) << 4) + (value % 10);
		}

		public static int BcdToDecimal(int value)
		{
			return ((value >> 4) * 10) + value % 16;
		}
	}
}

namespace StoicGoose
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
	}
}

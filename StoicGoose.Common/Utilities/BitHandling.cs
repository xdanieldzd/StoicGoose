namespace StoicGoose.Common.Utilities
{
	public static class BitHandling
	{
		public static void ChangeBit(ref byte value, int bit, bool state)
		{
			if (state)
				value |= (byte)(1 << bit);
			else
				value &= (byte)~(1 << bit);
		}

		public static bool IsBitSet(byte value, int bit) => (value & (1 << bit)) != 0;
	}
}

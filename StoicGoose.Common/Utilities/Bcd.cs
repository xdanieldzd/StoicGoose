namespace StoicGoose.Common.Utilities
{
	public static class Bcd
	{
		public static int DecimalToBcd(int value) => ((value / 10) << 4) + (value % 10);
		public static int BcdToDecimal(int value) => ((value >> 4) * 10) + value % 16;
	}
}

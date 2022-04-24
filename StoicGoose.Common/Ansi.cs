namespace StoicGoose.Common
{
	public static class Ansi
	{
		public readonly static string Reset = "\x1B[0m";
		public readonly static string Black = "\x1B[30m";
		public readonly static string Red = "\x1B[31m";
		public readonly static string Green = "\x1B[32m";
		public readonly static string Yellow = "\x1B[33m";
		public readonly static string Blue = "\x1B[34m";
		public readonly static string Magenta = "\x1B[35m";
		public readonly static string Cyan = "\x1B[36m";
		public readonly static string White = "\x1B[37m";

		public static string RGB(byte r, byte g, byte b) => $"\x1B[38;2;{r};{g};{b}m";
	}
}

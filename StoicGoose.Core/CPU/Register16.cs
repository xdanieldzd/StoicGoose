using System.Runtime.InteropServices;

namespace StoicGoose.Core.CPU
{
	[StructLayout(LayoutKind.Explicit)]
	public struct Register16
	{
		[FieldOffset(0)]
		public byte Low;
		[FieldOffset(1)]
		public byte High;

		[FieldOffset(0)]
		public ushort Word;

		public static implicit operator Register16(ushort value) => new() { Word = value };
		public static implicit operator ushort(Register16 reg) => reg.Word;

		public static ushort operator +(Register16 a, Register16 b) => (ushort)(a.Word + b.Word);
	}
}

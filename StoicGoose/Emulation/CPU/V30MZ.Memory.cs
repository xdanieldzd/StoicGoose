namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		public delegate byte MemoryReadDelegate(uint address);
		public delegate void MemoryWriteDelegate(uint address, byte value);
		public delegate byte RegisterReadDelegate(ushort register);
		public delegate void RegisterWriteDelegate(ushort register, byte value);

		readonly MemoryReadDelegate memoryReadDelegate;
		readonly MemoryWriteDelegate memoryWriteDelegate;
		readonly RegisterReadDelegate registerReadDelegate;
		readonly RegisterWriteDelegate registerWriteDelegate;

		private byte ReadMemory8(ushort segment, ushort offset)
		{
			return memoryReadDelegate((uint)((segment << 4) + offset));
		}

		private ushort ReadMemory16(ushort segment, ushort offset)
		{
			return (ushort)((memoryReadDelegate((uint)((segment << 4) + offset + 1)) << 8) | memoryReadDelegate((uint)((segment << 4) + offset)));
		}

		private void WriteMemory8(ushort segment, ushort offset, byte value)
		{
			memoryWriteDelegate((uint)((segment << 4) + offset), value);
		}

		private void WriteMemory16(ushort segment, ushort offset, ushort value)
		{
			memoryWriteDelegate((uint)((segment << 4) + offset), (byte)(value & 0xFF));
			memoryWriteDelegate((uint)((segment << 4) + offset + 1), (byte)(value >> 8));
		}
	}
}

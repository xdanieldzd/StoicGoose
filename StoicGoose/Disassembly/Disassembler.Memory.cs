namespace StoicGoose.Disassembly
{
	public delegate byte MemoryReadDelegate(uint address);
	public delegate void MemoryWriteDelegate(uint address, byte value);

	public partial class Disassembler
	{
		public byte ReadMemory8(ushort segment, ushort offset)
		{
			return ReadDelegate((uint)((segment << 4) + offset));
		}

		public ushort ReadMemory16(ushort segment, ushort offset)
		{
			return (ushort)((ReadDelegate((uint)((segment << 4) + offset + 1)) << 8) | ReadDelegate((uint)((segment << 4) + offset)));
		}

		public void WriteMemory8(ushort segment, ushort offset, byte value)
		{
			WriteDelegate((uint)((segment << 4) + offset), value);
		}

		public void WriteMemory16(ushort segment, ushort offset, ushort value)
		{
			WriteDelegate((uint)((segment << 4) + offset), (byte)(value & 0xFF));
			WriteDelegate((uint)((segment << 4) + offset + 1), (byte)(value >> 8));
		}
	}
}

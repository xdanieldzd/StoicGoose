namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		readonly MemoryReadDelegate memoryReadDelegate;
		readonly MemoryWriteDelegate memoryWriteDelegate;
		readonly PortReadDelegate portReadDelegate;
		readonly PortWriteDelegate portWriteDelegate;

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

		private byte ReadPort8(ushort port)
		{
			return portReadDelegate(port);
		}

		private ushort ReadPort16(ushort port)
		{
			return (ushort)(portReadDelegate((ushort)(port + 1)) << 8 | portReadDelegate(port));
		}

		private void WritePort8(ushort port, byte value)
		{
			portWriteDelegate(port, value);
		}

		private void WritePort16(ushort port, ushort value)
		{
			portWriteDelegate(port, (byte)(value & 0xFF));
			portWriteDelegate((ushort)(port + 1), (byte)(value >> 8));
		}
	}
}

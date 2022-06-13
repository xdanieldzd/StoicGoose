namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		private byte ReadMemory8(ushort segment, ushort offset)
		{
			return machine.ReadMemory((uint)((segment << 4) + offset));
		}

		private ushort ReadMemory16(ushort segment, ushort offset)
		{
			return (ushort)((machine.ReadMemory((uint)((segment << 4) + offset + 1)) << 8) | machine.ReadMemory((uint)((segment << 4) + offset)));
		}

		private void WriteMemory8(ushort segment, ushort offset, byte value)
		{
			machine.WriteMemory((uint)((segment << 4) + offset), value);
		}

		private void WriteMemory16(ushort segment, ushort offset, ushort value)
		{
			machine.WriteMemory((uint)((segment << 4) + offset), (byte)(value & 0xFF));
			machine.WriteMemory((uint)((segment << 4) + offset + 1), (byte)(value >> 8));
		}

		private ushort ReadPort16(ushort port)
		{
			return (ushort)(machine.ReadPort((ushort)(port + 1)) << 8 | machine.ReadPort(port));
		}

		private void WritePort16(ushort port, ushort value)
		{
			machine.WritePort(port, (byte)(value & 0xFF));
			machine.WritePort((ushort)(port + 1), (byte)(value >> 8));
		}
	}
}

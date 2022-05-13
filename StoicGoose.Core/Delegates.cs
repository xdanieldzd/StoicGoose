namespace StoicGoose.Core
{
	public delegate byte MemoryReadDelegate(uint address);
	public delegate void MemoryWriteDelegate(uint address, byte value);
	public delegate byte PortReadDelegate(ushort port);
	public delegate void PortWriteDelegate(ushort port, byte value);

	public delegate byte WaveTableReadDelegate(ushort address);
}

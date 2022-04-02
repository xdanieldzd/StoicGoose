﻿namespace StoicGoose.Emulation
{
	public delegate byte MemoryReadDelegate(uint address);
	public delegate void MemoryWriteDelegate(uint address, byte value);
	public delegate byte RegisterReadDelegate(ushort register);
	public delegate void RegisterWriteDelegate(ushort register, byte value);
}
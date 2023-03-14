using System;

using StoicGoose.Core.Interfaces;

namespace StoicGoose.Core
{
	public class Bootstrap : IComponent
	{
		readonly byte[] rom = Array.Empty<byte>();
		readonly uint romMask = 0;

		public Bootstrap(int size)
		{
			rom = new byte[size];
			romMask = (uint)(rom.Length - 1);
		}

		public void Reset()
		{
			//
		}

		public void Shutdown()
		{
			//
		}

		public void LoadRom(byte[] data)
		{
			if (data.Length != rom.Length)
				throw new Exception("Data size mismatch error");

			Buffer.BlockCopy(data, 0, rom, 0, data.Length);
		}

		public byte ReadMemory(uint address)
		{
			return rom[address & romMask];
		}
	}
}

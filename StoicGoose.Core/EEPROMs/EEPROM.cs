using System;

using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.EEPROMs
{
	public sealed class EEPROM : IPortAccessComponent
	{
		readonly byte[] contents = default;
		readonly int numAddressBits = 0;

		bool eraseWriteEnable = false;

		byte dataLo, dataHi, addressLo, addressHi, statusCmd;

		public EEPROM(int size, int addressBits)
		{
			contents = new byte[size];
			numAddressBits = addressBits;
		}

		public void Reset()
		{
			dataLo = dataHi = 0;
			addressLo = addressHi = 0;
			statusCmd = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void LoadContents(byte[] data)
		{
			if (data.Length != contents.Length)
				throw new Exception("Data size mismatch error");

			Buffer.BlockCopy(data, 0, contents, 0, data.Length);
		}

		public byte[] GetContents()
		{
			return contents;
		}

		public void Program(int address, byte value)
		{
			contents[address & (contents.Length - 1)] = value;
		}

		private void BeginAccess()
		{
			var addressHiLo = (addressHi << 8) | addressLo;

			var address = (addressHiLo & ((1 << numAddressBits) - 1)) << 1;
			var opcode = (addressHiLo >> (numAddressBits + 0)) & 0b11;
			var extOpcode = (addressHiLo >> (numAddressBits - 2)) & 0b11; // if opcode == 0, then extended
			var start = ((addressHiLo >> (numAddressBits + 2)) & 0b1) == 0b1;

			// only one bit may be set
			var requestBitsSet = 0;
			for (var i = 4; i < 8; i++) requestBitsSet += (statusCmd >> i) & 0b1;
			if (requestBitsSet != 1) return;

			switch ((statusCmd >> 4) & 0b1111)
			{
				/* Read request? */
				case 0b0001:
					PerformAccess(start, opcode, extOpcode, address);
					ChangeBit(ref statusCmd, 0, true);
					ChangeBit(ref statusCmd, 4, false);
					break;

				/* Write request? */
				case 0b0010:
					PerformAccess(start, opcode, extOpcode, address);
					ChangeBit(ref statusCmd, 1, true);
					ChangeBit(ref statusCmd, 5, false);
					break;

				/* Erase or misc request? */
				case 0b0100:
					PerformAccess(start, opcode, extOpcode, address);
					ChangeBit(ref statusCmd, 2, true);
					ChangeBit(ref statusCmd, 6, false);
					break;

				/* Reset request? */
				case 0b1000:
					Reset();
					ChangeBit(ref statusCmd, 3, true);
					ChangeBit(ref statusCmd, 7, false);
					break;
			}
		}

		private void PerformAccess(bool start, int opcode, int extOpcode, int address)
		{
			if (!start) return;

			switch (opcode)
			{
				/* EWEN/ERAL/WRAL/EWDS */
				case 0b00:
					{
						switch (extOpcode)
						{
							/* EWDS */
							case 0b00:
								eraseWriteEnable = false;
								break;

							/* WRAL */
							case 0b01:
								if (eraseWriteEnable)
								{
									for (var i = 0; i < contents.Length; i += 2)
									{
										contents[i + 0] = dataLo;
										contents[i + 1] = dataHi;
									}
								}
								break;

							/* ERAL */
							case 0b10:
								if (eraseWriteEnable)
								{
									for (var i = 0; i < contents.Length; i++)
										contents[i] = 0xFF;
								}
								break;

							/* EWEN */
							case 0b11:
								eraseWriteEnable = true;
								break;
						}
					}
					break;

				/* WRITE */
				case 0b01:
					if (eraseWriteEnable)
					{
						contents[address + 0] = dataLo;
						contents[address + 1] = dataHi;
					}
					break;

				/* READ */
				case 0b10:
					dataLo = contents[address + 0];
					dataHi = contents[address + 1];
					break;

				/* ERASE */
				case 0b11:
					if (eraseWriteEnable)
					{
						dataLo = contents[address + 0];
						contents[address + 0] = 0xFF;
						dataHi = contents[address + 1];
						contents[address + 1] = 0xFF;
					}
					break;
			}
		}

		public byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				case 0x00: retVal = dataLo; break;
				case 0x01: retVal = dataHi; break;
				case 0x02: retVal = addressLo; break;
				case 0x03: retVal = addressHi; break;
				case 0x04: retVal = (byte)(statusCmd & 0b0011); break;
			}

			return retVal;
		}

		public void WritePort(ushort port, byte value)
		{
			switch (port)
			{
				case 0x00: dataLo = value; break;
				case 0x01: dataHi = value; break;
				case 0x02: addressLo = value; break;
				case 0x03: addressHi = value; break;
				case 0x04:
					statusCmd = (byte)((statusCmd & 0b00001111) | (value & 0b11110000));
					BeginAccess();
					break;
			}
		}
	}
}

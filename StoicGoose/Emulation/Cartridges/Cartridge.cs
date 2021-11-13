using System;

namespace StoicGoose.Emulation.Cartridges
{
	public class Cartridge
	{
		byte[] rom, sram;
		ushort[] eeprom;
		uint romMask, sramMask;

		Metadata metadata;

		/* REG_BANK_xxx */
		byte romBank2, sramBank, romBank0, romBank1;

		/* REG_EEP_xxx */
		// TODO merge internal & cart eeproms to separate eeprom class
		ushort eepData, eepAddressHiLo;
		bool eepStart, eepWriteEnable;
		byte eepCommand, eepAddress, eepStatus;

		// REG_...

		public Metadata Metadata => metadata;

		public Cartridge()
		{
			rom = Array.Empty<byte>();
			sram = Array.Empty<byte>();
			eeprom = Array.Empty<ushort>();
		}

		public void Reset()
		{
			romBank2 = 0xFF;
			sramBank = 0xFF;
			romBank0 = 0xFF;
			romBank1 = 0xFF;

			eepData = eepAddressHiLo = 0;
			eepStart = eepWriteEnable = false;
			eepCommand = eepAddress = eepStatus = 0;
		}

		public void LoadRom(byte[] data)
		{
			rom = data;
			romMask = (uint)(rom.Length - 1);

			metadata = new Metadata(rom);

			if (metadata.SaveSize != 0)
			{
				if (metadata.IsSramSave)
				{
					sram = new byte[metadata.SaveSize];
					sramMask = (uint)(sram.Length - 1);
				}
				else if (metadata.IsEepromSave)
				{
					eeprom = new ushort[metadata.SaveSize];
				}
			}
		}

		public void LoadSram(byte[] data)
		{
			if (data.Length != sram.Length) throw new Exception("Sram size mismatch");
			Buffer.BlockCopy(data, 0, sram, 0, data.Length);
		}

		public byte[] GetSram()
		{
			return sram.Clone() as byte[];
		}

		public byte ReadMemory(uint address)
		{
			return address switch
			{
				/* SRAM */
				var n when n >= 0x010000 && n < 0x020000 => sram.Length != 0 ? sram[((uint)(sramBank << 16) | (address & 0x0FFFF)) & sramMask] : (byte)0x90,
				/* ROM bank 0 */
				var n when n >= 0x020000 && n < 0x030000 => rom[((uint)(romBank0 << 16) | (address & 0x0FFFF)) & romMask],
				/* ROM bank 1 */
				var n when n >= 0x030000 && n < 0x040000 => rom[((uint)(romBank1 << 16) | (address & 0x0FFFF)) & romMask],
				/* ROM bank 2 */
				var n when n >= 0x040000 && n < 0x100000 => rom[((uint)(romBank2 << 20) | (address & 0xFFFFF)) & romMask],
				/* Unmapped */
				_ => 0x90,
			};
		}

		public void WriteMemory(uint address, byte value)
		{
			/* SRAM */
			if (address >= 0x010000 && address < 0x020000 && sram.Length != 0)
				sram[((uint)(sramBank << 16) | (address & 0x0FFFF)) & sramMask] = value;
		}

		public byte ReadRegister(ushort register)
		{
			return register switch
			{
				/* REG_BANK_ROM2 */
				0xC0 => romBank2,
				/* REG_BANK_SRAM */
				0xC1 => sramBank,
				/* REG_BANK_ROM0 */
				0xC2 => romBank0,
				/* REG_BANK_ROM1 */
				0xC3 => romBank1,
				/* REG_EEP_DATA (low) */
				0xC4 => (byte)(eepData & 0xFF),
				/* REG_EEP_DATA (high) */
				0xC5 => (byte)((eepData >> 8) & 0xFF),
				/* REG_EEP_ADDR (low) */
				0xC6 => (byte)(eepAddressHiLo & 0xFF),
				/* REG_EEP_ADDR (high) */
				0xC7 => (byte)((eepAddressHiLo >> 8) & 0xFF),
				/* REG_EEP_STATUS (read) */
				0xC8 => (byte)(eepStatus & 0b11),
				/* Unmapped */
				_ => 0x90,
			};
		}

		public void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				case 0xC0:
					/* REG_BANK_ROM2 */
					romBank2 = value;
					break;

				case 0xC1:
					/* REG_BANK_SRAM */
					sramBank = value;
					break;

				case 0xC2:
					/* REG_BANK_ROM0 */
					romBank0 = value;
					break;

				case 0xC3:
					/* REG_BANK_ROM1 */
					romBank1 = value;
					break;

				case 0xC4:
					/* REG_EEP_DATA (low) */
					eepData = (ushort)((eepData & 0xFF00) | value);
					break;

				case 0xC5:
					/* REG_EEP_DATA (high) */
					eepData = (ushort)((eepData & 0x00FF) | (value << 8));
					break;

				case 0xC6:
					/* REG_EEP_ADDR (low) */
					eepAddressHiLo = (ushort)((eepAddressHiLo & 0xFF00) | value);

					eepStart = ((eepAddressHiLo >> 8) & 0b1) == 0b1;
					eepAddress = (byte)((eepAddressHiLo >> 0) & 0b111111);
					eepCommand = (byte)((eepAddressHiLo >> 6) & 0b11);
					break;

				case 0xC7:
					/* REG_EEP_ADDR (high) */
					eepAddressHiLo = (ushort)((eepAddressHiLo & 0x00FF) | (value << 8));

					eepStart = ((eepAddressHiLo >> 8) & 0b1) == 0b1;
					eepAddress = (byte)((eepAddressHiLo >> 0) & 0b111111);
					eepCommand = (byte)((eepAddressHiLo >> 6) & 0b11);
					break;

				case 0xC8:
					/* REG_EEP_CMD (write) */

					// TODO
					if (!eepStart) break;   // ????

					switch ((value >> 4) & 0b111)
					{
						case 0b001:
							/* READ */
							// TODO
							eepData = eeprom[eepAddress];
							eepStatus = 3;
							break;

						case 0b010:
							/* WRITE/WRAL */
							// TODO
							eepStatus = 2;
							break;

						case 0b100:
							/* EWEN/EWDS/ERAL/ERASE */
							// TODO
							if (eepCommand == 0)
							{
								var extCommand = (eepAddress >> 4) & 0b11;
								switch (extCommand)
								{
									case 0:
										/* EWDS */
										eepWriteEnable = false;
										break;
									case 2:
										/* ERAL */
										if (eepWriteEnable)
											for (var i = 0; i < eeprom.Length; i++)
												eeprom[i] = 0xFFFF;
										break;
									case 3:
										/* EWEN */
										eepWriteEnable = true;
										break;
								}
							}
							else
							{
								if (eepWriteEnable)
									eeprom[eepAddress] = 0;
							}

							eepStatus = 2;
							break;
					}
					break;

					//
			}
		}
	}
}

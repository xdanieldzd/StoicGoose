using System;

using StoicGoose.Emulation.EEPROMs;

namespace StoicGoose.Emulation.Cartridges
{
	public class Cartridge : IComponent
	{
		byte[] rom, sram;
		uint romMask, sramMask;

		Metadata metadata;

		/* REG_BANK_xxx */
		byte romBank2, sramBank, romBank0, romBank1;

		/* REG_EEP_xxx -> EEPROM class */
		EEPROM eeprom = default;

		public Metadata Metadata => metadata;

		public Cartridge()
		{
			rom = Array.Empty<byte>();
			sram = Array.Empty<byte>();
		}

		public void Reset()
		{
			romBank2 = 0xFF;
			sramBank = 0xFF;
			romBank0 = 0xFF;
			romBank1 = 0xFF;

			eeprom?.Reset();
		}

		public void Shutdown()
		{
			eeprom?.Shutdown();
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
					switch (metadata.SaveType)
					{
						// TODO: verify size/address bits
						case Metadata.SaveTypes.Eeprom1Kbit: eeprom = new EEPROM(metadata.SaveSize, 6); break;
						case Metadata.SaveTypes.Eeprom16Kbit: eeprom = new EEPROM(metadata.SaveSize, 10); break;
						case Metadata.SaveTypes.Eeprom8Kbit: eeprom = new EEPROM(metadata.SaveSize, 9); break;
					}
				}
			}
		}

		public void LoadSram(byte[] data)
		{
			if (data.Length != sram.Length) throw new Exception("Sram size mismatch");
			Buffer.BlockCopy(data, 0, sram, 0, data.Length);
		}

		public void LoadEeprom(byte[] data)
		{
			eeprom?.LoadContents(data);
		}

		public byte[] GetSram()
		{
			return sram.Clone() as byte[];
		}

		public byte[] GetEeprom()
		{
			return eeprom?.GetContents().Clone() as byte[];
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
				0xC4 => eeprom != null ? eeprom.ReadRegister((byte)(register - 0xC4)) : (byte)0x90,
				/* REG_EEP_DATA (high) */
				0xC5 => eeprom != null ? eeprom.ReadRegister((byte)(register - 0xC4)) : (byte)0x90,
				/* REG_EEP_ADDR (low) */
				0xC6 => eeprom != null ? eeprom.ReadRegister((byte)(register - 0xC4)) : (byte)0x90,
				/* REG_EEP_ADDR (high) */
				0xC7 => eeprom != null ? eeprom.ReadRegister((byte)(register - 0xC4)) : (byte)0x90,
				/* REG_EEP_STATUS (read) */
				0xC8 => eeprom != null ? eeprom.ReadRegister((byte)(register - 0xC4)) : (byte)0x90,
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
				case 0xC5:
				case 0xC6:
				case 0xC7:
				case 0xC8:
					/* REG_EEP_DATA (low) */
					/* REG_EEP_DATA (high) */
					/* REG_EEP_ADDR (low) */
					/* REG_EEP_ADDR (high) */
					/* REG_EEP_CMD (write) */
					eeprom?.WriteRegister((byte)(register - 0xC4), value);
					break;
			}
		}
	}
}

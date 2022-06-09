using System;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.EEPROMs;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.Core.Cartridges
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

		/* REG_RTC_xxx -> RTC class */
		RTC rtc = default;

		public bool IsLoaded => rom?.Length > 0;
		public int SizeInBytes => rom?.Length ?? 0;
		public uint Crc32 { get; private set; } = default;

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
			rtc?.Reset();

			// HACK: set RTC to current date/time on boot for testing
			rtc?.Program(DateTime.Now);
		}

		public void Shutdown()
		{
			eeprom?.Shutdown();
			rtc?.Shutdown();
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

			if (metadata.IsRtcPresent)
			{
				// NOTE: "RTC present" flag is not entirely consistent; ex. Digimon Tamers Battle Spirit has the flag, but does not have an RTC
				rtc = new RTC();
			}

			Crc32 = Common.Utilities.Crc32.Calculate(rom);

			Log.WriteEvent(LogSeverity.Information, this, "ROM loaded.");
			Log.WriteLine($"~ {Ansi.Cyan}Cartridge metadata{Ansi.Reset} ~");
			Log.WriteLine($" Publisher ID: {Metadata.PublisherCode}, {Metadata.PublisherName} [0x{Metadata.PublisherId:X2}]");
			Log.WriteLine($" System type: {Metadata.SystemType}");
			Log.WriteLine($" Game ID: 0x{Metadata.GameId:X2}");
			Log.WriteLine($"  Calculated ID string: {Metadata.GameIdString}");
			Log.WriteLine($" Game revision: 0x{Metadata.GameRevision:X2}");
			Log.WriteLine($" ROM size: {Metadata.RomSize} [0x{(byte)Metadata.RomSize:X2}]");
			Log.WriteLine($" Save type/size: {Metadata.SaveType}/{Metadata.SaveSize} [0x{(byte)Metadata.SaveType:X2}]");
			Log.WriteLine($" Misc flags: 0x{Metadata.MiscFlags:X2}");
			Log.WriteLine($"  Orientation: {Metadata.Orientation}");
			Log.WriteLine($"  ROM bus width: {Metadata.RomBusWidth}");
			Log.WriteLine($"  ROM access speed: {Metadata.RomAccessSpeed}");
			Log.WriteLine($" RTC present: {Metadata.IsRtcPresent} [0x{Metadata.RtcPresentFlag:X2}]");
			Log.WriteLine($" Checksum (from metadata): 0x{Metadata.Checksum:X4}");
			Log.WriteLine($"  Checksum (calculated): 0x{Metadata.CalculatedChecksum:X4}");
			Log.WriteLine($"  Checksum is {(metadata.IsChecksumValid ? $"{Ansi.Green}valid" : $"{Ansi.Red}invalid")}{Ansi.Reset}!");

			if (metadata.PublisherId == 0x01 && metadata.GameId == 0x27)
			{
				// HACK: Meitantei Conan - Nishi no Meitantei Saidai no Kiki, prevent crash on startup (see TODO in V30MZ, prefetching)
				rom[0xFFFE8] = 0xEA;
				rom[0xFFFE9] = 0x00;
				rom[0xFFFEA] = 0x00;
				rom[0xFFFEB] = 0x00;
				rom[0xFFFEC] = 0x20;
				Log.WriteLine($"~ {Ansi.Red}Conan prefetch hack enabled{Ansi.Reset} ~");
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

		public bool Step(int clockCyclesInStep)
		{
			return rtc != null && rtc.Step(clockCyclesInStep);
		}

		public byte ReadMemory(uint address)
		{
			return address switch
			{
				/* SRAM */
				var n when n >= 0x010000 && n < 0x020000 && sram.Length != 0 => sram[((uint)(sramBank << 16) | (address & 0x0FFFF)) & sramMask],
				/* ROM bank 0 */
				var n when n >= 0x020000 && n < 0x030000 && rom.Length != 0 => rom[((uint)(romBank0 << 16) | (address & 0x0FFFF)) & romMask],
				/* ROM bank 1 */
				var n when n >= 0x030000 && n < 0x040000 && rom.Length != 0 => rom[((uint)(romBank1 << 16) | (address & 0x0FFFF)) & romMask],
				/* ROM bank 2 */
				var n when n >= 0x040000 && n < 0x100000 && rom.Length != 0 => rom[((uint)(romBank2 << 20) | (address & 0xFFFFF)) & romMask],
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

		public byte ReadPort(ushort port)
		{
			return port switch
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
				0xC4 => eeprom != null ? eeprom.ReadPort((byte)(port - 0xC4)) : (byte)0x90,
				/* REG_EEP_DATA (high) */
				0xC5 => eeprom != null ? eeprom.ReadPort((byte)(port - 0xC4)) : (byte)0x90,
				/* REG_EEP_ADDR (low) */
				0xC6 => eeprom != null ? eeprom.ReadPort((byte)(port - 0xC4)) : (byte)0x90,
				/* REG_EEP_ADDR (high) */
				0xC7 => eeprom != null ? eeprom.ReadPort((byte)(port - 0xC4)) : (byte)0x90,
				/* REG_EEP_STATUS (read) */
				0xC8 => eeprom != null ? eeprom.ReadPort((byte)(port - 0xC4)) : (byte)0x90,
				/* REG_RTC_STATUS (read) */
				0xCA => rtc != null ? rtc.ReadPort((byte)(port - 0xCA)) : (byte)0x90,
				/* REG_RTC_DATA */
				0xCB => rtc != null ? rtc.ReadPort((byte)(port - 0xCA)) : (byte)0x90,
				/* Unmapped */
				_ => 0x90,
			};
		}

		public void WritePort(ushort port, byte value)
		{
			switch (port)
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
					eeprom?.WritePort((byte)(port - 0xC4), value);
					break;

				case 0xCA:
				case 0xCB:
					/* REG_RTC_CMD (write) */
					/* REG_RTC_DATA */
					rtc?.WritePort((byte)(port - 0xCA), value);
					break;
			}
		}
	}
}

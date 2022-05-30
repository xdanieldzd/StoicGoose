using System.Collections.Generic;

namespace StoicGoose.Core.Cartridges
{
	public class Metadata
	{
		public enum SystemTypes : byte
		{
			WonderSwan = 0x00,
			WonderSwanColor = 0x01
		}

		public enum RomSizes : byte
		{
			Rom1Mbit = 0x00,     // ???
			Rom2Mbit = 0x01,     // ???
			Rom4Mbit = 0x02,
			Rom8Mbit = 0x03,
			Rom16Mbit = 0x04,
			Rom24Mbit = 0x05,
			Rom32Mbit = 0x06,
			Rom48Mbit = 0x07,
			Rom64Mbit = 0x08,
			Rom128Mbit = 0x09
		}

		public enum SaveTypes : byte
		{
			None = 0x00,
			Sram64Kbit = 0x01,
			Sram256Kbit = 0x02,
			Sram1Mbit = 0x03,
			Sram2Mbit = 0x04,
			Sram4Mbit = 0x05,
			Eeprom1Kbit = 0x10,
			Eeprom16Kbit = 0x20,
			Eeprom8Kbit = 0x50      //???
		}

		public enum Orientations : byte
		{
			Horizontal = 0 << 0,
			Vertical = 1 << 0,
		}

		public enum RomBusWidths : byte
		{
			Width16Bit = 0 << 1,
			Width8Bit = 1 << 1,
		}

		public enum RomAccessSpeeds : byte
		{
			Speed3Cycle = 0 << 2,
			Speed1Cycle = 1 << 2
		}

		readonly Dictionary<byte, (string code, string name)> publishers = new()
		{
			{ 0x00, ("???", "Misc. (invalid)") },
			{ 0x01, ("BAN", "Bandai") },
			{ 0x02, ("TAT", "Taito") },
			{ 0x03, ("TMY", "Tomy") },
			{ 0x04, ("KEX", "Koei") },
			{ 0x05, ("DTE", "Data East") },
			{ 0x06, ("AAE", "Asmik Ace") },
			{ 0x07, ("MDE", "Media Entertainment") },
			{ 0x08, ("NHB", "Nichibutsu") },
			{ 0x0A, ("CCJ", "Coconuts Japan") },
			{ 0x0B, ("SUM", "Sammy") },
			{ 0x0C, ("SUN", "Sunsoft") },
			{ 0x0D, ("PAW", "Mebius (?)") },
			{ 0x0E, ("BPR", "Banpresto") },
			{ 0x10, ("JLC", "Jaleco") },
			{ 0x11, ("MGA", "Imagineer") },
			{ 0x12, ("KNM", "Konami") },
			{ 0x16, ("KBS", "Kobunsha") },
			{ 0x17, ("BTM", "Bottom Up") },
			{ 0x18, ("KGT", "Kaga Tech") },
			{ 0x19, ("SRV", "Sunrise") },
			{ 0x1A, ("CFT", "Cyber Front") },
			{ 0x1B, ("MGH", "Mega House") },
			{ 0x1D, ("BEC", "Interbec") },
			{ 0x1E, ("NAP", "Nihon Application") },
			{ 0x1F, ("BVL", "Bandai Visual") },
			{ 0x20, ("ATN", "Athena") },
			{ 0x21, ("KDX", "KID") },
			{ 0x22, ("HAL", "HAL Corporation") },
			{ 0x23, ("YKE", "Yuki Enterprise") },
			{ 0x24, ("OMM", "Omega Micott") },
			{ 0x25, ("LAY", "Layup") },
			{ 0x26, ("KDK", "Kadokawa Shoten") },
			{ 0x27, ("SHL", "Shall Luck") },
			{ 0x28, ("SQR", "Squaresoft") },
			{ 0x2A, ("SCC", "NTT DoCoMo (?)") },    /* MobileWonderGate */
			{ 0x2B, ("TMC", "Tom Create") },
			{ 0x2D, ("NMC", "Namco") },
			{ 0x2E, ("SES", "Movic (?)") },
			{ 0x2F, ("HTR", "E3 Staff (?)") },
			{ 0x31, ("VGD", "Vanguard") },
			{ 0x32, ("MGT", "Megatron") },
			{ 0x33, ("WIZ", "Wiz") },
			{ 0x36, ("CAP", "Capcom") },
		};

		readonly Dictionary<SaveTypes, int> saveSizes = new()
		{
			{ SaveTypes.None, 0 },
			{ SaveTypes.Sram64Kbit, 1024 * 8 },
			{ SaveTypes.Sram256Kbit, 1024 * 32 },
			{ SaveTypes.Sram1Mbit, 1024 * 128 },
			{ SaveTypes.Sram2Mbit, 1024 * 256 },
			{ SaveTypes.Sram4Mbit, 1024 * 512 },
			{ SaveTypes.Eeprom1Kbit, 2 * 64 },
			{ SaveTypes.Eeprom16Kbit, 2 * 1024 },
			{ SaveTypes.Eeprom8Kbit, 2 * 512 },
		};

		public byte PublisherId { get; private set; }
		public SystemTypes SystemType { get; private set; }
		public byte GameId { get; private set; }
		public byte GameRevision { get; private set; }
		public RomSizes RomSize { get; private set; }
		public SaveTypes SaveType { get; private set; }
		public byte MiscFlags { get; private set; }
		public byte RtcPresentFlag { get; private set; }
		public ushort Checksum { get; private set; }

		public string PublisherCode => publishers.ContainsKey(PublisherId) ? publishers[PublisherId].code : "???";
		public string PublisherName => publishers.ContainsKey(PublisherId) ? publishers[PublisherId].name : "(Unknown)";

		public string GameIdString => $"SWJ-{PublisherCode}{(SystemType == SystemTypes.WonderSwan ? "0" : "C")}{GameId:X2}";

		public Orientations Orientation => (Orientations)(MiscFlags & (1 << 0));
		public RomBusWidths RomBusWidth => (RomBusWidths)(MiscFlags & (1 << 1));
		public RomAccessSpeeds RomAccessSpeed => (RomAccessSpeeds)(MiscFlags & (1 << 2));

		public int SaveSize => saveSizes.ContainsKey(SaveType) ? saveSizes[SaveType] : 0;

		public bool IsSramSave =>
			SaveType == SaveTypes.Sram64Kbit || SaveType == SaveTypes.Sram256Kbit ||
			SaveType == SaveTypes.Sram1Mbit || SaveType == SaveTypes.Sram2Mbit || SaveType == SaveTypes.Sram4Mbit;

		public bool IsEepromSave =>
			SaveType == SaveTypes.Eeprom1Kbit || SaveType == SaveTypes.Eeprom16Kbit || SaveType == SaveTypes.Eeprom8Kbit;

		public bool IsRtcPresent => RtcPresentFlag != 0;

		public ushort CalculatedChecksum { get; private set; }

		public bool IsChecksumValid => Checksum == CalculatedChecksum;

		public Metadata(byte[] data)
		{
			var offset = data.Length - 10;
			PublisherId = data[offset + 0];
			SystemType = (SystemTypes)data[offset + 1];
			GameId = data[offset + 2];
			GameRevision = data[offset + 3];
			RomSize = (RomSizes)data[offset + 4];
			SaveType = (SaveTypes)data[offset + 5];
			MiscFlags = data[offset + 6];
			RtcPresentFlag = data[offset + 7];
			Checksum = (ushort)(data[offset + 9] << 8 | data[offset + 8]);

			CalculatedChecksum = 0;
			for (var i = 0; i < data.Length - 2; i++)
				CalculatedChecksum += data[i + 0];
		}
	}
}

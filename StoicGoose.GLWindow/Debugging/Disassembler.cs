using System;
using System.Collections.Generic;

using Iced.Intel;

using StoicGoose.Core;

namespace StoicGoose.GLWindow.Debugging
{
	public class Disassembler
	{
		readonly Decoder decoder = default;
		readonly MemoryCodeReader memoryCodeReader = new();

		readonly Dictionary<ushort, List<DisassembledInstruction>> segmentCache = new();

		MemoryReadDelegate memoryReadDelegate = default;
		public MemoryReadDelegate ReadMemoryFunction
		{
			get => memoryReadDelegate;
			set => memoryReadDelegate = memoryCodeReader.ReadDelegate = value;
		}

		public Disassembler() => decoder = Decoder.Create(16, memoryCodeReader, DecoderOptions.NoInvalidCheck);

		public List<DisassembledInstruction> DisassembleSegment(ushort segment)
		{
			if (!segmentCache.ContainsKey(segment))
			{
				var instructions = new List<DisassembledInstruction>();

				memoryCodeReader.Position = segment << 4;
				decoder.IP = 0;
				while (decoder.IP < 0x10000 && memoryCodeReader.CanReadByte)
					instructions.Add(new(segment, decoder.Decode(), memoryReadDelegate));

				segmentCache[segment] = instructions;
			}

			return segmentCache[segment];
		}
	}

	public class DisassembledInstruction
	{
		readonly static NasmFormatter formatter = new();
		readonly static StringOutput stringOutput = new();

		public ushort Segment { get; private set; } = 0;
		public ushort Address { get; private set; } = 0;
		public byte[] Bytes { get; private set; } = Array.Empty<byte>();

		public string Mnemonic { get; private set; } = string.Empty;
		public string Comment { get; private set; } = string.Empty;

		public bool IsValid { get; } = false;

		static DisassembledInstruction()
		{
			formatter.Options.BinaryPrefix = "0b";
			formatter.Options.BinarySuffix = null;
			formatter.Options.HexPrefix = "0x";
			formatter.Options.HexSuffix = null;
			formatter.Options.SmallHexNumbersInDecimal = false;
			formatter.Options.SpaceAfterOperandSeparator = true;
		}

		public DisassembledInstruction() { }

		public DisassembledInstruction(ushort segment, Iced.Intel.Instruction icedInstruction, MemoryReadDelegate readDelegate)
		{
			/* Set segment:address pair */
			Segment = segment;
			Address = icedInstruction.IP16;

			/* Get instruction bytes */
			var byteList = new List<byte>();
			for (var i = 0; i < icedInstruction.Length; i++)
				byteList.Add(readDelegate((uint)((segment << 4) + Address + i)));
			Bytes = byteList.ToArray();

			/* Turn invalid instructions into byte declarations */
			if (icedInstruction.IsInvalid)
			{
				icedInstruction.Code = Code.DeclareByte;
				for (var i = 0; i < Bytes.Length; i++)
					icedInstruction.SetDeclareByteValue(i, Bytes[i]);
			}

			/* Generate comment */
			if ((icedInstruction.Code == Code.Out_imm8_AL && icedInstruction.Op0Kind == OpKind.Immediate8) ||
				(icedInstruction.Code == Code.Out_imm8_AX && icedInstruction.Op0Kind == OpKind.Immediate8))
				Comment = wonderSwanPortNamesOutput[icedInstruction.Immediate8];

			else if ((icedInstruction.Code == Code.In_AL_imm8 && icedInstruction.Op1Kind == OpKind.Immediate8) ||
				(icedInstruction.Code == Code.In_AX_imm8 && icedInstruction.Op1Kind == OpKind.Immediate8))
				Comment = wonderSwanPortNamesInput[icedInstruction.Immediate8];

			/* Get mnemonic */
			formatter.Format(icedInstruction, stringOutput);
			Mnemonic = stringOutput.ToStringAndReset();

			/* Yup, this one is valid */
			IsValid = true;
		}

		readonly static string[] wonderSwanPortNamesOutput = new string[256]
		{
			"REG_DISP_CTRL", "REG_BACK_COLOR", "REG_LINE_CUR", "REG_LINE_CMP", "REG_SPR_BASE", "REG_SPR_FIRST", "REG_SPR_COUNT", "REG_MAP_BASE",
			"REG_SCR2_WIN_X0", "REG_SCR2_WIN_Y0", "REG_SCR2_WIN_X1", "REG_SCR2_WIN_Y1", "REG_SPR_WIN_X0", "REG_SPR_WIN_Y0", "REG_SPR_WIN_X1", "REG_SPR_WIN_Y1",
			"REG_SCR1_X", "REG_SCR1_Y", "REG_SCR2_X", "REG_SCR2_Y", "REG_LCD_CTRL", "REG_LCD_ICON", "REG_LCD_VTOTAL", "REG_LCD_VSYNC",
			null, null, null, null, "REG_PALMONO_POOL_0", "REG_PALMONO_POOL_1", "REG_PALMONO_POOL_2", "REG_PALMONO_POOL_3",
			"REG_PALMONO_0_LO", "REG_PALMONO_0_HI", "REG_PALMONO_1_LO", "REG_PALMONO_1_HI", "REG_PALMONO_2_LO", "REG_PALMONO_2_HI", "REG_PALMONO_3_LO", "REG_PALMONO_3_HI",
			"REG_PALMONO_4_LO", "REG_PALMONO_4_HI", "REG_PALMONO_5_LO", "REG_PALMONO_5_HI", "REG_PALMONO_6_LO", "REG_PALMONO_6_HI", "REG_PALMONO_7_LO", "REG_PALMONO_7_HI",
			"REG_PALMONO_8_LO", "REG_PALMONO_8_HI", "REG_PALMONO_9_LO", "REG_PALMONO_9_HI", "REG_PALMONO_A_LO", "REG_PALMONO_A_HI", "REG_PALMONO_B_LO", "REG_PALMONO_B_HI",
			"REG_PALMONO_C_LO", "REG_PALMONO_C_HI", "REG_PALMONO_D_LO", "REG_PALMONO_D_HI", "REG_PALMONO_E_LO", "REG_PALMONO_E_HI", "REG_PALMONO_F_LO", "REG_PALMONO_F_HI",
			"REG_DMA_SRC_LO", "REG_DMA_SRC_MD", "REG_DMA_SRC_HI", null, "REG_DMA_DST_LO", "REG_DMA_DST_HI", "REG_DMA_LEN_LO", "REG_DMA_LEN_HI",
			"REG_DMA_CTRL", null, "REG_SDMA_SRC_LO", "REG_SDMA_SRC_MD", "REG_SDMA_SRC_HI", null, "REG_SDMA_LEN_LO", "REG_SDMA_LEN_MD",
			"REG_SDMA_LEN_HI", null, "REG_SDMA_CTRL", null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			"REG_DISP_MODE", null, "REG_WSC_SYSTEM", null, null, null, null, null,
			null, null, "REG_HYPER_CTRL", "REG_HYPER_CHAN_CTRL", null, null, null, null,
			"REG_UNK_70", "REG_UNK_71", "REG_UNK_72", "REG_UNK_73", "REG_UNK_74", "REG_UNK_75", "REG_UNK_76", "REG_UNK_77",
			null, null, null, null, null, null, null, null,
			"REG_SND_CH1_PITCH_LO", "REG_SND_CH1_PITCH_HI", "REG_SND_CH2_PITCH_LO", "REG_SND_CH2_PITCH_HI", "REG_SND_CH3_PITCH_LO", "REG_SND_CH3_PITCH_HI", "REG_SND_CH4_PITCH_LO", "REG_SND_CH4_PITCH_HI",
			"REG_SND_CH1_VOL", "REG_SND_CH2_VOL", "REG_SND_CH3_VOL", "REG_SND_CH4_VOL", "REG_SND_SWEEP_VALUE", "REG_SND_SWEEP_TIME", "REG_SND_NOISE", "REG_SND_WAVE_BASE",
			"REG_SND_CTRL", "REG_SND_OUTPUT", "REG_SND_RANDOM_LO", "REG_SND_RANDOM_HI", "REG_SND_VOICE_CTRL", "REG_SND_HYPERVOICE", "REG_SND_9697_LO", "REG_SND_9697_HI",
			"REG_SND_9899_LO", "REG_SND_9899_HI", "REG_SND_9A", "REG_SND_9B", "REG_SND_9C", "REG_SND_9D", "REG_SND_VOLUME", null,
			"REG_HW_FLAGS", null, "REG_TMR_CTRL", null, "REG_HTMR_FREQ_LO", "REG_HTMR_FREQ_HI", "REG_VTMR_FREQ_LO", "REG_VTMR_FREQ_HI",
			"REG_HTMR_CTR_LO", "REG_HTMR_CTR_HI", "REG_VTMR_CTR_LO", "REG_VTMR_CTR_HI", null, null, null, null,
			"REG_INT_BASE", "REG_SER_DATA", "REG_INT_ENABLE", "REG_SER_STATUS", "REG_INT_STATUS", "REG_KEYPAD", "REG_INT_ACK", null,
			null, null, "REG_IEEP_DATA_LO", "REG_IEEP_DATA_HI", "REG_IEEP_ADDR_LO", "REG_IEEP_ADDR_HI", "REG_IEEP_CMD", null,
			"REG_BANK_ROM2", "REG_BANK_SRAM", "REG_BANK_ROM0", "REG_BANK_ROM1", "REG_EEP_DATA_LO", "REG_EEP_DATA_HI", "REG_EEP_ADDR_LO", "REG_EEP_ADDR_HI",
			"REG_EEP_CMD", null, "REG_RTC_CMD", "REG_RTC_DATA", "REG_GPO_EN", "REG_GPO_DATA", "REG_WW_FLASH_CE", null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
		};

		readonly static string[] wonderSwanPortNamesInput = new string[256]
		{
			"REG_DISP_CTRL", "REG_BACK_COLOR", "REG_LINE_CUR", "REG_LINE_CMP", "REG_SPR_BASE", "REG_SPR_FIRST", "REG_SPR_COUNT", "REG_MAP_BASE",
			"REG_SCR2_WIN_X0", "REG_SCR2_WIN_Y0", "REG_SCR2_WIN_X1", "REG_SCR2_WIN_Y1", "REG_SPR_WIN_X0", "REG_SPR_WIN_Y0", "REG_SPR_WIN_X1", "REG_SPR_WIN_Y1",
			"REG_SCR1_X", "REG_SCR1_Y", "REG_SCR2_X", "REG_SCR2_Y", "REG_LCD_CTRL", "REG_LCD_ICON", "REG_LCD_VTOTAL", "REG_LCD_VSYNC",
			null, null, null, null, "REG_PALMONO_POOL_0", "REG_PALMONO_POOL_1", "REG_PALMONO_POOL_2", "REG_PALMONO_POOL_3",
			"REG_PALMONO_0_LO", "REG_PALMONO_0_HI", "REG_PALMONO_1_LO", "REG_PALMONO_1_HI", "REG_PALMONO_2_LO", "REG_PALMONO_2_HI", "REG_PALMONO_3_LO", "REG_PALMONO_3_HI",
			"REG_PALMONO_4_LO", "REG_PALMONO_4_HI", "REG_PALMONO_5_LO", "REG_PALMONO_5_HI", "REG_PALMONO_6_LO", "REG_PALMONO_6_HI", "REG_PALMONO_7_LO", "REG_PALMONO_7_HI",
			"REG_PALMONO_8_LO", "REG_PALMONO_8_HI", "REG_PALMONO_9_LO", "REG_PALMONO_9_HI", "REG_PALMONO_A_LO", "REG_PALMONO_A_HI", "REG_PALMONO_B_LO", "REG_PALMONO_B_HI",
			"REG_PALMONO_C_LO", "REG_PALMONO_C_HI", "REG_PALMONO_D_LO", "REG_PALMONO_D_HI", "REG_PALMONO_E_LO", "REG_PALMONO_E_HI", "REG_PALMONO_F_LO", "REG_PALMONO_F_HI",
			"REG_DMA_SRC_LO", "REG_DMA_SRC_MD", "REG_DMA_SRC_HI", null, "REG_DMA_DST_LO", "REG_DMA_DST_HI", "REG_DMA_LEN_LO", "REG_DMA_LEN_HI",
			"REG_DMA_CTRL", null, "REG_SDMA_SRC_LO", "REG_SDMA_SRC_MD", "REG_SDMA_SRC_HI", null, "REG_SDMA_LEN_LO", "REG_SDMA_LEN_MD",
			"REG_SDMA_LEN_HI", null, "REG_SDMA_CTRL", null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			"REG_DISP_MODE", null, "REG_WSC_SYSTEM", null, null, null, null, null,
			null, null, "REG_HYPER_CTRL", "REG_HYPER_CHAN_CTRL", null, null, null, null,
			"REG_UNK_70", "REG_UNK_71", "REG_UNK_72", "REG_UNK_73", "REG_UNK_74", "REG_UNK_75", "REG_UNK_76", "REG_UNK_77",
			null, null, null, null, null, null, null, null,
			"REG_SND_CH1_PITCH_LO", "REG_SND_CH1_PITCH_HI", "REG_SND_CH2_PITCH_LO", "REG_SND_CH2_PITCH_HI", "REG_SND_CH3_PITCH_LO", "REG_SND_CH3_PITCH_HI", "REG_SND_CH4_PITCH_LO", "REG_SND_CH4_PITCH_HI",
			"REG_SND_CH1_VOL", "REG_SND_CH2_VOL", "REG_SND_CH3_VOL", "REG_SND_CH4_VOL", "REG_SND_SWEEP_VALUE", "REG_SND_SWEEP_TIME", "REG_SND_NOISE", "REG_SND_WAVE_BASE",
			"REG_SND_CTRL", "REG_SND_OUTPUT", "REG_SND_RANDOM_LO", "REG_SND_RANDOM_HI", "REG_SND_VOICE_CTRL", "REG_SND_HYPERVOICE", "REG_SND_9697_LO", "REG_SND_9697_HI",
			"REG_SND_9899_LO", "REG_SND_9899_HI", "REG_SND_9A", "REG_SND_9B", "REG_SND_9C", "REG_SND_9D", "REG_SND_VOLUME", null,
			"REG_HW_FLAGS", null, "REG_TMR_CTRL", null, "REG_HTMR_FREQ_LO", "REG_HTMR_FREQ_HI", "REG_VTMR_FREQ_LO", "REG_VTMR_FREQ_HI",
			"REG_HTMR_CTR_LO", "REG_HTMR_CTR_HI", "REG_VTMR_CTR_LO", "REG_VTMR_CTR_HI", null, null, null, null,
			"REG_INT_BASE", "REG_SER_DATA", "REG_INT_ENABLE", "REG_SER_STATUS", "REG_INT_STATUS", "REG_KEYPAD", "REG_INT_ACK", null,
			null, null, "REG_IEEP_DATA_LO", "REG_IEEP_DATA_HI", "REG_IEEP_ADDR_LO", "REG_IEEP_ADDR_HI", "REG_IEEP_STATUS", null,
			"REG_BANK_ROM2", "REG_BANK_SRAM", "REG_BANK_ROM0", "REG_BANK_ROM1", "REG_EEP_DATA_LO", "REG_EEP_DATA_HI", "REG_EEP_ADDR_LO", "REG_EEP_ADDR_HI",
			"REG_EEP_STATUS", null, "REG_RTC_STATUS", "REG_RTC_DATA", "REG_GPO_EN", "REG_GPO_DATA", "REG_WW_FLASH_CE", null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null, null,
		};
	}

	public class MemoryCodeReader : CodeReader
	{
		readonly int startPosition = 0, endPosition = 0;
		int currentPosition = 0;

		public int Position
		{
			get => currentPosition - startPosition;
			set
			{
				if (value > Count)
					throw new ArgumentOutOfRangeException(nameof(value));
				currentPosition = startPosition + value;
			}
		}

		public int Count => endPosition - startPosition;

		public bool CanReadByte => currentPosition < endPosition;

		public MemoryReadDelegate ReadDelegate { get; set; } = default;

		public MemoryCodeReader()
		{
			startPosition = 0;
			endPosition = 0x100000;

			currentPosition = 0;
		}

		public override int ReadByte()
		{
			if (currentPosition >= endPosition || ReadDelegate == default)
				return -1;

			return ReadDelegate((uint)currentPosition++);
		}
	}
}

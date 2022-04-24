using System;
using System.Collections.Generic;
using System.Linq;

using Gee.External.Capstone;
using Gee.External.Capstone.X86;

using StoicGoose.Core;

namespace StoicGoose.Debugging
{
	public sealed partial class Disassembler
	{
		const int segmentSize = 0x10000;

		readonly CapstoneX86Disassembler disassembler = default;
		readonly Dictionary<ushort, IEnumerable<Instruction>> disassembledSegmentCache = new();

		public MemoryReadDelegate ReadDelegate { get; set; }

		readonly static Disassembler instance = new();
		public static Disassembler Instance => instance;

		static Disassembler() { }

		Disassembler()
		{
			disassembler = CapstoneDisassembler.CreateX86Disassembler(X86DisassembleMode.Bit16);
			disassembler.DisassembleSyntax = DisassembleSyntax.Intel;
			disassembler.EnableInstructionDetails = true;
			disassembler.EnableSkipDataMode = true;

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Disassembler initialized.");
		}

		~Disassembler()
		{
			disassembler.Dispose();
		}

		public void Reset()
		{
			disassembledSegmentCache.Clear();

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Disassembler reset.");
		}

		public Instruction DisassembleInstruction(ushort segment, ushort address)
		{
			var instructions = disassembledSegmentCache.ContainsKey(segment) ? disassembledSegmentCache[segment] : DisassembleSegment(segment);
			return instructions.First(x => x.Address == address);
		}

		public IEnumerable<Instruction> DisassembleSegment(ushort segment)
		{
			if (disassembledSegmentCache.ContainsKey(segment))
				return disassembledSegmentCache[segment];

			var data = new byte[segmentSize];
			for (var i = 0; i < data.Length; i++) data[i] = ReadDelegate((uint)((segment << 4) + i));

			var instructions = new List<Instruction>();
			foreach (var disassembled in disassembler.Disassemble(data, 0))
				instructions.Add(new Instruction(disassembled));

			disassembledSegmentCache.Add(segment, instructions);

			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Disassembled segment 0x{segment:X4}, {instructions.Count} instruction(s).");

			return instructions;
		}
	}

	public sealed class Instruction
	{
		public ushort Address { get; set; } = 0;
		public byte[] Bytes { get; set; } = Array.Empty<byte>();
		public string Mnemonic { get; set; } = string.Empty;
		public string Operand { get; set; } = string.Empty;
		public string Comment { get; set; } = string.Empty;

		public Instruction() { }

		public Instruction(X86Instruction disassembled)
		{
			Address = (ushort)disassembled.Address;
			Bytes = disassembled.Bytes;
			Mnemonic = disassembled.Mnemonic;
			Operand = disassembled.Operand;

			if (disassembled.Id == X86InstructionId.X86_INS_OUT && disassembled.Details.Operands[0].Type == X86OperandType.Immediate)
				Comment = wonderSwanPortNamesOutput[disassembled.Details.Operands[0].Immediate];
			else if (disassembled.Id == X86InstructionId.X86_INS_IN && disassembled.Details.Operands[1].Type == X86OperandType.Immediate)
				Comment = wonderSwanPortNamesInput[disassembled.Details.Operands[1].Immediate];
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
}

﻿using System.Collections.Generic;

namespace StoicGoose.Disassembly
{
	public partial class Disassembler
	{
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
			"REG_SND_9899_LO", "REG_SND_9899_HI", "REG_SND_9A", "REG_SND_9B", "REG_SND_9C", "REG_SND_9D", "REG_SND_9E", null,
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
			"REG_SND_9899_LO", "REG_SND_9899_HI", "REG_SND_9A", "REG_SND_9B", "REG_SND_9C", "REG_SND_9D", "REG_SND_9E", null,
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

		readonly static string[] registerNames8 = new string[] { "al", "cl", "dl", "bl", "ah", "ch", "dh", "bh" };
		readonly static string[] registerNames16 = new string[] { "ax", "cx", "dx", "bx", "sp", "bp", "si", "di" };
		readonly static string[] segmentNames = new string[] { "es", "cs", "ss", "ds", "es", "cs", "ss", "ds" };

		readonly static string[] group1Opcodes = new string[] { "add", "or", "adc", "sbb", "and", "sub", "xor", "cmp" };
		readonly static string[] group2Opcodes = new string[] { "rol", "ror", "rcl", "rcr", "shl", "shr", null, "sar" };
		readonly static string[] group3Opcodes = new string[] { "test", null, "not", "neg", "mul", "imul", "div", "idiv" };
		readonly static string[] group4Opcodes = new string[] { "inc", "dec", "call", "call", "jmp", "jmp", "push", null };

		readonly static Dictionary<byte, string> basicOpcodeDict = new()
		{
			{ 0x06, "push es" },
			{ 0x07, "pop es" },
			{ 0x0E, "push cs" },
			{ 0x16, "push ss" },
			{ 0x17, "pop ss" },
			{ 0x1E, "push ds" },
			{ 0x1F, "pop ds" },
			{ 0x27, "daa" },
			{ 0x2F, "das" },
			{ 0x37, "aaa" },
			{ 0x3F, "aas" },
			{ 0x40, "inc ax" },
			{ 0x41, "inc cx" },
			{ 0x42, "inc dx" },
			{ 0x43, "inc bx" },
			{ 0x44, "inc sp" },
			{ 0x45, "inc bp" },
			{ 0x46, "inc si" },
			{ 0x47, "inc di" },
			{ 0x48, "dec ax" },
			{ 0x49, "dec cx" },
			{ 0x4A, "dec dx" },
			{ 0x4B, "dec bx" },
			{ 0x4C, "dec sp" },
			{ 0x4D, "dec bp" },
			{ 0x4E, "dec si" },
			{ 0x4F, "dec di" },
			{ 0x50, "push ax" },
			{ 0x51, "push cx" },
			{ 0x52, "push dx" },
			{ 0x53, "push bx" },
			{ 0x54, "push sp" },
			{ 0x55, "push bp" },
			{ 0x56, "push si" },
			{ 0x57, "push di" },
			{ 0x58, "pop ax" },
			{ 0x59, "pop cx" },
			{ 0x5A, "pop dx" },
			{ 0x5B, "pop bx" },
			{ 0x5C, "pop sp" },
			{ 0x5D, "pop bp" },
			{ 0x5E, "pop si" },
			{ 0x5F, "pop di" },
			{ 0x60, "pusha" },
			{ 0x61, "popa" },
			{ 0x98, "cbw" },
			{ 0x99, "cwd" },
			{ 0x9B, "wait" },
			{ 0x9C, "pushf" },
			{ 0x9D, "popf" },
			{ 0x9E, "sahf" },
			{ 0x9F, "lahf" },
			{ 0xC3, "ret" },
			{ 0xC9, "leave" },
			{ 0xCB, "retf" },
			{ 0xCC, "int 3" },
			{ 0xCE, "into" },
			{ 0xCF, "iret" },
			{ 0xEC, "in al,dx" },
			{ 0xED, "in ax,dx" },
			{ 0xEE, "out dx,al" },
			{ 0xEF, "out dx,ax" },
			{ 0xF4, "hlt" },
			{ 0xF5, "cmc" },
			{ 0xF8, "clc" },
			{ 0xF9, "stc" },
			{ 0xFA, "cli" },
			{ 0xFB, "sti" },
			{ 0xFC, "cld" },
			{ 0xFD, "std" },
		};

		readonly static Dictionary<byte, string> conditionalJumpOpcodeDict = new()
		{
			{ 0x70, "jo" },
			{ 0x71, "jno" },
			{ 0x72, "jb" },
			{ 0x73, "jnb" },
			{ 0x74, "jz" },
			{ 0x75, "jnz" },
			{ 0x76, "jbe" },
			{ 0x77, "ja" },
			{ 0x78, "js" },
			{ 0x79, "jns" },
			{ 0x7A, "jpe" },
			{ 0x7B, "jpo" },
			{ 0x7C, "jl" },
			{ 0x7D, "jge" },
			{ 0x7E, "jle" },
			{ 0x7F, "jg" },
		};

		readonly static Dictionary<byte, (string op, string arg)> registerImmediate8OpcodeDict = new()
		{
			{ 0x04, ("add", "al") },
			{ 0x0C, ("or", "al") },
			{ 0x14, ("adc", "al") },
			{ 0x1C, ("sbb", "al") },
			{ 0x24, ("and", "al") },
			{ 0x2C, ("sub", "al") },
			{ 0x34, ("xor", "al") },
			{ 0x3C, ("cmp", "al") },
			{ 0xA8, ("test", "al") },
			{ 0xB0, ("mov", "al") },
			{ 0xB1, ("mov", "cl") },
			{ 0xB2, ("mov", "dl") },
			{ 0xB3, ("mov", "bl") },
			{ 0xB4, ("mov", "ah") },
			{ 0xB5, ("mov", "ch") },
			{ 0xB6, ("mov", "dh") },
			{ 0xB7, ("mov", "bh") },
		};

		readonly static Dictionary<byte, (string op, string arg)> registerImmediate16OpcodeDict = new()
		{
			{ 0x05, ("add", "ax") },
			{ 0x0D, ("or", "ax") },
			{ 0x15, ("adc", "ax") },
			{ 0x1D, ("sbb", "ax") },
			{ 0x25, ("and", "ax") },
			{ 0x2D, ("sub", "ax") },
			{ 0x35, ("xor", "ax") },
			{ 0x3D, ("cmp", "ax") },
			{ 0xA9, ("test", "ax") },
			{ 0xB8, ("mov", "ax") },
			{ 0xB9, ("mov", "cx") },
			{ 0xBA, ("mov", "dx") },
			{ 0xBB, ("mov", "bx") },
			{ 0xBC, ("mov", "sp") },
			{ 0xBD, ("mov", "bp") },
			{ 0xBE, ("mov", "si") },
			{ 0xBF, ("mov", "di") },
		};

		readonly static List<byte> argsEvalLength1List = new()
		{
			0x00,
			0x01,
			0x02,
			0x03,
			0x08,
			0x09,
			0x0A,
			0x0B,
			0x10,
			0x11,
			0x12,
			0x13,
			0x18,
			0x19,
			0x1A,
			0x1B,
			0x20,
			0x21,
			0x22,
			0x23,
			0x28,
			0x29,
			0x2A,
			0x2B,
			0x30,
			0x31,
			0x32,
			0x33,
			0x38,
			0x39,
			0x3A,
			0x3B,
			0x62,
			0x6A,
			0x84,
			0x85,
			0x86,
			0x87,
			0x88,
			0x89,
			0x8A,
			0x8B,
			0x8C,
			0x8D,
			0x8E,
			0x8F,
			0xCD,
			0xD0,
			0xD1,
			0xD2,
			0xD3,
			0xD4,
			0xD5,
			0xE0,
			0xE1,
			0xE2,
			0xE3,
			0xE4,
			0xE5,
			0xE6,
			0xE7,
			0xEB,
			0xFE,
		};

		readonly static List<byte> argsEvalLength2List = new()
		{
			0x68,
			0x6B,
			0x80,
			0x82,
			0x83,
			0xA0,
			0xA1,
			0xA2,
			0xA3,
			0xC0,
			0xC1,
			0xC2,
			0xC6,
			0xCA,
			0xE8,
			0xE9,
		};

		readonly static List<byte> argsEvalLength3List = new()
		{
			0x69,
			0x81,
			0xC4,
			0xC5,
			0xC7,
			0xC8,
		};

		readonly static List<byte> argsEvalLength4List = new()
		{
			0x9A,
			0xEA,
		};
	}
}

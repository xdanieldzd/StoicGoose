﻿using StoicGoose.Emulation.Machines;
using StoicGoose.Interface;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Display
{
	public sealed class SphinxDisplayController : DisplayControllerCommon
	{
		public override double HorizontalClock => MachineCommon.CpuClock / HorizontalTotal;

		const int paletteBase = 0x0FE00;

		/* REG_BACK_COLOR */
		[ImGuiRegister(0x001, "REG_BACK_COLOR")]
		[ImGuiBitDescription("Background color index", 0, 3)]
		public override byte BackColorIndex { get; protected set; }
		[ImGuiRegister(0x001, "REG_BACK_COLOR")]
		[ImGuiBitDescription("Background color palette", 4, 7)]
		public byte BackColorPalette { get; private set; }

		/* REG_SPR_BASE */
		[ImGuiRegister(0x004, "REG_SPR_BASE")]
		[ImGuiBitDescription("Sprite table base address", 0, 5)]
		[ImGuiFormat("X4", 9)]
		public override int SprBase { get; protected set; }

		/* REG_MAP_BASE */
		[ImGuiRegister(0x007, "REG_MAP_BASE")]
		[ImGuiBitDescription("SCR1 base address", 0, 3)]
		[ImGuiFormat("X4", 11)]
		public override int Scr1Base { get; protected set; }
		[ImGuiRegister(0x007, "REG_MAP_BASE")]
		[ImGuiBitDescription("SCR2 base address", 4, 7)]
		[ImGuiFormat("X4", 11)]
		public override int Scr2Base { get; protected set; }

		/* REG_LCD_CTRL */
		[ImGuiRegister(0x014, "REG_LCD_CTRL")]
		[ImGuiBitDescription("LCD contrast setting; high contrast?", 1)]
		public bool LcdContrastHigh { get; private set; }

		/* REG_DISP_MODE */
		public bool DisplayColorFlagSet { get; private set; }
		public bool Display4bppFlagSet { get; private set; }

		bool isGrayscaleMode => !DisplayColorFlagSet && !Display4bppFlagSet;
		bool isColorMode => !isGrayscaleMode;

		bool is4bppDepth => DisplayColorFlagSet && Display4bppFlagSet;
		bool is2bppDepth => !is4bppDepth;

		public SphinxDisplayController(MemoryReadDelegate memoryRead) : base(memoryRead) { }

		protected override void ResetRegisters()
		{
			base.ResetRegisters();

			BackColorPalette = 0;
			LcdContrastHigh = false;
			DisplayColorFlagSet = Display4bppFlagSet = false;
		}

		protected override void RenderBackColor(int y, int x)
		{
			if (isColorMode)
				WriteToFramebuffer(y, x, BackColorPalette, BackColorIndex);
			else
				WriteToFramebuffer(y, x, (byte)(15 - PalMonoPools[BackColorIndex & 0b0111]));
		}

		protected override void RenderSCR1(int y, int x)
		{
			if (!Scr1Enable) return;

			var scrollX = (x + Scr1ScrollX) & 0xFF;
			var scrollY = (y + Scr1ScrollY) & 0xFF;

			var attribs = GetTileAttribs(Scr1Base, scrollY, scrollX);
			var tileNum = GetTileNumber(attribs);
			var tilePal = GetTilePalette(attribs);

			var color = GetPixelColor(tileNum, scrollY ^ (GetTileVerticalFlip(attribs) * 7), scrollX ^ (GetTileHorizontalFlip(attribs) * 7));

			if (IsColorOpaque(tilePal, color))
			{
				SetScreenUsageFlag(y, x, screenUsageSCR1);
				if (isColorMode)
					WriteToFramebuffer(y, x, tilePal, color);
				else
					WriteToFramebuffer(y, x, (byte)(15 - PalMonoPools[PalMonoData[tilePal, color & 0b11]]));
			}
		}

		protected override void RenderSCR2(int y, int x)
		{
			if (!Scr2Enable) return;

			var scrollX = (x + Scr2ScrollX) & 0xFF;
			var scrollY = (y + Scr2ScrollY) & 0xFF;

			var attribs = GetTileAttribs(Scr2Base, scrollY, scrollX);
			var tileNum = GetTileNumber(attribs);
			var tilePal = GetTilePalette(attribs);

			var color = GetPixelColor(tileNum, scrollY ^ (GetTileVerticalFlip(attribs) * 7), scrollX ^ (GetTileHorizontalFlip(attribs) * 7));

			if (IsColorOpaque(tilePal, color))
			{
				if (!Scr2WindowEnable || (Scr2WindowEnable && ((!Scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (Scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					SetScreenUsageFlag(y, x, screenUsageSCR2);
					if (isColorMode)
						WriteToFramebuffer(y, x, tilePal, color);
					else
						WriteToFramebuffer(y, x, (byte)(15 - PalMonoPools[PalMonoData[tilePal, color & 0b11]]));
				}
			}
		}

		protected override void RenderSprites(int y, int x)
		{
			if (!SprEnable) return;

			activeSpriteCountOnLine = 0;
			for (var i = 0; i < spriteCountNextFrame; i++)
			{
				var spriteY = (spriteData[i] >> 16) & 0xFF;
				if ((byte)(y - spriteY) <= 7 && activeSpriteCountOnLine < maxSpritesPerLine)
					activeSpritesOnLine[activeSpriteCountOnLine++] = spriteData[i];
			}

			for (var i = 0; i < activeSpriteCountOnLine; i++)
			{
				var activeSprite = activeSpritesOnLine[i];

				var tileNum = (ushort)(activeSprite & 0x01FF);
				var tilePal = (byte)(((activeSprite >> 9) & 0b111) + 8);
				var windowDisplayOutside = ((activeSprite >> 12) & 0b1) == 0b1;
				var priorityAboveSCR2 = ((activeSprite >> 13) & 0b1) == 0b1;

				var spriteY = (activeSprite >> 16) & 0xFF;
				var spriteX = (activeSprite >> 24) & 0xFF;

				if (x < 0 || x >= HorizontalDisp || (byte)(x - spriteX) > 7) continue;

				var color = GetPixelColor(tileNum, (byte)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (byte)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)));

				if (!SprWindowEnable || (SprWindowEnable && (windowDisplayOutside != IsInsideSPRWindow(y, x))))
				{
					if (IsColorOpaque(tilePal, color) && (!IsScreenUsageFlagSet(y, x, screenUsageSCR2) || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
						{
							SetScreenUsageFlag(y, x, screenUsageSPR);
							if (isColorMode)
								WriteToFramebuffer(y, x, tilePal, color);
							else
								WriteToFramebuffer(y, x, (byte)(15 - PalMonoPools[PalMonoData[tilePal, color & 0b11]]));
						}
					}
				}
			}
		}

		protected override ushort GetTileNumber(ushort attribs)
		{
			return (ushort)((attribs & 0x01FF) | (isColorMode ? (((attribs >> 13) & 0b1) << 9) : 0));
		}

		protected override byte GetPixelColor(ushort tile, int y, int x)
		{
			byte color = 0;

			if (is2bppDepth)
			{
				/* 2bpp, like WS */
				tile &= 0x01FF;

				if (isPlanarMode)
				{
					var address = (uint)(0x2000 + (tile << 4) + ((y % 8) << 1));
					var data = (ushort)(memoryReadDelegate(address + 1) << 8 | memoryReadDelegate(address));
					var color0 = (data >> 7 - (x % 8)) & 0b1;
					var color1 = (data >> 15 - (x % 8)) & 0b1;
					color = (byte)(color1 << 1 | color0);
				}
				else if (isPackedMode)
				{
					var data = memoryReadDelegate((ushort)(0x2000 + (tile << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
					color = (byte)((data >> 6 - (((x % 8) & 0b11) << 1)) & 0b11);
				}
			}
			else
			{
				/* 4bpp, Color-only */
				tile &= 0x03FF;

				if (isPlanarMode)
				{
					var address = (uint)(0x4000 + (tile << 5) + ((y % 8) << 2));
					var data = (uint)(memoryReadDelegate(address + 3) << 24 | memoryReadDelegate(address + 2) << 16 | memoryReadDelegate(address + 1) << 8 | memoryReadDelegate(address));
					var color0 = (data >> 7 - (x % 8)) & 0b1;
					var color1 = (data >> 15 - (x % 8)) & 0b1;
					var color2 = (data >> 23 - (x % 8)) & 0b1;
					var color3 = (data >> 31 - (x % 8)) & 0b1;
					color = (byte)(color3 << 3 | color2 << 2 | color1 << 1 | color0);
				}
				else if (isPackedMode)
				{
					var data = memoryReadDelegate((ushort)(0x4000 + (tile << 5) + ((y % 8) << 2) + ((x % 8) >> 1)));
					color = (byte)((data >> 4 - (((x % 8) & 0b1) << 2)) & 0b1111);
				}
			}

			return color;
		}

		protected override bool IsColorOpaque(byte palette, byte color)
		{
			return color != 0 || (!isColorMode && color == 0 && !IsBitSet(palette, 2));
		}

		private void WriteToFramebuffer(int y, int x, byte palette, byte color)
		{
			var offset = (uint)(paletteBase + (palette << 5) + (color << 1));
			var pixel = (ushort)(memoryReadDelegate(offset + 1) << 8 | memoryReadDelegate(offset));

			byte b = (byte)(((pixel >> 0) & 0b1111) | ((pixel >> 0) & 0b1111) << 4);
			byte g = (byte)(((pixel >> 4) & 0b1111) | ((pixel >> 4) & 0b1111) << 4);
			byte r = (byte)(((pixel >> 8) & 0b1111) | ((pixel >> 8) & 0b1111) << 4);
			WriteToFramebuffer(y, x, r, g, b);
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x00:
					/* REG_DISP_CTRL */
					ChangeBit(ref retVal, 0, Scr1Enable);
					ChangeBit(ref retVal, 1, Scr2Enable);
					ChangeBit(ref retVal, 2, SprEnable);
					ChangeBit(ref retVal, 3, SprWindowEnable);
					ChangeBit(ref retVal, 4, Scr2WindowDisplayOutside);
					ChangeBit(ref retVal, 5, Scr2WindowEnable);
					break;

				case 0x01:
					/* REG_BACK_COLOR */
					retVal |= (byte)((BackColorIndex & 0b1111) << 0);
					retVal |= (byte)((BackColorPalette & 0b1111) << 4);
					break;

				case 0x02:
					/* REG_LINE_CUR */
					retVal |= (byte)(LineCurrent & 0xFF);
					break;

				case 0x03:
					/* REG_LINE_CMP */
					retVal |= (byte)(LineCompare & 0xFF);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					retVal |= (byte)(SprBase & 0b111111);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					retVal |= (byte)(SprFirst & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					retVal |= (byte)(SprCount & 0xFF);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					retVal |= (byte)((Scr1Base & 0b1111) << 0);
					retVal |= (byte)((Scr2Base & 0b1111) << 4);
					break;

				case 0x08:
					/* REG_SCR2_WIN_X0 */
					retVal |= (byte)(Scr2WinX0 & 0xFF);
					break;

				case 0x09:
					/* REG_SCR2_WIN_Y0 */
					retVal |= (byte)(Scr2WinY0 & 0xFF);
					break;

				case 0x0A:
					/* REG_SCR2_WIN_X1 */
					retVal |= (byte)(Scr2WinX1 & 0xFF);
					break;

				case 0x0B:
					/* REG_SCR2_WIN_Y1 */
					retVal |= (byte)(Scr2WinY1 & 0xFF);
					break;

				case 0x0C:
					/* REG_SPR_WIN_X0 */
					retVal |= (byte)(SprWinX0 & 0xFF);
					break;

				case 0x0D:
					/* REG_SPR_WIN_Y0 */
					retVal |= (byte)(SprWinY0 & 0xFF);
					break;

				case 0x0E:
					/* REG_SPR_WIN_X1 */
					retVal |= (byte)(SprWinX1 & 0xFF);
					break;

				case 0x0F:
					/* REG_SPR_WIN_Y1 */
					retVal |= (byte)(SprWinY1 & 0xFF);
					break;

				case 0x10:
					/* REG_SCR1_X */
					retVal |= (byte)(Scr1ScrollX & 0xFF);
					break;

				case 0x11:
					/* REG_SCR1_Y */
					retVal |= (byte)(Scr1ScrollY & 0xFF);
					break;

				case 0x12:
					/* REG_SCR2_X */
					retVal |= (byte)(Scr2ScrollX & 0xFF);
					break;

				case 0x13:
					/* REG_SCR2_Y */
					retVal |= (byte)(Scr2ScrollY & 0xFF);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					ChangeBit(ref retVal, 0, LcdActive);
					ChangeBit(ref retVal, 1, LcdContrastHigh);
					break;

				case 0x15:
					/* REG_LCD_ICON */
					ChangeBit(ref retVal, 0, IconSleep);
					ChangeBit(ref retVal, 1, IconVertical);
					ChangeBit(ref retVal, 2, IconHorizontal);
					ChangeBit(ref retVal, 3, IconAux1);
					ChangeBit(ref retVal, 4, IconAux2);
					ChangeBit(ref retVal, 5, IconAux3);
					break;

				case 0x16:
					/* REG_LCD_VTOTAL */
					retVal |= (byte)(VTotal & 0xFF);
					break;

				case 0x17:
					/* REG_LCD_VSYNC */
					retVal |= (byte)(VSync & 0xFF);
					break;

				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					/* REG_PALMONO_POOL_x */
					retVal |= (byte)(PalMonoPools[((register & 0b11) << 1) | 0] << 0);
					retVal |= (byte)(PalMonoPools[((register & 0b11) << 1) | 1] << 4);
					break;

				case ushort _ when register >= 0x20 && register <= 0x3F:
					/* REG_PALMONO_x */
					retVal |= (byte)(PalMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 0] << 0);
					retVal |= (byte)(PalMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 1] << 4);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					ChangeBit(ref retVal, 5, DisplayPackedFormatSet);
					ChangeBit(ref retVal, 6, DisplayColorFlagSet);
					ChangeBit(ref retVal, 7, Display4bppFlagSet);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					ChangeBit(ref retVal, 0, HBlankTimer.Enable);
					ChangeBit(ref retVal, 1, HBlankTimer.Repeating);
					ChangeBit(ref retVal, 2, VBlankTimer.Enable);
					ChangeBit(ref retVal, 3, VBlankTimer.Repeating);
					break;

				// TODO verify timer reads

				case 0xA4:
				case 0xA5:
					/* REG_HTMR_FREQ */
					retVal |= (byte)((HBlankTimer.Frequency >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xA6:
				case 0xA7:
					/* REG_VTMR_FREQ */
					retVal |= (byte)((VBlankTimer.Frequency >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xA8:
				case 0xA9:
					/* REG_HTMR_CTR */
					retVal |= (byte)((HBlankTimer.Counter >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xAA:
				case 0xAB:
					/* REG_VTMR_CTR */
					retVal |= (byte)((VBlankTimer.Counter >> ((register & 0b1) * 8)) & 0xFF);
					break;
			}

			return retVal;
		}

		public override void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				case 0x00:
					/* REG_DISP_CTRL */
					Scr1Enable = IsBitSet(value, 0);
					Scr2Enable = IsBitSet(value, 1);
					SprEnable = IsBitSet(value, 2);
					SprWindowEnable = IsBitSet(value, 3);
					Scr2WindowDisplayOutside = IsBitSet(value, 4);
					Scr2WindowEnable = IsBitSet(value, 5);
					break;

				case 0x01:
					/* REG_BACK_COLOR */
					BackColorIndex = (byte)((value >> 0) & 0b1111);
					BackColorPalette = (byte)((value >> 4) & 0b1111);
					break;

				case 0x03:
					/* REG_LINE_CMP */
					LineCompare = (byte)(value & 0xFF);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					SprBase = (byte)(value & 0b111111);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					SprFirst = (byte)(value & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					SprCount = (byte)(value & 0xFF);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					Scr1Base = (byte)((value >> 0) & 0b1111);
					Scr2Base = (byte)((value >> 4) & 0b1111);
					break;

				case 0x08:
					/* REG_SCR2_WIN_X0 */
					Scr2WinX0 = (byte)(value & 0xFF);
					break;

				case 0x09:
					/* REG_SCR2_WIN_Y0 */
					Scr2WinY0 = (byte)(value & 0xFF);
					break;

				case 0x0A:
					/* REG_SCR2_WIN_X1 */
					Scr2WinX1 = (byte)(value & 0xFF);
					break;

				case 0x0B:
					/* REG_SCR2_WIN_Y1 */
					Scr2WinY1 = (byte)(value & 0xFF);
					break;

				case 0x0C:
					/* REG_SPR_WIN_X0 */
					SprWinX0 = (byte)(value & 0xFF);
					break;

				case 0x0D:
					/* REG_SPR_WIN_Y0 */
					SprWinY0 = (byte)(value & 0xFF);
					break;

				case 0x0E:
					/* REG_SPR_WIN_X1 */
					SprWinX1 = (byte)(value & 0xFF);
					break;

				case 0x0F:
					/* REG_SPR_WIN_Y1 */
					SprWinY1 = (byte)(value & 0xFF);
					break;

				case 0x10:
					/* REG_SCR1_X */
					Scr1ScrollX = (byte)(value & 0xFF);
					break;

				case 0x11:
					/* REG_SCR1_Y */
					Scr1ScrollY = (byte)(value & 0xFF);
					break;

				case 0x12:
					/* REG_SCR2_X */
					Scr2ScrollX = (byte)(value & 0xFF);
					break;

				case 0x13:
					/* REG_SCR2_Y */
					Scr2ScrollY = (byte)(value & 0xFF);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					LcdActive = IsBitSet(value, 0);
					LcdContrastHigh = IsBitSet(value, 1);
					break;

				case 0x15:
					/* REG_LCD_ICON */
					IconSleep = IsBitSet(value, 0);
					IconVertical = IsBitSet(value, 1);
					IconHorizontal = IsBitSet(value, 2);
					IconAux1 = IsBitSet(value, 3);
					IconAux2 = IsBitSet(value, 4);
					IconAux3 = IsBitSet(value, 5);
					break;

				case 0x16:
					/* REG_LCD_VTOTAL */
					VTotal = (byte)(value & 0xFF);
					break;

				case 0x17:
					/* REG_LCD_VSYNC */
					VSync = (byte)(value & 0xFF);
					break;

				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					/* REG_PALMONO_POOL_x */
					PalMonoPools[((register & 0b11) << 1) | 0] = (byte)((value >> 0) & 0b1111);
					PalMonoPools[((register & 0b11) << 1) | 1] = (byte)((value >> 4) & 0b1111);
					break;

				case ushort _ when register >= 0x20 && register <= 0x3F:
					/* REG_PALMONO_x */
					PalMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 0] = (byte)((value >> 0) & 0b111);
					PalMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 1] = (byte)((value >> 4) & 0b111);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					DisplayPackedFormatSet = IsBitSet(value, 5);
					DisplayColorFlagSet = IsBitSet(value, 6);
					Display4bppFlagSet = IsBitSet(value, 7);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					var hEnable = IsBitSet(value, 0);
					var vEnable = IsBitSet(value, 2);

					if (!HBlankTimer.Enable && hEnable) HBlankTimer.Reload();
					if (!VBlankTimer.Enable && vEnable) VBlankTimer.Reload();

					HBlankTimer.Enable = hEnable;
					HBlankTimer.Repeating = IsBitSet(value, 1);
					VBlankTimer.Enable = vEnable;
					VBlankTimer.Repeating = IsBitSet(value, 3);
					break;

				case 0xA4:
					/* REG_HTMR_FREQ (low) */
					HBlankTimer.Frequency = (ushort)((HBlankTimer.Frequency & 0xFF00) | value);
					HBlankTimer.Counter = (ushort)((HBlankTimer.Counter & 0xFF00) | value);
					break;

				case 0xA5:
					/* REG_HTMR_FREQ (high) */
					HBlankTimer.Frequency = (ushort)((HBlankTimer.Frequency & 0x00FF) | (value << 8));
					HBlankTimer.Counter = (ushort)((HBlankTimer.Counter & 0x00FF) | (value << 8));
					break;

				case 0xA6:
					/* REG_VTMR_FREQ (low) */
					VBlankTimer.Frequency = (ushort)((VBlankTimer.Frequency & 0xFF00) | value);
					VBlankTimer.Counter = (ushort)((VBlankTimer.Counter & 0xFF00) | value);
					break;

				case 0xA7:
					/* REG_VTMR_FREQ (high) */
					VBlankTimer.Frequency = (ushort)((VBlankTimer.Frequency & 0x00FF) | (value << 8));
					VBlankTimer.Counter = (ushort)((VBlankTimer.Counter & 0x00FF) | (value << 8));
					break;
			}
		}
	}
}

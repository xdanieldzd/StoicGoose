using System;

using StoicGoose.Emulation.Machines;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Display
{
	public sealed class AswanDisplayController : DisplayControllerCommon
	{
		public override double HorizontalClock => WonderSwan.CpuClock / HorizontalTotal;

		public AswanDisplayController(MemoryReadDelegate memoryRead) : base(memoryRead) { }

		protected override void RenderBackColor(int y, int x)
		{
			WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[backColorIndex & 0b0111]));
		}

		protected override void RenderSCR1(int y, int x)
		{
			if (!scr1Enable) return;

			var scrollX = (x + scr1ScrollX) & 0xFF;
			var scrollY = (y + scr1ScrollY) & 0xFF;

			var attribs = GetTileAttribs(scr1Base, scrollY, scrollX);
			var tileNum = GetTileNumber(attribs);
			var tilePal = GetTilePalette(attribs);

			var color = GetPixelColor(tileNum, scrollY ^ (GetTileVerticalFlip(attribs) * 7), scrollX ^ (GetTileHorizontalFlip(attribs) * 7));

			if (IsColorOpaque(tilePal, color))
			{
				SetScreenUsageFlag(y, x, screenUsageSCR1);
				WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));
			}
		}

		protected override void RenderSCR2(int y, int x)
		{
			if (!scr2Enable) return;

			var scrollX = (x + scr2ScrollX) & 0xFF;
			var scrollY = (y + scr2ScrollY) & 0xFF;

			var attribs = GetTileAttribs(scr2Base, scrollY, scrollX);
			var tileNum = GetTileNumber(attribs);
			var tilePal = GetTilePalette(attribs);

			var color = GetPixelColor(tileNum, scrollY ^ (GetTileVerticalFlip(attribs) * 7), scrollX ^ (GetTileHorizontalFlip(attribs) * 7));

			if (IsColorOpaque(tilePal, color))
			{
				if (!scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					SetScreenUsageFlag(y, x, screenUsageSCR2);
					WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));
				}
			}
		}

		protected override void RenderSprites(int y, int x)
		{
			if (!sprEnable) return;

			activeSpritesOnLine.Clear();

			for (var i = sprFirst + Math.Min(maxSpriteCount, sprCount) - 1; i >= sprFirst; i--)
			{
				if (spriteData[i] == 0) continue;   //HACK: helps prevent garbage sprites in ex. pocket fighter, but prob only b/c inaccurate timing?

				var spriteY = (spriteData[i] >> 16) & 0xFF;
				if ((byte)(y - spriteY) <= 7 && activeSpritesOnLine.Count < 32)
					activeSpritesOnLine.Add(spriteData[i]);
			}

			foreach (var activeSprite in activeSpritesOnLine)
			{
				var tileNum = (ushort)(activeSprite & 0x01FF);
				var tilePal = (byte)(((activeSprite >> 9) & 0b111) + 8);
				var windowDisplayOutside = ((activeSprite >> 12) & 0b1) == 0b1;
				var priorityAboveSCR2 = ((activeSprite >> 13) & 0b1) == 0b1;

				var spriteY = (activeSprite >> 16) & 0xFF;
				var spriteX = (activeSprite >> 24) & 0xFF;

				if (x < 0 || x >= HorizontalDisp || (byte)(x - spriteX) > 7) continue;

				var color = GetPixelColor(tileNum, (byte)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (byte)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)));

				if (!sprWindowEnable || (sprWindowEnable && (windowDisplayOutside != IsInsideSPRWindow(y, x))))
				{
					if (IsColorOpaque(tilePal, color) && (!IsScreenUsageFlagSet(y, x, screenUsageSCR2) || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
						{
							SetScreenUsageFlag(y, x, screenUsageSPR);
							WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));
						}
					}
				}
			}
		}

		protected override ushort GetTileNumber(ushort attribs)
		{
			return (ushort)(attribs & 0x01FF);
		}

		protected override byte GetPixelColor(ushort tile, int y, int x)
		{
			byte color = 0;

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

			return color;
		}

		protected override bool IsColorOpaque(byte palette, byte color)
		{
			return color != 0 || (color == 0 && !IsBitSet(palette, 2));
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x00:
					/* REG_DISP_CTRL */
					ChangeBit(ref retVal, 0, scr1Enable);
					ChangeBit(ref retVal, 1, scr2Enable);
					ChangeBit(ref retVal, 2, sprEnable);
					ChangeBit(ref retVal, 3, sprWindowEnable);
					ChangeBit(ref retVal, 4, scr2WindowDisplayOutside);
					ChangeBit(ref retVal, 5, scr2WindowEnable);
					break;

				case 0x01:
					/* REG_BACK_COLOR */
					retVal |= (byte)(backColorIndex & 0b111);
					break;

				case 0x02:
					/* REG_LINE_CUR */
					retVal |= (byte)(lineCurrent & 0xFF);
					break;

				case 0x03:
					/* REG_LINE_CMP */
					retVal |= (byte)(lineCompare & 0xFF);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					retVal |= (byte)(sprBase & 0b11111);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					retVal |= (byte)(sprFirst & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					retVal |= (byte)(sprCount & 0xFF);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					retVal |= (byte)((scr1Base & 0b111) << 0);
					retVal |= (byte)((scr2Base & 0b111) << 4);
					break;

				case 0x08:
					/* REG_SCR2_WIN_X0 */
					retVal |= (byte)(scr2WinX0 & 0xFF);
					break;

				case 0x09:
					/* REG_SCR2_WIN_Y0 */
					retVal |= (byte)(scr2WinY0 & 0xFF);
					break;

				case 0x0A:
					/* REG_SCR2_WIN_X1 */
					retVal |= (byte)(scr2WinX1 & 0xFF);
					break;

				case 0x0B:
					/* REG_SCR2_WIN_Y1 */
					retVal |= (byte)(scr2WinY1 & 0xFF);
					break;

				case 0x0C:
					/* REG_SPR_WIN_X0 */
					retVal |= (byte)(sprWinX0 & 0xFF);
					break;

				case 0x0D:
					/* REG_SPR_WIN_Y0 */
					retVal |= (byte)(sprWinY0 & 0xFF);
					break;

				case 0x0E:
					/* REG_SPR_WIN_X1 */
					retVal |= (byte)(sprWinX1 & 0xFF);
					break;

				case 0x0F:
					/* REG_SPR_WIN_Y1 */
					retVal |= (byte)(sprWinY1 & 0xFF);
					break;

				case 0x10:
					/* REG_SCR1_X */
					retVal |= (byte)(scr1ScrollX & 0xFF);
					break;

				case 0x11:
					/* REG_SCR1_Y */
					retVal |= (byte)(scr1ScrollY & 0xFF);
					break;

				case 0x12:
					/* REG_SCR2_X */
					retVal |= (byte)(scr2ScrollX & 0xFF);
					break;

				case 0x13:
					/* REG_SCR2_Y */
					retVal |= (byte)(scr2ScrollY & 0xFF);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					ChangeBit(ref retVal, 0, lcdActive);
					break;

				case 0x15:
					/* REG_LCD_ICON */
					ChangeBit(ref retVal, 0, iconSleep);
					ChangeBit(ref retVal, 1, iconVertical);
					ChangeBit(ref retVal, 2, iconHorizontal);
					ChangeBit(ref retVal, 3, iconAux1);
					ChangeBit(ref retVal, 4, iconAux2);
					ChangeBit(ref retVal, 5, iconAux3);
					break;

				case 0x16:
					/* REG_LCD_VTOTAL */
					retVal |= (byte)(vtotal & 0xFF);
					break;

				case 0x17:
					/* REG_LCD_VSYNC */
					retVal |= (byte)(vsync & 0xFF);
					break;

				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					/* REG_PALMONO_POOL_x */
					retVal |= (byte)(palMonoPools[((register & 0b11) << 1) | 0] << 0);
					retVal |= (byte)(palMonoPools[((register & 0b11) << 1) | 1] << 4);
					break;

				case ushort _ when register >= 0x20 && register <= 0x3F:
					/* REG_PALMONO_x */
					retVal |= (byte)(palMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 0] << 0);
					retVal |= (byte)(palMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 1] << 4);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					ChangeBit(ref retVal, 5, displayPackedFormatSet);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					ChangeBit(ref retVal, 0, hBlankTimer.Enable);
					ChangeBit(ref retVal, 1, hBlankTimer.Repeating);
					ChangeBit(ref retVal, 2, vBlankTimer.Enable);
					ChangeBit(ref retVal, 3, vBlankTimer.Repeating);
					break;

				// TODO verify timer reads

				case 0xA4:
				case 0xA5:
					/* REG_HTMR_FREQ */
					retVal |= (byte)((hBlankTimer.Frequency >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xA6:
				case 0xA7:
					/* REG_VTMR_FREQ */
					retVal |= (byte)((vBlankTimer.Frequency >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xA8:
				case 0xA9:
					/* REG_HTMR_CTR */
					retVal |= (byte)((hBlankTimer.Counter >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0xAA:
				case 0xAB:
					/* REG_VTMR_CTR */
					retVal |= (byte)((vBlankTimer.Counter >> ((register & 0b1) * 8)) & 0xFF);
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
					scr1Enable = IsBitSet(value, 0);
					scr2Enable = IsBitSet(value, 1);
					sprEnable = IsBitSet(value, 2);
					sprWindowEnable = IsBitSet(value, 3);
					scr2WindowDisplayOutside = IsBitSet(value, 4);
					scr2WindowEnable = IsBitSet(value, 5);
					break;

				case 0x01:
					/* REG_BACK_COLOR */
					backColorIndex = (byte)(value & 0b111);
					break;

				case 0x03:
					/* REG_LINE_CMP */
					lineCompare = (byte)(value & 0xFF);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					sprBase = (byte)(value & 0b11111);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					sprFirst = (byte)(value & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					sprCount = (byte)(value & 0xFF);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					scr1Base = (byte)((value >> 0) & 0b111);
					scr2Base = (byte)((value >> 4) & 0b111);
					break;

				case 0x08:
					/* REG_SCR2_WIN_X0 */
					scr2WinX0 = (byte)(value & 0xFF);
					break;

				case 0x09:
					/* REG_SCR2_WIN_Y0 */
					scr2WinY0 = (byte)(value & 0xFF);
					break;

				case 0x0A:
					/* REG_SCR2_WIN_X1 */
					scr2WinX1 = (byte)(value & 0xFF);
					break;

				case 0x0B:
					/* REG_SCR2_WIN_Y1 */
					scr2WinY1 = (byte)(value & 0xFF);
					break;

				case 0x0C:
					/* REG_SPR_WIN_X0 */
					sprWinX0 = (byte)(value & 0xFF);
					break;

				case 0x0D:
					/* REG_SPR_WIN_Y0 */
					sprWinY0 = (byte)(value & 0xFF);
					break;

				case 0x0E:
					/* REG_SPR_WIN_X1 */
					sprWinX1 = (byte)(value & 0xFF);
					break;

				case 0x0F:
					/* REG_SPR_WIN_Y1 */
					sprWinY1 = (byte)(value & 0xFF);
					break;

				case 0x10:
					/* REG_SCR1_X */
					scr1ScrollX = (byte)(value & 0xFF);
					break;

				case 0x11:
					/* REG_SCR1_Y */
					scr1ScrollY = (byte)(value & 0xFF);
					break;

				case 0x12:
					/* REG_SCR2_X */
					scr2ScrollX = (byte)(value & 0xFF);
					break;

				case 0x13:
					/* REG_SCR2_Y */
					scr2ScrollY = (byte)(value & 0xFF);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					lcdActive = IsBitSet(value, 0);
					break;

				case 0x15:
					/* REG_LCD_ICON */
					iconSleep = IsBitSet(value, 0);
					iconVertical = IsBitSet(value, 1);
					iconHorizontal = IsBitSet(value, 2);
					iconAux1 = IsBitSet(value, 3);
					iconAux2 = IsBitSet(value, 4);
					iconAux3 = IsBitSet(value, 5);
					break;

				case 0x16:
					/* REG_LCD_VTOTAL */
					vtotal = (byte)(value & 0xFF);
					break;

				case 0x17:
					/* REG_LCD_VSYNC */
					vsync = (byte)(value & 0xFF);
					break;

				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					/* REG_PALMONO_POOL_x */
					palMonoPools[((register & 0b11) << 1) | 0] = (byte)((value >> 0) & 0b1111);
					palMonoPools[((register & 0b11) << 1) | 1] = (byte)((value >> 4) & 0b1111);
					break;

				case ushort _ when register >= 0x20 && register <= 0x3F:
					/* REG_PALMONO_x */
					palMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 0] = (byte)((value >> 0) & 0b111);
					palMonoData[(register >> 1) & 0b1111, ((register & 0b1) << 1) | 1] = (byte)((value >> 4) & 0b111);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					displayPackedFormatSet = IsBitSet(value, 5);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					var hEnable = IsBitSet(value, 0);
					var vEnable = IsBitSet(value, 2);

					if (!hBlankTimer.Enable && hEnable) hBlankTimer.Reload();
					if (!vBlankTimer.Enable && vEnable) vBlankTimer.Reload();

					hBlankTimer.Enable = hEnable;
					hBlankTimer.Repeating = IsBitSet(value, 1);
					vBlankTimer.Enable = vEnable;
					vBlankTimer.Repeating = IsBitSet(value, 3);
					break;

				case 0xA4:
					/* REG_HTMR_FREQ (low) */
					hBlankTimer.Frequency = (ushort)((hBlankTimer.Frequency & 0xFF00) | value);
					hBlankTimer.Counter = (ushort)((hBlankTimer.Counter & 0xFF00) | value);
					break;

				case 0xA5:
					/* REG_HTMR_FREQ (high) */
					hBlankTimer.Frequency = (ushort)((hBlankTimer.Frequency & 0x00FF) | (value << 8));
					hBlankTimer.Counter = (ushort)((hBlankTimer.Counter & 0x00FF) | (value << 8));
					break;

				case 0xA6:
					/* REG_VTMR_FREQ (low) */
					vBlankTimer.Frequency = (ushort)((vBlankTimer.Frequency & 0xFF00) | value);
					vBlankTimer.Counter = (ushort)((vBlankTimer.Counter & 0xFF00) | value);
					break;

				case 0xA7:
					/* REG_VTMR_FREQ (high) */
					vBlankTimer.Frequency = (ushort)((vBlankTimer.Frequency & 0x00FF) | (value << 8));
					vBlankTimer.Counter = (ushort)((vBlankTimer.Counter & 0x00FF) | (value << 8));
					break;
			}
		}
	}
}

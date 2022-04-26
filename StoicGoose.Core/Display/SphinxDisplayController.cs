using StoicGoose.Common.Attributes;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Display
{
	public sealed class SphinxDisplayController : DisplayControllerCommon
	{
		/* REG_BACK_COLOR */
		byte backColorPalette;
		/* REG_LCD_CTRL */
		bool lcdContrastHigh;
		/* REG_DISP_MODE */
		bool displayColorFlagSet, display4bppFlagSet;

		public SphinxDisplayController(MemoryReadDelegate memoryRead) : base(memoryRead) { }

		protected override void ResetRegisters()
		{
			base.ResetRegisters();

			backColorPalette = 0;
			lcdContrastHigh = false;
			displayColorFlagSet = display4bppFlagSet = false;
		}

		protected override void RenderSleep(int y, int x)
		{
			WriteToFramebuffer(y, x, 0, 0, 0);
		}

		protected override void RenderBackColor(int y, int x)
		{
			if (displayColorFlagSet || display4bppFlagSet)
				WriteToFramebuffer(y, x, backColorPalette, backColorIndex);
			else
				WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[backColorIndex & 0b0111]));
		}

		protected override void RenderSCR1(int y, int x)
		{
			if (!scr1Enable) return;

			var scrollX = (x + scr1ScrollX) & 0xFF;
			var scrollY = (y + scr1ScrollY) & 0xFF;

			var mapOffset = (uint)((scr1Base << 11) | ((scrollY >> 3) << 6) | ((scrollX >> 3) << 1));
			var attribs = ReadMemory16(mapOffset);
			var tileNum = (ushort)((attribs & 0x01FF) | (displayColorFlagSet || display4bppFlagSet ? (((attribs >> 13) & 0b1) << 9) : 0));
			var tilePal = (byte)((attribs >> 9) & 0b1111);

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (color != 0 || (!(displayColorFlagSet || display4bppFlagSet) && color == 0 && !IsBitSet(tilePal, 2)))
			{
				if (displayColorFlagSet || display4bppFlagSet)
					WriteToFramebuffer(y, x, tilePal, color);
				else
					WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal][color & 0b11]]));
			}
		}

		protected override void RenderSCR2(int y, int x)
		{
			if (!scr2Enable) return;

			var scrollX = (x + scr2ScrollX) & 0xFF;
			var scrollY = (y + scr2ScrollY) & 0xFF;

			var mapOffset = (uint)((scr2Base << 11) | ((scrollY >> 3) << 6) | ((scrollX >> 3) << 1));
			var attribs = ReadMemory16(mapOffset);
			var tileNum = (ushort)((attribs & 0x01FF) | (displayColorFlagSet || display4bppFlagSet ? (((attribs >> 13) & 0b1) << 9) : 0));
			var tilePal = (byte)((attribs >> 9) & 0b1111);

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (color != 0 || (!(displayColorFlagSet || display4bppFlagSet) && color == 0 && !IsBitSet(tilePal, 2)))
			{
				if (!scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					isUsedBySCR2[(y * HorizontalDisp) + x] = true;
					if (displayColorFlagSet || display4bppFlagSet)
						WriteToFramebuffer(y, x, tilePal, color);
					else
						WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal][color & 0b11]]));
				}
			}
		}

		protected override void RenderSprites(int y, int x)
		{
			if (!sprEnable) return;

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

				var spriteX = (activeSprite >> 24) & 0xFF;
				if (x < 0 || x >= HorizontalDisp || (byte)(x - spriteX) > 7) continue;

				var windowDisplayOutside = ((activeSprite >> 12) & 0b1) == 0b1;
				if (!sprWindowEnable || (sprWindowEnable && (windowDisplayOutside != IsInsideSPRWindow(y, x))))
				{
					var tileNum = (ushort)(activeSprite & 0x01FF);
					var tilePal = (byte)(((activeSprite >> 9) & 0b111) + 8);
					var priorityAboveSCR2 = ((activeSprite >> 13) & 0b1) == 0b1;
					var spriteY = (activeSprite >> 16) & 0xFF;

					var color = GetPixelColor(tileNum, (byte)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (byte)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)));

					if ((color != 0 || (!(displayColorFlagSet || display4bppFlagSet) && color == 0 && !IsBitSet(tilePal, 2))) && (!isUsedBySCR2[(y * HorizontalDisp) + x] || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
						{
							if (displayColorFlagSet || display4bppFlagSet)
								WriteToFramebuffer(y, x, tilePal, color);
							else
								WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal][color & 0b11]]));
						}
					}
				}
			}
		}

		protected override byte GetPixelColor(ushort tile, int y, int x)
		{
			if (!displayColorFlagSet || !display4bppFlagSet)
			{
				/* 2bpp, like WS */
				if (!displayPackedFormatSet)
				{
					var data = ReadMemory16((uint)(0x2000 + ((tile & 0x01FF) << 4) + ((y % 8) << 1)));
					return (byte)((((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b11);
				}
				else
				{
					var data = ReadMemory8((uint)(0x2000 + ((tile & 0x01FF) << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
					return (byte)((data >> 6 - (((x % 8) & 0b11) << 1)) & 0b11);
				}
			}
			else
			{
				/* 4bpp, Color-only */
				if (!displayPackedFormatSet)
				{
					var data = ReadMemory32((uint)(0x4000 + ((tile & 0x03FF) << 5) + ((y % 8) << 2)));
					return (byte)((((data >> 31 - (x % 8)) & 0b1) << 3 | ((data >> 23 - (x % 8)) & 0b1) << 2 | ((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b1111);
				}
				else
				{
					var data = ReadMemory8((ushort)(0x4000 + ((tile & 0x03FF) << 5) + ((y % 8) << 2) + ((x % 8) >> 1)));
					return (byte)((data >> 4 - (((x % 8) & 0b1) << 2)) & 0b1111);
				}
			}
		}

		private void WriteToFramebuffer(int y, int x, byte palette, byte color)
		{
			var pixel = ReadMemory16((uint)(0x0FE00 + (palette << 5) + (color << 1)));

			int b = (pixel >> 0) & 0b1111;
			int g = (pixel >> 4) & 0b1111;
			int r = (pixel >> 8) & 0b1111;

			// TODO: get a WSC, figure out how high contrast is supposed to look?
			if (lcdContrastHigh)
			{
				b = (b | b << 2) << 2;
				g = (g | g << 2) << 2;
				r = (r | r << 2) << 2;
			}
			else
			{
				b |= b << 4;
				g |= g << 4;
				r |= r << 4;
			}

			WriteToFramebuffer(y, x, (byte)r, (byte)g, (byte)b);
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x01:
					/* REG_BACK_COLOR */
					retVal |= (byte)((backColorIndex & 0b1111) << 0);
					retVal |= (byte)((backColorPalette & 0b1111) << 4);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					retVal |= (byte)(sprBase & 0b111111);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					retVal |= (byte)((scr1Base & 0b1111) << 0);
					retVal |= (byte)((scr2Base & 0b1111) << 4);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					ChangeBit(ref retVal, 0, lcdActive);
					ChangeBit(ref retVal, 1, lcdContrastHigh);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					ChangeBit(ref retVal, 5, displayPackedFormatSet);
					ChangeBit(ref retVal, 6, displayColorFlagSet);
					ChangeBit(ref retVal, 7, display4bppFlagSet);
					break;

				default:
					/* Fall through to common */
					retVal = base.ReadRegister(register);
					break;
			}

			return retVal;
		}

		public override void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				case 0x01:
					/* REG_BACK_COLOR */
					backColorIndex = (byte)((value >> 0) & 0b1111);
					backColorPalette = (byte)((value >> 4) & 0b1111);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					sprBase = (byte)(value & 0b111111);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					scr1Base = (byte)((value >> 0) & 0b1111);
					scr2Base = (byte)((value >> 4) & 0b1111);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					lcdActive = IsBitSet(value, 0);
					lcdContrastHigh = IsBitSet(value, 1);
					break;

				case 0x60:
					/* REG_DISP_MODE */
					displayPackedFormatSet = IsBitSet(value, 5);
					displayColorFlagSet = IsBitSet(value, 6);
					display4bppFlagSet = IsBitSet(value, 7);
					break;

				default:
					/* Fall through to common */
					base.WriteRegister(register, value);
					break;
			}
		}

		[ImGuiRegister("REG_BACK_COLOR", 0x001)]
		[ImGuiBitDescription("Background color index", 0, 3)]
		public override byte BackColorIndex => backColorIndex;
		[ImGuiRegister("REG_BACK_COLOR", 0x001)]
		[ImGuiBitDescription("Background color palette", 4, 7)]
		public byte BackColorPalette => backColorPalette;
		[ImGuiRegister("REG_SPR_BASE", 0x004)]
		[ImGuiBitDescription("Sprite table base address", 0, 5)]
		[ImGuiFormat("X4", 9)]
		public override int SprBase => sprBase;
		[ImGuiRegister("REG_MAP_BASE", 0x007)]
		[ImGuiBitDescription("SCR1 base address", 0, 3)]
		[ImGuiFormat("X4", 11)]
		public override int Scr1Base => scr1Base;
		[ImGuiRegister("REG_MAP_BASE", 0x007)]
		[ImGuiBitDescription("SCR2 base address", 4, 7)]
		[ImGuiFormat("X4", 11)]
		public override int Scr2Base => scr2Base;
		[ImGuiRegister("REG_LCD_CTRL", 0x014)]
		[ImGuiBitDescription("LCD contrast setting; high contrast?", 1)]
		public bool LcdContrastHigh => lcdContrastHigh;
		[ImGuiRegister("REG_DISP_MODE", 0x060)]
		[ImGuiBitDescription("Display color mode; is color?", 6)]
		public bool DisplayColorFlagSet => displayColorFlagSet;
		[ImGuiRegister("REG_DISP_MODE", 0x060)]
		[ImGuiBitDescription("Tile bits-per-pixel; is 4bpp?", 7)]
		public bool Display4bppFlagSet => display4bppFlagSet;
	}
}

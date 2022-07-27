using StoicGoose.Common.Attributes;
using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Display
{
	public sealed class SphinxDisplayController : DisplayControllerCommon
	{
		// TODO: reimplement high contrast mode; also, get a WSC, figure out how it's supposed to look?

		/* REG_BACK_COLOR */
		byte backColorPalette;
		/* REG_LCD_CTRL */
		bool lcdContrastHigh;
		/* REG_DISP_MODE */
		bool displayColorFlagSet, display4bppFlagSet;

		public SphinxDisplayController(IMachine machine) : base(machine) { }

		protected override void ResetRegisters()
		{
			base.ResetRegisters();

			backColorPalette = 0;
			lcdContrastHigh = false;
			displayColorFlagSet = display4bppFlagSet = false;
		}

		protected override void RenderSleep(int y, int x)
		{
			DisplayUtilities.CopyPixel((0, 0, 0), outputFramebuffer, x, y, HorizontalDisp);
		}

		protected override void RenderBackColor(int y, int x)
		{
			if (displayColorFlagSet || display4bppFlagSet)
				DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel(DisplayUtilities.ReadColor(machine, backColorPalette, backColorIndex)), outputFramebuffer, x, y, HorizontalDisp);
			else
				DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[backColorIndex & 0b0111])), outputFramebuffer, x, y, HorizontalDisp);
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

			var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7), displayPackedFormatSet, displayColorFlagSet, display4bppFlagSet);
			if (pixelColor != 0 || (!(displayColorFlagSet || display4bppFlagSet) && pixelColor == 0 && !IsBitSet(tilePal, 2)))
			{
				if (displayColorFlagSet || display4bppFlagSet)
					DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel(DisplayUtilities.ReadColor(machine, tilePal, pixelColor)), outputFramebuffer, x, y, HorizontalDisp);
				else
					DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
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

			var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7), displayPackedFormatSet, displayColorFlagSet, display4bppFlagSet);
			if (pixelColor != 0 || (!(displayColorFlagSet || display4bppFlagSet) && pixelColor == 0 && !IsBitSet(tilePal, 2)))
			{
				if (!scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					isUsedBySCR2[(y * HorizontalDisp) + x] = true;

					if (displayColorFlagSet || display4bppFlagSet)
						DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel(DisplayUtilities.ReadColor(machine, tilePal, pixelColor)), outputFramebuffer, x, y, HorizontalDisp);
					else
						DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
				}
			}
		}

		protected override void RenderSprites(int y, int x)
		{
			if (!sprEnable) return;

			if (x == 0)
			{
				activeSpriteCountOnLine = 0;
				for (var i = 0; i < spriteCountNextFrame; i++)
				{
					var spriteY = (spriteData[i] >> 16) & 0xFF;
					if ((byte)(y - spriteY) <= 7 && activeSpriteCountOnLine < maxSpritesPerLine)
						activeSpritesOnLine[activeSpriteCountOnLine++] = spriteData[i];
				}
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

					var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, (byte)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (byte)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)), displayPackedFormatSet, displayColorFlagSet, display4bppFlagSet);
					if ((pixelColor != 0 || (!(displayColorFlagSet || display4bppFlagSet) && pixelColor == 0 && !IsBitSet(tilePal, 2))) && (!isUsedBySCR2[(y * HorizontalDisp) + x] || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
						{
							if (displayColorFlagSet || display4bppFlagSet)
								DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel(DisplayUtilities.ReadColor(machine, tilePal, pixelColor)), outputFramebuffer, x, y, HorizontalDisp);
							else
								DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
						}
					}
				}
			}
		}

		public override byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
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
					retVal = base.ReadPort(port);
					break;
			}

			return retVal;
		}

		public override void WritePort(ushort port, byte value)
		{
			switch (port)
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
					base.WritePort(port, value);
					break;
			}
		}

		[Port("REG_BACK_COLOR", 0x001)]
		[BitDescription("Background color index", 0, 3)]
		public override byte BackColorIndex => backColorIndex;
		[Port("REG_BACK_COLOR", 0x001)]
		[BitDescription("Background color palette", 4, 7)]
		public byte BackColorPalette => backColorPalette;
		[Port("REG_SPR_BASE", 0x004)]
		[BitDescription("Sprite table base address", 0, 5)]
		[Format("X4", 9)]
		public override int SprBase => sprBase;
		[Port("REG_MAP_BASE", 0x007)]
		[BitDescription("SCR1 base address", 0, 3)]
		[Format("X4", 11)]
		public override int Scr1Base => scr1Base;
		[Port("REG_MAP_BASE", 0x007)]
		[BitDescription("SCR2 base address", 4, 7)]
		[Format("X4", 11)]
		public override int Scr2Base => scr2Base;
		[Port("REG_LCD_CTRL", 0x014)]
		[BitDescription("LCD contrast setting; high contrast?", 1)]
		public bool LcdContrastHigh => lcdContrastHigh;
		[Port("REG_DISP_MODE", 0x060)]
		[BitDescription("Display color mode; is color?", 6)]
		public bool DisplayColorFlagSet => displayColorFlagSet;
		[Port("REG_DISP_MODE", 0x060)]
		[BitDescription("Tile bits-per-pixel; is 4bpp?", 7)]
		public bool Display4bppFlagSet => display4bppFlagSet;
	}
}

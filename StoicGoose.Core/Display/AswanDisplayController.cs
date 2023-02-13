using StoicGoose.Core.Interfaces;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Display
{
	public sealed class AswanDisplayController : DisplayControllerCommon
	{
		public AswanDisplayController(IMachine machine) : base(machine) { }

		protected override void RenderSleep(int y, int x)
		{
			DisplayUtilities.CopyPixel((255, 255, 255), outputFramebuffer, x, y, HorizontalDisp);
		}

		protected override void RenderBackColor(int y, int x)
		{
			DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[backColorIndex & 0b0111])), outputFramebuffer, x, y, HorizontalDisp);
		}

		protected override void RenderSCR1(int y, int x)
		{
			if (!scr1Enable) return;

			var scrollX = (x + scr1ScrollX) & 0xFF;
			var scrollY = (y + scr1ScrollY) & 0xFF;

			var mapOffset = (uint)((scr1Base << 11) | ((scrollY >> 3) << 6) | ((scrollX >> 3) << 1));
			var attribs = ReadMemory16(mapOffset);
			var tileNum = (ushort)(attribs & 0x01FF);
			var tilePal = (byte)((attribs >> 9) & 0b1111);

			var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7), false, false, false);

			var isOpaque = !IsBitSet(tilePal, 2) || pixelColor != 0;
			if (!isOpaque) return;

			DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
		}

		protected override void RenderSCR2(int y, int x)
		{
			if (!scr2Enable) return;

			var scrollX = (x + scr2ScrollX) & 0xFF;
			var scrollY = (y + scr2ScrollY) & 0xFF;

			var mapOffset = (uint)((scr2Base << 11) | ((scrollY >> 3) << 6) | ((scrollX >> 3) << 1));
			var attribs = ReadMemory16(mapOffset);
			var tileNum = (ushort)(attribs & 0x01FF);
			var tilePal = (byte)((attribs >> 9) & 0b1111);

			var isVisible = !scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x))));
			if (!isVisible) return;

			var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7), false, false, false);

			var isOpaque = !IsBitSet(tilePal, 2) || pixelColor != 0;
			if (!isOpaque) return;

			isUsedBySCR2[(y * HorizontalDisp) + x] = true;

			DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
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
					var tilePal = (byte)((activeSprite >> 9) & 0b111);
					var priorityAboveSCR2 = ((activeSprite >> 13) & 0b1) == 0b1;
					var spriteY = (activeSprite >> 16) & 0xFF;

					var isVisible = !isUsedBySCR2[(y * HorizontalDisp) + x] || priorityAboveSCR2;
					if (!isVisible) continue;

					var pixelColor = DisplayUtilities.ReadPixel(machine, tileNum, (byte)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (byte)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)), false, false, false);

					var isOpaque = !IsBitSet(tilePal, 2) || pixelColor != 0;
					if (!isOpaque) continue;

					tilePal += 8;

					DisplayUtilities.CopyPixel(DisplayUtilities.GeneratePixel((byte)(15 - palMonoPools[palMonoData[tilePal][pixelColor & 0b11]])), outputFramebuffer, x, y, HorizontalDisp);
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
					retVal |= (byte)(backColorIndex & 0b111);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					retVal |= (byte)(sprBase & 0b11111);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					retVal |= (byte)((scr1Base & 0b111) << 0);
					retVal |= (byte)((scr2Base & 0b111) << 4);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					ChangeBit(ref retVal, 0, lcdActive);
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
					backColorIndex = (byte)(value & 0b111);
					break;

				case 0x04:
					/* REG_SPR_BASE */
					sprBase = (byte)(value & 0b11111);
					break;

				case 0x07:
					/* REG_MAP_BASE */
					scr1Base = (byte)((value >> 0) & 0b111);
					scr2Base = (byte)((value >> 4) & 0b111);
					break;

				case 0x14:
					/* REG_LCD_CTRL */
					lcdActive = IsBitSet(value, 0);
					break;

				default:
					/* Fall through to common */
					base.WritePort(port, value);
					break;
			}
		}
	}
}

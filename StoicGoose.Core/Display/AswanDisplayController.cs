using static StoicGoose.Common.Utilities;

namespace StoicGoose.Core.Display
{
	public sealed class AswanDisplayController : DisplayControllerCommon
	{
		public AswanDisplayController(MemoryReadDelegate memoryRead) : base(memoryRead) { }

		protected override void RenderSleep(int y, int x)
		{
			WriteToFramebuffer(y, x, 255, 255, 255);
		}

		protected override void RenderBackColor(int y, int x)
		{
			WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[backColorIndex & 0b0111]));
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

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (color != 0 || (color == 0 && !IsBitSet(tilePal, 2)))
				WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal][color & 0b11]]));
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

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (color != 0 || (color == 0 && !IsBitSet(tilePal, 2)))
			{
				if (!scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					isUsedBySCR2[(y * HorizontalDisp) + x] = true;
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

					if ((color != 0 || (color == 0 && !IsBitSet(tilePal, 2))) && (!isUsedBySCR2[(y * HorizontalDisp) + x] || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
							WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal][color & 0b11]]));
					}
				}
			}
		}

		protected override byte GetPixelColor(ushort tile, int y, int x)
		{
			if (!displayPackedFormatSet)
			{
				var data = ReadMemory16((uint)(0x2000 + (tile << 4) + ((y % 8) << 1)));
				return (byte)((((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b11);
			}
			else
			{
				var data = ReadMemory8((uint)(0x2000 + (tile << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
				return (byte)((data >> 6 - (((x % 8) & 0b11) << 1)) & 0b11);
			}
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
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

				case 0x60:
					/* REG_DISP_MODE */
					ChangeBit(ref retVal, 5, displayPackedFormatSet);
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

				case 0x60:
					/* REG_DISP_MODE */
					displayPackedFormatSet = IsBitSet(value, 5);
					break;

				default:
					/* Fall through to common */
					base.WriteRegister(register, value);
					break;
			}
		}
	}
}

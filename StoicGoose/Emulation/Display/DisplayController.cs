using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StoicGoose.Emulation.Machines;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Display
{
	public sealed class DisplayController
	{
		public const int HorizontalDisp = 224;
		public const int HorizontalBlank = 32;
		public const int HorizontalTotal = HorizontalDisp + HorizontalBlank;
		public const double HorizontalClock = WonderSwan.CpuClock / HorizontalTotal;

		public const int VerticalDisp = 144;
		public const int VerticalBlank = 15;
		public const int VerticalTotal = VerticalDisp + VerticalBlank;
		public const double VerticalClock = 12000 / (double)VerticalTotal;

		public const int ScreenWidth = HorizontalDisp;
		public const int ScreenHeight = VerticalDisp;

		const int maxSpriteCount = 128;

		public event EventHandler<RenderScreenEventArgs> RenderScreen;
		public void OnRenderScreen(RenderScreenEventArgs e) { RenderScreen?.Invoke(this, e); }

		[Flags]
		public enum DisplayInterrupts
		{
			None = 0,
			LineCompare = 1 << 0,
			VBlankTimer = 1 << 1,
			VBlank = 1 << 2,
			HBlankTimer = 1 << 3
		}

		readonly uint[] spriteData = new uint[maxSpriteCount];
		readonly uint[] spriteDataNextFrame = new uint[maxSpriteCount];
		readonly List<uint> activeSpritesOnLine = new List<uint>();

		const byte screenUsageEmpty = 0;
		const byte screenUsageSCR1 = 1 << 0;
		const byte screenUsageSCR2 = 1 << 1;
		const byte screenUsageSPR = 1 << 2;
		readonly byte[] screenUsage;

		int cycleCount;
		readonly int clockCyclesPerLine;
		readonly byte[] outputFramebuffer;

		public delegate byte MemoryReadDelegate(uint address);
		readonly MemoryReadDelegate memoryReadDelegate;

		/* REG_DISP_CTRL */
		bool scr1Enable, scr2Enable, sprEnable, sprWindowEnable, scr2WindowDisplayOutside, scr2WindowEnable;
		/* REG_BACK_COLOR */
		byte backColor;
		/* REG_LINE_xxx */
		int lineCurrent, lineCompare;
		/* REG_SPR_xxx */
		int sprBase, sprFirst, sprCount;
		/* REG_MAP_BASE */
		int scr1Base, scr2Base;
		/* REG_SCR2_WIN_xx */
		int scr2WinX0, scr2WinY0, scr2WinX1, scr2WinY1;
		/* REG_SPR_WIN_xx */
		int sprWinX0, sprWinY0, sprWinX1, sprWinY1;
		/* REG_SCR1_xx */
		int scr1ScrollX, scr1ScrollY;
		/* REG_SCR2_xx */
		int scr2ScrollX, scr2ScrollY;
		/* REG_LCD_xxx */
		bool lcdActive;
		bool iconSleep, iconVertical, iconHorizontal, iconAux1, iconAux2, iconAux3;
		int vtotal, vsync;
		/* REG_PALMONO_POOL_x */
		readonly byte[] palMonoPools = new byte[8];
		/* REG_PALMONO_x */
		readonly byte[,] palMonoData = new byte[16, 4];
		/* REG_DISP_MODE */
		bool isDisplayFormatPacked;
		/* REG_xTMR_xxx */
		readonly DisplayTimer hBlankTimer = new DisplayTimer(), vBlankTimer = new DisplayTimer();

		public DisplayController(MemoryReadDelegate memoryRead)
		{
			memoryReadDelegate = memoryRead;

			screenUsage = new byte[HorizontalDisp * VerticalDisp];

			clockCyclesPerLine = HorizontalTotal;
			outputFramebuffer = new byte[ScreenWidth * ScreenHeight * 4];
		}

		public void Reset()
		{
			cycleCount = 0;

			ResetScreenUsageMap();
			ResetFramebuffer();

			for (var i = 0; i < maxSpriteCount; i++)
				spriteData[i] = spriteDataNextFrame[i] = 0;

			ResetRegisters();
		}

		private void ResetScreenUsageMap()
		{
			for (var i = 0; i < screenUsage.Length; i++)
				screenUsage[i] = screenUsageEmpty;
		}

		private void ResetFramebuffer()
		{
			for (var i = 0; i < outputFramebuffer.Length; i += 4)
			{
				outputFramebuffer[i + 0] = 255; //B
				outputFramebuffer[i + 1] = 255; //G
				outputFramebuffer[i + 2] = 255; //R
				outputFramebuffer[i + 3] = 255; //A
			}
		}

		private void ResetRegisters()
		{
			scr1Enable = scr2Enable = sprEnable = sprWindowEnable = scr2WindowDisplayOutside = scr2WindowEnable = false;
			backColor = 0;
			lineCurrent = lineCompare = 0;
			sprBase = sprFirst = sprCount = 0;
			scr1Base = scr2Base = 0;
			scr2WinX0 = scr2WinY0 = scr2WinX1 = scr2WinY1 = 0;
			sprWinX0 = sprWinY0 = sprWinX1 = sprWinY1 = 0;
			scr1ScrollX = scr1ScrollY = 0;
			scr2ScrollX = scr2ScrollY = 0;
			lcdActive = true;   //Final Lap 2000 depends on bootstrap doing this, otherwise LCD stays off?
			iconSleep = iconVertical = iconHorizontal = iconAux1 = iconAux2 = iconAux3 = false;
			vtotal = VerticalTotal - 1;
			vsync = VerticalTotal - 4; //Full usage/meaning unknown, so we're ignoring it for now
			for (var i = 0; i < palMonoPools.Length; i++) palMonoPools[i] = 0;
			for (var i = 0; i < palMonoData.GetLength(0); i++) for (var j = 0; j < palMonoData.GetLength(1); j++) palMonoData[i, j] = 0;
			isDisplayFormatPacked = false;
			hBlankTimer.Reset();
			vBlankTimer.Reset();
		}

		public DisplayInterrupts Step(int clockCyclesInStep)
		{
			var interrupt = DisplayInterrupts.None;

			for (var i = 0; i < clockCyclesInStep; i++)
			{
				if (cycleCount == 0)
				{
					// V-blank interrupt
					if (lineCurrent == VerticalDisp)
					{
						interrupt |= DisplayInterrupts.VBlank;

						// V-timer interrupt
						if (vBlankTimer.Step())
							interrupt |= DisplayInterrupts.VBlankTimer;
					}

					// line compare interrupt
					if (lineCurrent == lineCompare)
						interrupt |= DisplayInterrupts.LineCompare;

					// sprite fetch
					if (lineCurrent == VerticalDisp - 2)
					{
						for (var j = 0; j < spriteDataNextFrame.Length; j++) spriteDataNextFrame[j] = 0;
						for (var j = sprFirst; j < sprFirst + Math.Min(maxSpriteCount, sprCount); j++)
						{
							var k = (uint)((sprBase << 9) + (j << 2));
							spriteDataNextFrame[j] = (uint)(memoryReadDelegate(k + 3) << 24 | memoryReadDelegate(k + 2) << 16 | memoryReadDelegate(k + 1) << 8 | memoryReadDelegate(k + 0));
						}
					}
				}

				// render pixel
				RenderPixel(lineCurrent, cycleCount++);

				if (cycleCount == HorizontalDisp)
				{
					// H-timer interrupt
					if (hBlankTimer.Step())
						interrupt |= DisplayInterrupts.HBlankTimer;
				}

				if (cycleCount == clockCyclesPerLine)
				{
					// go to next scanline
					lineCurrent++;

					// is frame finished
					if (lineCurrent >= Math.Max(VerticalDisp, vtotal) + 1)
					{
						lineCurrent = 0;

						// copy sprite data for next frame
						for (var j = 0; j < maxSpriteCount; j++)
							spriteData[j] = spriteDataNextFrame[j];

						OnRenderScreen(new RenderScreenEventArgs(outputFramebuffer.Clone() as byte[]));
						ResetScreenUsageMap();
					}

					// end of scanline
					cycleCount = 0;
				}
			}

			return interrupt;
		}

		private void RenderPixel(int y, int x)
		{
			if (y < 0 || y >= VerticalDisp || x < 0 || x >= HorizontalDisp) return;

			if (lcdActive)
			{
				WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[backColor & 0b0111]));

				RenderSCR1(y, x);
				RenderSCR2(y, x);
				RenderSprites(y, x);
			}
			else
			{
				// LCD sleeping
				WriteToFramebuffer(y, x, 255, 255, 255);
			}
		}

		private void RenderSCR1(int y, int x)
		{
			if (!scr1Enable) return;

			var scrollX = (x + scr1ScrollX) & 0xFF;
			var scrollY = (y + scr1ScrollY) & 0xFF;

			var mapOffset = GetMapOffset(scr1Base, scrollY, scrollX);
			var attribs = (ushort)(memoryReadDelegate(mapOffset + 1) << 8 | memoryReadDelegate(mapOffset));
			var tileNum = (ushort)(attribs & 0x01FF);
			var tilePal = (attribs >> 9) & 0b1111;

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (IsColorOpaque((byte)tilePal, color))
			{
				SetScreenUsageFlag(y, x, screenUsageSCR1);
				WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));

				if (GlobalVariables.EnableRenderSCR1DebugColors) WriteToFramebuffer(y, x, (byte)((tileNum & 0xf) << 4), 0, 0);
			}
		}

		private void RenderSCR2(int y, int x)
		{
			if (!scr2Enable) return;

			var scrollX = (x + scr2ScrollX) & 0xFF;
			var scrollY = (y + scr2ScrollY) & 0xFF;

			var mapOffset = GetMapOffset(scr2Base, scrollY, scrollX);
			var attribs = (ushort)(memoryReadDelegate(mapOffset + 1) << 8 | memoryReadDelegate(mapOffset));
			var tileNum = (ushort)(attribs & 0x01FF);
			var tilePal = (attribs >> 9) & 0b1111;

			var color = GetPixelColor(tileNum, scrollY ^ (((attribs >> 15) & 0b1) * 7), scrollX ^ (((attribs >> 14) & 0b1) * 7));

			if (IsColorOpaque((byte)tilePal, color))
			{
				if (!scr2WindowEnable || (scr2WindowEnable && ((!scr2WindowDisplayOutside && IsInsideSCR2Window(y, x)) || (scr2WindowDisplayOutside && IsOutsideSCR2Window(y, x)))))
				{
					SetScreenUsageFlag(y, x, screenUsageSCR2);
					WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));

					if (GlobalVariables.EnableRenderSCR2DebugColors) WriteToFramebuffer(y, x, 0, (byte)((tileNum & 0xf) << 4), 0);
				}
			}
		}

		private void RenderSprites(int y, int x)
		{
			if (!sprEnable) return;

			activeSpritesOnLine.Clear();

			for (var i = sprFirst + Math.Min(maxSpriteCount, sprCount) - 1; i >= sprFirst; i--)
			{
				if (spriteData[i] == 0) continue;   //HACK: helps prevent garbage sprites in ex. pocket fighter, but prob only b/c inaccurate timing?

				var spriteY = (int)((spriteData[i] >> 16) & 0xFF);
				var spriteX = (int)((spriteData[i] >> 24) & 0xFF);

				if (y >= spriteY && y < spriteY + 8 && x >= spriteX && x < spriteX + 8 && activeSpritesOnLine.Count < 32)
					activeSpritesOnLine.Add(spriteData[i]);
			}

			foreach (var activeSprite in activeSpritesOnLine)
			{
				var tileNum = (ushort)(activeSprite & 0x01FF);
				var tilePal = ((activeSprite >> 9) & 0b111) + 8;
				var windowDisplayOutside = ((activeSprite >> 12) & 0b1) == 0b1;
				var priorityAboveSCR2 = ((activeSprite >> 13) & 0b1) == 0b1;

				var spriteY = (int)((activeSprite >> 16) & 0xFF);
				var spriteX = (int)((activeSprite >> 24) & 0xFF);

				if (x < 0 || x >= HorizontalDisp) continue;

				byte color = GetPixelColor(tileNum, (int)((y - spriteY) ^ (((activeSprite >> 15) & 0b1) * 7)), (int)((x - spriteX) ^ (((activeSprite >> 14) & 0b1) * 7)));

				if (!sprWindowEnable || (sprWindowEnable && (windowDisplayOutside != IsInsideSPRWindow(y, x))))
				{
					if (IsColorOpaque((byte)tilePal, color) && (!IsScreenUsageFlagSet(y, x, screenUsageSCR2) || priorityAboveSCR2))
					{
						if (y >= 0 && y < VerticalDisp && x >= 0 && x < HorizontalDisp)
						{
							SetScreenUsageFlag(y, x, screenUsageSPR);
							WriteToFramebuffer(y, x, (byte)(15 - palMonoPools[palMonoData[tilePal, color & 0b11]]));

							if (GlobalVariables.EnableRenderSPRDebugColors) WriteToFramebuffer(y, x, 0, 0, (byte)((tileNum & 0xf) << 4));
						}
					}
				}
			}
		}

		private uint GetMapOffset(int scrBase, int y, int x)
		{
			var mapOffset = (ushort)(scrBase << 11);
			mapOffset |= (ushort)((y >> 3) << 6);
			mapOffset |= (ushort)((x >> 3) << 1);
			return mapOffset;
		}

		private byte GetPixelColor(ushort tile, int y, int x)
		{
			byte color;

			if (!isDisplayFormatPacked)
			{
				var address = (uint)(0x2000 + (tile << 4) + ((y % 8) << 1));
				var data = (ushort)(memoryReadDelegate(address + 1) << 8 | memoryReadDelegate(address));
				var color0 = (data >> 7 - (x % 8)) & 0b1;
				var color1 = (data >> 15 - (x % 8)) & 0b1;
				color = (byte)(color1 << 1 | color0);
			}
			else
			{
				var data = memoryReadDelegate((ushort)(0x2000 + (tile << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
				color = (byte)(data >> 6 - (((x % 8) & 0b11) << 1));
			}

			return color;
		}

		private bool IsColorOpaque(byte palette, byte color)
		{
			return color != 0 || (color == 0 && !IsBitSet(palette, 2));
		}

		private void ValidateWindowCoordinates(ref int x0, ref int x1, ref int y0, ref int y1)
		{
			// Thank you for this fix, for the encouragement and hints and advice, for just having been there... Thank you for everything, Near.
			// https://forum.fobby.net/index.php?t=msg&goto=6085

			if (x0 > x1) Swap(ref x0, ref x1);
			if (y0 > y1) Swap(ref y0, ref y1);
		}

		private bool IsInsideSCR2Window(int y, int x)
		{
			var x0 = scr2WinX0;
			var x1 = scr2WinX1;
			var y0 = scr2WinY0;
			var y1 = scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return ((x >= x0 && x <= x1) || (x >= x1 && x <= x0)) &&
				((y >= y0 && y <= y1) || (y >= y1 && y <= y0));
		}

		private bool IsOutsideSCR2Window(int y, int x)
		{
			var x0 = scr2WinX0;
			var x1 = scr2WinX1;
			var y0 = scr2WinY0;
			var y1 = scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return x < x0 || x > x1 || y < y0 || y > y1;
		}

		private bool IsInsideSPRWindow(int y, int x)
		{
			var x0 = sprWinX0;
			var x1 = sprWinX1;
			var y0 = sprWinY0;
			var y1 = sprWinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return ((x >= x0 && x <= x1) || (x >= x1 && x <= x0)) &&
				((y >= y0 && y <= y1) || (y >= y1 && y <= y0));
		}

		private byte GetScreenUsageFlag(int y, int x)
		{
			return screenUsage[(y * HorizontalDisp) + (x % HorizontalDisp)];
		}

		private bool IsScreenUsageFlagSet(int y, int x, byte flag)
		{
			return (GetScreenUsageFlag(y, x) & flag) == flag;
		}

		private void SetScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * HorizontalDisp) + (x % HorizontalDisp)] |= flag;
		}

		private void WriteToFramebuffer(int y, int x, byte pixel)
		{
			byte r, g, b;
			r = g = b = (byte)((pixel << 4) | pixel);
			WriteToFramebuffer(y, x, r, g, b);
		}

		private void WriteToFramebuffer(int y, int x, byte r, byte g, byte b)
		{
			var outputAddress = ((y * HorizontalDisp) + x) * 4;
			outputFramebuffer[outputAddress + 0] = b;
			outputFramebuffer[outputAddress + 1] = g;
			outputFramebuffer[outputAddress + 2] = r;
			outputFramebuffer[outputAddress + 3] = 255;
		}

		public List<string> GetActiveIcons()
		{
			var list = new List<string>();
			if (iconSleep) list.Add("sleep");
			if (iconVertical) list.Add("vertical");
			if (iconHorizontal) list.Add("horizontal");
			if (iconAux1) list.Add("aux1");
			if (iconAux2) list.Add("aux2");
			if (iconAux3) list.Add("aux3");
			return list;
		}

		public byte ReadRegister(ushort register)
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
					retVal |= (byte)(backColor & 0b111);
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
					ChangeBit(ref retVal, 5, isDisplayFormatPacked);
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

		public void WriteRegister(ushort register, byte value)
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
					backColor = (byte)(value & 0b111);
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
					isDisplayFormatPacked = IsBitSet(value, 5);
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

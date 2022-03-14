using System;
using System.Collections.Generic;

using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Display
{
	public abstract class DisplayControllerCommon : IComponent
	{
		public const int HorizontalDisp = 224;
		public const int HorizontalBlank = 32;
		public const int HorizontalTotal = HorizontalDisp + HorizontalBlank;
		public abstract double HorizontalClock { get; }

		public const int VerticalDisp = 144;
		public const int VerticalBlank = 15;
		public const int VerticalTotal = VerticalDisp + VerticalBlank;
		public const double VerticalClock = 12000 / (double)VerticalTotal;

		public const int ScreenWidth = HorizontalDisp;
		public const int ScreenHeight = VerticalDisp;

		protected const int maxSpriteCount = 128;

		public event EventHandler<UpdateScreenEventArgs> UpdateScreen;
		public void OnUpdateScreen(UpdateScreenEventArgs e) { UpdateScreen?.Invoke(this, e); }

		[Flags]
		public enum DisplayInterrupts
		{
			None = 0,
			LineCompare = 1 << 0,
			VBlankTimer = 1 << 1,
			VBlank = 1 << 2,
			HBlankTimer = 1 << 3
		}

		protected enum TileAttribScreens { SCR1, SCR2 }

		protected readonly uint[] spriteData = new uint[maxSpriteCount];
		protected readonly uint[] spriteDataNextFrame = new uint[maxSpriteCount];
		protected readonly List<uint> activeSpritesOnLine = new();

		protected const byte screenUsageEmpty = 0;
		protected const byte screenUsageSCR1 = 1 << 0;
		protected const byte screenUsageSCR2 = 1 << 1;
		protected const byte screenUsageSPR = 1 << 2;
		protected readonly byte[] screenUsage;

		protected int cycleCount;
		protected readonly int clockCyclesPerLine;
		protected readonly byte[] outputFramebuffer;

		public delegate byte MemoryReadDelegate(uint address);
		protected readonly MemoryReadDelegate memoryReadDelegate;

		/* REG_DISP_CTRL */
		protected bool scr1Enable, scr2Enable, sprEnable, sprWindowEnable, scr2WindowDisplayOutside, scr2WindowEnable;
		/* REG_BACK_COLOR */
		protected byte backColorIndex;
		/* REG_LINE_xxx */
		protected int lineCurrent, lineCompare;
		/* REG_SPR_xxx */
		protected int sprBase, sprFirst, sprCount;
		/* REG_MAP_BASE */
		protected int scr1Base, scr2Base;
		/* REG_SCR2_WIN_xx */
		protected int scr2WinX0, scr2WinY0, scr2WinX1, scr2WinY1;
		/* REG_SPR_WIN_xx */
		protected int sprWinX0, sprWinY0, sprWinX1, sprWinY1;
		/* REG_SCR1_xx */
		protected int scr1ScrollX, scr1ScrollY;
		/* REG_SCR2_xx */
		protected int scr2ScrollX, scr2ScrollY;
		/* REG_LCD_xxx */
		protected bool lcdActive;
		protected bool iconSleep, iconVertical, iconHorizontal, iconAux1, iconAux2, iconAux3;
		protected int vtotal, vsync;
		/* REG_PALMONO_POOL_x */
		protected readonly byte[] palMonoPools = new byte[8];
		/* REG_PALMONO_x */
		protected readonly byte[,] palMonoData = new byte[16, 4];
		/* REG_DISP_MODE */
		protected bool displayPackedFormatSet;
		/* REG_xTMR_xxx */
		protected readonly DisplayTimer hBlankTimer = new(), vBlankTimer = new();

		protected bool isPackedMode => displayPackedFormatSet;
		protected bool isPlanarMode => !displayPackedFormatSet;

		public DisplayControllerCommon(MemoryReadDelegate memoryRead)
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

		protected void ResetScreenUsageMap()
		{
			for (var i = 0; i < screenUsage.Length; i++)
				screenUsage[i] = screenUsageEmpty;
		}

		protected void ResetFramebuffer()
		{
			for (var i = 0; i < outputFramebuffer.Length; i += 4)
			{
				outputFramebuffer[i + 0] = 255; //B
				outputFramebuffer[i + 1] = 255; //G
				outputFramebuffer[i + 2] = 255; //R
				outputFramebuffer[i + 3] = 255; //A
			}
		}

		protected virtual void ResetRegisters()
		{
			scr1Enable = scr2Enable = sprEnable = sprWindowEnable = scr2WindowDisplayOutside = scr2WindowEnable = false;
			backColorIndex = 0;
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
			displayPackedFormatSet = false;
			hBlankTimer.Reset();
			vBlankTimer.Reset();
		}

		public void Shutdown()
		{
			//
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
					// line compare interrupt
					if (lineCurrent == lineCompare)
						interrupt |= DisplayInterrupts.LineCompare;

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

						OnUpdateScreen(new UpdateScreenEventArgs(outputFramebuffer.Clone() as byte[]));
						ResetScreenUsageMap();
					}

					// end of scanline
					cycleCount = 0;
				}
			}

			return interrupt;
		}

		protected void RenderPixel(int y, int x)
		{
			if (y < 0 || y >= VerticalDisp || x < 0 || x >= HorizontalDisp) return;

			if (lcdActive)
			{
				RenderBackColor(y, x);
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

		protected abstract void RenderBackColor(int y, int x);
		protected abstract void RenderSCR1(int y, int x);
		protected abstract void RenderSCR2(int y, int x);
		protected abstract void RenderSprites(int y, int x);

		protected uint GetMapOffset(int scrBase, int y, int x)
		{
			var mapOffset = (ushort)(scrBase << 11);
			mapOffset |= (ushort)((y >> 3) << 6);
			mapOffset |= (ushort)((x >> 3) << 1);
			return mapOffset;
		}

		protected ushort GetTileAttribs(int scrBase, int scrollY, int scrollX)
		{
			var mapOffset = GetMapOffset(scrBase, scrollY, scrollX);
			return (ushort)(memoryReadDelegate(mapOffset + 1) << 8 | memoryReadDelegate(mapOffset));
		}

		protected abstract ushort GetTileNumber(ushort attribs);

		protected byte GetTilePalette(ushort attribs) => (byte)((attribs >> 9) & 0b1111);
		protected int GetTileVerticalFlip(ushort attribs) => (attribs >> 15) & 0b1;
		protected int GetTileHorizontalFlip(ushort attribs) => (attribs >> 14) & 0b1;

		protected abstract byte GetPixelColor(ushort tile, int y, int x);
		protected abstract bool IsColorOpaque(byte palette, byte color);

		protected void ValidateWindowCoordinates(ref int x0, ref int x1, ref int y0, ref int y1)
		{
			// Thank you for this fix, for the encouragement and hints and advice, for just having been there... Thank you for everything, Near.
			// https://forum.fobby.net/index.php?t=msg&goto=6085

			if (x0 > x1) Swap(ref x0, ref x1);
			if (y0 > y1) Swap(ref y0, ref y1);
		}

		protected bool IsInsideSCR2Window(int y, int x)
		{
			var x0 = scr2WinX0;
			var x1 = scr2WinX1;
			var y0 = scr2WinY0;
			var y1 = scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return ((x >= x0 && x <= x1) || (x >= x1 && x <= x0)) &&
				((y >= y0 && y <= y1) || (y >= y1 && y <= y0));
		}

		protected bool IsOutsideSCR2Window(int y, int x)
		{
			var x0 = scr2WinX0;
			var x1 = scr2WinX1;
			var y0 = scr2WinY0;
			var y1 = scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return x < x0 || x > x1 || y < y0 || y > y1;
		}

		protected bool IsInsideSPRWindow(int y, int x)
		{
			var x0 = sprWinX0;
			var x1 = sprWinX1;
			var y0 = sprWinY0;
			var y1 = sprWinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return ((x >= x0 && x <= x1) || (x >= x1 && x <= x0)) &&
				((y >= y0 && y <= y1) || (y >= y1 && y <= y0));
		}

		protected byte GetScreenUsageFlag(int y, int x)
		{
			return screenUsage[(y * HorizontalDisp) + (x % HorizontalDisp)];
		}

		protected bool IsScreenUsageFlagSet(int y, int x, byte flag)
		{
			return (GetScreenUsageFlag(y, x) & flag) == flag;
		}

		protected void SetScreenUsageFlag(int y, int x, byte flag)
		{
			screenUsage[(y * HorizontalDisp) + (x % HorizontalDisp)] |= flag;
		}

		protected void WriteToFramebuffer(int y, int x, byte pixel)
		{
			byte r, g, b;
			r = g = b = (byte)((pixel << 4) | pixel);
			WriteToFramebuffer(y, x, r, g, b);
		}

		protected void WriteToFramebuffer(int y, int x, byte r, byte g, byte b)
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

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);
	}
}

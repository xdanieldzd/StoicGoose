using System;

using StoicGoose.Interface.Attributes;
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
		protected const int maxSpritesPerLine = 32;

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
		protected readonly uint[] activeSpritesOnLine = new uint[maxSpritesPerLine];

		protected const byte screenUsageEmpty = 0;
		protected const byte screenUsageSCR1 = 1 << 0;
		protected const byte screenUsageSCR2 = 1 << 1;
		protected const byte screenUsageSPR = 1 << 2;
		protected readonly byte[] screenUsage = new byte[HorizontalDisp * VerticalDisp];

		protected int spriteCountNextFrame = 0, activeSpriteCountOnLine = 0;

		protected int cycleCount = 0;
		protected readonly int clockCyclesPerLine = HorizontalTotal;
		protected readonly byte[] outputFramebuffer = new byte[ScreenWidth * ScreenHeight * 4];

		protected readonly MemoryReadDelegate memoryReadDelegate = default;

		/* REG_DISP_CTRL */
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SCR1 enable", 0)]
		public bool Scr1Enable { get; protected set; } = false;
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SCR2 enable", 1)]
		public bool Scr2Enable { get; protected set; } = false;
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SPR enable", 2)]
		public bool SprEnable { get; protected set; } = false;
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SPR window enable", 3)]
		public bool SprWindowEnable { get; protected set; } = false;
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SCR2 window mode; display outside?", 4)]
		public bool Scr2WindowDisplayOutside { get; protected set; } = false;
		[ImGuiRegister(0x000, "REG_DISP_CTRL")]
		[ImGuiBitDescription("SCR2 window enable", 5)]
		public bool Scr2WindowEnable { get; protected set; } = false;

		/* REG_BACK_COLOR */
		[ImGuiRegister(0x001, "REG_BACK_COLOR")]
		[ImGuiBitDescription("Background color pool index", 0, 2)]
		public virtual byte BackColorIndex { get; protected set; } = 0;

		/* REG_LINE_xxx */
		[ImGuiRegister(0x002, "REG_LINE_CUR")]
		[ImGuiBitDescription("Current line being drawn")]
		public int LineCurrent { get; protected set; } = 0;
		[ImGuiRegister(0x003, "REG_LINE_CMP")]
		[ImGuiBitDescription("Line compare interrupt line")]
		public int LineCompare { get; protected set; } = 0;

		/* REG_SPR_xxx */
		[ImGuiRegister(0x004, "REG_SPR_BASE")]
		[ImGuiBitDescription("Sprite table base address", 0, 4)]
		[ImGuiFormat("X4", 9)]
		public virtual int SprBase { get; protected set; } = 0;
		[ImGuiRegister(0x005, "REG_SPR_FIRST")]
		[ImGuiBitDescription("First sprite to draw", 0, 6)]
		public int SprFirst { get; protected set; } = 0;
		[ImGuiRegister(0x006, "REG_SPR_COUNT")]
		[ImGuiBitDescription("Number of sprites to draw")]
		public int SprCount { get; protected set; } = 0;

		/* REG_MAP_BASE */
		[ImGuiRegister(0x007, "REG_MAP_BASE")]
		[ImGuiBitDescription("SCR1 base address", 0, 2)]
		[ImGuiFormat("X4", 11)]
		public virtual int Scr1Base { get; protected set; } = 0;
		[ImGuiRegister(0x007, "REG_MAP_BASE")]
		[ImGuiBitDescription("SCR2 base address", 4, 6)]
		[ImGuiFormat("X4", 11)]
		public virtual int Scr2Base { get; protected set; } = 0;

		/* REG_SCR2_WIN_xx */
		[ImGuiRegister(0x008, "REG_SCR2_WIN_X0")]
		[ImGuiBitDescription("Top-left X of SCR2 window")]
		public int Scr2WinX0 { get; protected set; } = 0;
		[ImGuiRegister(0x009, "REG_SCR2_WIN_Y0")]
		[ImGuiBitDescription("Top-left Y of SCR2 window")]
		public int Scr2WinY0 { get; protected set; } = 0;
		[ImGuiRegister(0x00A, "REG_SCR2_WIN_X1")]
		[ImGuiBitDescription("Bottom-right X of SCR2 window")]
		public int Scr2WinX1 { get; protected set; } = 0;
		[ImGuiRegister(0x00B, "REG_SCR2_WIN_Y1")]
		[ImGuiBitDescription("Bottom-right Y of SCR2 window")]
		public int Scr2WinY1 { get; protected set; } = 0;

		/* REG_SPR_WIN_xx */
		[ImGuiRegister(0x00C, "REG_SPR_WIN_X0")]
		[ImGuiBitDescription("Top-left X of SPR window")]
		public int SprWinX0 { get; protected set; } = 0;
		[ImGuiRegister(0x00D, "REG_SPR_WIN_Y0")]
		[ImGuiBitDescription("Top-left Y of SPR window")]
		public int SprWinY0 { get; protected set; } = 0;
		[ImGuiRegister(0x00E, "REG_SPR_WIN_X1")]
		[ImGuiBitDescription("Bottom-right X of SPR window")]
		public int SprWinX1 { get; protected set; } = 0;
		[ImGuiRegister(0x00F, "REG_SPR_WIN_Y1")]
		[ImGuiBitDescription("Bottom-right Y of SPR window")]
		public int SprWinY1 { get; protected set; } = 0;

		/* REG_SCR1_xx */
		[ImGuiRegister(0x010, "REG_SCR1_X")]
		[ImGuiBitDescription("SCR1 X scroll")]
		public int Scr1ScrollX { get; protected set; } = 0;
		[ImGuiRegister(0x011, "REG_SCR1_Y")]
		[ImGuiBitDescription("SCR1 Y scroll")]
		public int Scr1ScrollY { get; protected set; } = 0;

		/* REG_SCR2_xx */
		[ImGuiRegister(0x012, "REG_SCR2_X")]
		[ImGuiBitDescription("SCR2 X scroll")]
		public int Scr2ScrollX { get; protected set; } = 0;
		[ImGuiRegister(0x013, "REG_SCR2_Y")]
		[ImGuiBitDescription("SCR2 Y scroll")]
		public int Scr2ScrollY { get; protected set; } = 0;

		/* REG_LCD_CTRL */
		[ImGuiRegister(0x014, "REG_LCD_CTRL")]
		[ImGuiBitDescription("LCD sleep mode; is LCD active?", 0)]
		public bool LcdActive { get; protected set; } = false;

		/* REG_LCD_ICON */
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Sleep indicator", 0)]
		public bool IconSleep { get; protected set; } = false;
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Vertical orientation indicator", 1)]
		public bool IconVertical { get; protected set; } = false;
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Horizontal orientation indicator", 2)]
		public bool IconHorizontal { get; protected set; } = false;
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Auxiliary 1 (Small circle)", 3)]
		public bool IconAux1 { get; protected set; } = false;
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Auxiliary 2 (Medium circle)", 4)]
		public bool IconAux2 { get; protected set; } = false;
		[ImGuiRegister(0x015, "REG_LCD_ICON")]
		[ImGuiBitDescription("Auxiliary 3 (Big circle)", 5)]
		public bool IconAux3 { get; protected set; } = false;

		/* REG_LCD_VTOTAL */
		[ImGuiRegister(0x016, "REG_LCD_VTOTAL")]
		[ImGuiBitDescription("Display VTOTAL")]
		public int VTotal { get; protected set; } = 0;

		/* REG_LCD_VSYNC */
		[ImGuiRegister(0x017, "REG_LCD_VSYNC")]
		[ImGuiBitDescription("VSYNC line position")]
		public int VSync { get; protected set; } = 0;

		// TODO: reorganize & make public
		/* REG_PALMONO_POOL_x */
		protected readonly byte[] PalMonoPools = new byte[8];
		/* REG_PALMONO_x */
		protected readonly byte[,] PalMonoData = new byte[16, 4];

		/* REG_DISP_MODE */
		[ImGuiRegister(0x060, "REG_DISP_MODE")]
		[ImGuiBitDescription("Sleep indicator", 0)]
		public bool DisplayPackedFormatSet { get; protected set; } = false;

		/* REG_xTMR_xxx */
		protected readonly DisplayTimer HBlankTimer = new(), VBlankTimer = new();

		[ImGuiRegister(0x0A2, "REG_TMR_CTRL")]
		[ImGuiBitDescription("H-blank timer enable", 0)]
		public bool HBlankTimerEnable => HBlankTimer.Enable;
		[ImGuiRegister(0x0A2, "REG_TMR_CTRL")]
		[ImGuiBitDescription("H-blank timer mode; is repeating?", 1)]
		public bool HBlankTimerRepeating => HBlankTimer.Repeating;
		[ImGuiRegister(0x0A2, "REG_TMR_CTRL")]
		[ImGuiBitDescription("V-blank timer enable", 2)]
		public bool VBlankTimerEnable => VBlankTimer.Enable;
		[ImGuiRegister(0x0A2, "REG_TMR_CTRL")]
		[ImGuiBitDescription("V-blank timer mode; is repeating?", 3)]
		public bool VBlankTimerRepeating => VBlankTimer.Repeating;
		// TODO: ImGuiRegister cover multiple registers & timer frequencies/counters

		protected bool isPackedMode => DisplayPackedFormatSet;
		protected bool isPlanarMode => !DisplayPackedFormatSet;

		public int ClockCyclesPerLine => clockCyclesPerLine;

		public DisplayControllerCommon(MemoryReadDelegate memoryRead)
		{
			memoryReadDelegate = memoryRead;
		}

		public void Reset()
		{
			cycleCount = 0;

			ResetScreenUsageMap();
			ResetFramebuffer();

			for (var i = 0; i < maxSpriteCount; i++)
			{
				spriteData[i] = 0;
				spriteDataNextFrame[i] = 0;
			}
			for (var i = 0; i < maxSpritesPerLine; i++)
				activeSpritesOnLine[i] = 0;

			spriteCountNextFrame = 0;
			activeSpriteCountOnLine = 0;

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
			Scr1Enable = Scr2Enable = SprEnable = SprWindowEnable = Scr2WindowDisplayOutside = Scr2WindowEnable = false;
			BackColorIndex = 0;
			LineCurrent = LineCompare = 0;
			SprBase = SprFirst = SprCount = 0;
			Scr1Base = Scr2Base = 0;
			Scr2WinX0 = Scr2WinY0 = Scr2WinX1 = Scr2WinY1 = 0;
			SprWinX0 = SprWinY0 = SprWinX1 = SprWinY1 = 0;
			Scr1ScrollX = Scr1ScrollY = 0;
			Scr2ScrollX = Scr2ScrollY = 0;
			LcdActive = true;   //Final Lap 2000 depends on bootstrap doing this, otherwise LCD stays off?
			IconSleep = IconVertical = IconHorizontal = IconAux1 = IconAux2 = IconAux3 = false;
			VTotal = VerticalTotal - 1;
			VSync = VerticalTotal - 4; //Full usage/meaning unknown, so we're ignoring it for now
			for (var i = 0; i < PalMonoPools.Length; i++) PalMonoPools[i] = 0;
			for (var i = 0; i < PalMonoData.GetLength(0); i++) for (var j = 0; j < PalMonoData.GetLength(1); j++) PalMonoData[i, j] = 0;
			DisplayPackedFormatSet = false;
			HBlankTimer.Reset();
			VBlankTimer.Reset();
		}

		public void Shutdown()
		{
			//
		}

		public DisplayInterrupts Step(int clockCyclesInStep)
		{
			var interrupt = DisplayInterrupts.None;

			cycleCount += clockCyclesInStep;

			if (cycleCount >= clockCyclesPerLine)
			{
				// sprite fetch
				if (LineCurrent == VerticalDisp - 2)
				{
					spriteCountNextFrame = 0;
					for (var j = SprFirst; j < SprFirst + Math.Min(maxSpriteCount, SprCount); j++)
					{
						var k = (uint)((SprBase << 9) + (j << 2));
						spriteDataNextFrame[spriteCountNextFrame++] = (uint)(memoryReadDelegate(k + 3) << 24 | memoryReadDelegate(k + 2) << 16 | memoryReadDelegate(k + 1) << 8 | memoryReadDelegate(k + 0));
					}
				}

				// render pixels
				for (var x = 0; x < HorizontalDisp; x++)
					RenderPixel(LineCurrent, x);

				// line compare interrupt
				if (LineCurrent == LineCompare)
					interrupt |= DisplayInterrupts.LineCompare;

				// go to next scanline
				LineCurrent++;

				// is frame finished
				if (LineCurrent >= Math.Max(VerticalDisp, VTotal) + 1)
				{
					LineCurrent = 0;

					// copy sprite data for next frame
					for (int j = 0, k = spriteCountNextFrame - 1; k >= 0; j++, k--) spriteData[j] = spriteDataNextFrame[k];
					for (int j = 0; j < maxSpriteCount; j++) spriteDataNextFrame[j] = 0;

					OnUpdateScreen(new UpdateScreenEventArgs(outputFramebuffer.Clone() as byte[]));
					ResetScreenUsageMap();
				}
				else
				{
					// V-blank interrupt
					if (LineCurrent == VerticalDisp)
					{
						interrupt |= DisplayInterrupts.VBlank;

						// V-timer interrupt
						if (VBlankTimer.Step())
							interrupt |= DisplayInterrupts.VBlankTimer;
					}

					// H-timer interrupt
					if (HBlankTimer.Step())
						interrupt |= DisplayInterrupts.HBlankTimer;
				}

				// end of scanline
				cycleCount = 0;
			}

			return interrupt;
		}

		protected void RenderPixel(int y, int x)
		{
			if (y < 0 || y >= VerticalDisp || x < 0 || x >= HorizontalDisp) return;

			if (LcdActive)
			{
				RenderBackColor(y, x);
				RenderSCR1(y, x);
				RenderSCR2(y, x);
				RenderSprites(y, x);
			}
			else
			{
				// LCD sleeping
				RenderSleep(y, x);
			}
		}

		protected abstract void RenderSleep(int y, int x);
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
			var x0 = Scr2WinX0;
			var x1 = Scr2WinX1;
			var y0 = Scr2WinY0;
			var y1 = Scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return ((x >= x0 && x <= x1) || (x >= x1 && x <= x0)) &&
				((y >= y0 && y <= y1) || (y >= y1 && y <= y0));
		}

		protected bool IsOutsideSCR2Window(int y, int x)
		{
			var x0 = Scr2WinX0;
			var x1 = Scr2WinX1;
			var y0 = Scr2WinY0;
			var y1 = Scr2WinY1;

			ValidateWindowCoordinates(ref x0, ref x1, ref y0, ref y1);

			return x < x0 || x > x1 || y < y0 || y > y1;
		}

		protected bool IsInsideSPRWindow(int y, int x)
		{
			var x0 = SprWinX0;
			var x1 = SprWinX1;
			var y0 = SprWinY0;
			var y1 = SprWinY1;

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

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);
	}
}

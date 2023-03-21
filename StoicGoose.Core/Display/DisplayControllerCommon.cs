using System;

using StoicGoose.Common.Attributes;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.Machines;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Display
{
	public abstract class DisplayControllerCommon : IPortAccessComponent
	{
		public const int HorizontalDisp = 224;
		public const int HorizontalBlank = 32;
		public const int HorizontalTotal = HorizontalDisp + HorizontalBlank;
		public const double HorizontalClock = MachineCommon.CpuClock / HorizontalTotal;

		public const int VerticalDisp = 144;
		public const int VerticalBlank = 15;
		public const int VerticalTotal = VerticalDisp + VerticalBlank;
		public const double VerticalClock = 12000.0 / VerticalTotal;

		public const int ScreenWidth = HorizontalDisp;
		public const int ScreenHeight = VerticalDisp;

		protected const int maxSpriteCount = 128;
		protected const int maxSpritesPerLine = 32;

		[Flags]
		public enum DisplayInterrupts
		{
			None = 0,
			LineCompare = 1 << 0,
			VBlankTimer = 1 << 1,
			VBlank = 1 << 2,
			HBlankTimer = 1 << 3
		}

		protected readonly uint[] spriteData = new uint[maxSpriteCount];
		protected readonly uint[] spriteDataNextFrame = new uint[maxSpriteCount];
		protected readonly uint[] activeSpritesOnLine = new uint[maxSpritesPerLine];

		protected readonly bool[] isUsedBySCR2 = new bool[HorizontalDisp * VerticalDisp];

		protected int spriteCountNextFrame = 0, activeSpriteCountOnLine = 0;

		protected int cycleCount = 0;
		protected readonly byte[] outputFramebuffer = new byte[ScreenWidth * ScreenHeight * 4];

		public Action<byte[]> SendFramebuffer { get; set; } = default;

		protected readonly IMachine machine = default;

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
		protected readonly byte[] palMonoPools = default;
		/* REG_PALMONO_x */
		protected readonly byte[][] palMonoData = default;
		/* REG_xTMR_xxx */
		protected readonly DisplayTimer hBlankTimer = new(), vBlankTimer = new();

		public DisplayControllerCommon(IMachine machine)
		{
			this.machine = machine;

			palMonoPools = new byte[8];
			palMonoData = new byte[16][];
			for (var i = 0; i < palMonoData.GetLength(0); i++) palMonoData[i] = new byte[4];
		}

		public void Reset()
		{
			cycleCount = 0;

			Array.Fill<byte>(outputFramebuffer, 255);

			Array.Fill(isUsedBySCR2, false);

			Array.Fill<uint>(spriteData, 0);
			Array.Fill<uint>(spriteDataNextFrame, 0);
			Array.Fill<uint>(activeSpritesOnLine, 0);

			spriteCountNextFrame = 0;
			activeSpriteCountOnLine = 0;

			ResetRegisters();
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
			lcdActive = true; /* NOTE: Final Lap 2000 depends on bootstrap doing this, otherwise LCD stays off? */
			iconSleep = iconVertical = iconHorizontal = iconAux1 = iconAux2 = iconAux3 = false;
			vtotal = VerticalTotal - 1;
			vsync = VerticalTotal - 4; /* NOTE: Full usage/meaning unknown, so we're ignoring it for now */
			Array.Fill<byte>(palMonoPools, 0);
			for (var i = 0; i < palMonoData.GetLength(0); i++) Array.Fill<byte>(palMonoData[i], 0);
			hBlankTimer.Reset();
			vBlankTimer.Reset();
		}

		public void Shutdown()
		{
			/* Nothing to do... */
		}

		public DisplayInterrupts Step(int clockCyclesInStep)
		{
			var interrupt = DisplayInterrupts.None;

			cycleCount += clockCyclesInStep;

			if (cycleCount >= HorizontalTotal)
			{
				/* Sprite fetch */
				if (lineCurrent == VerticalDisp - 2)
				{
					spriteCountNextFrame = 0;
					for (var j = sprFirst; j < sprFirst + Math.Min(maxSpriteCount, sprCount); j++)
					{
						var k = (uint)((sprBase << 9) + (j << 2));
						spriteDataNextFrame[spriteCountNextFrame++] = (uint)(machine.ReadMemory(k + 3) << 24 | machine.ReadMemory(k + 2) << 16 | machine.ReadMemory(k + 1) << 8 | machine.ReadMemory(k + 0));
					}
				}

				/* Render pixels */
				for (var x = 0; x < HorizontalDisp; x++)
					RenderPixel(lineCurrent, x);

				/* Line compare interrupt */
				if (lineCurrent == lineCompare)
					interrupt |= DisplayInterrupts.LineCompare;

				/* H-timer interrupt */
				if (hBlankTimer.Step())
					interrupt |= DisplayInterrupts.HBlankTimer;

				/* V-blank interrupt */
				if (lineCurrent == VerticalDisp)
				{
					interrupt |= DisplayInterrupts.VBlank;

					/* V-timer interrupt */
					if (vBlankTimer.Step())
						interrupt |= DisplayInterrupts.VBlankTimer;

					/* Transfer framebuffer */
					SendFramebuffer?.Invoke(outputFramebuffer.Clone() as byte[]);
				}

				/* Advance scanline */
				lineCurrent++;

				/* Is frame finished? */
				if (lineCurrent > Math.Max(VerticalDisp, vtotal))
				{
					/* Copy sprite data for next frame */
					for (int j = 0, k = spriteCountNextFrame - 1; k >= 0; j++, k--) spriteData[j] = spriteDataNextFrame[k];
					Array.Fill<uint>(spriteDataNextFrame, 0);

					/* Reset variables */
					lineCurrent = 0;
					Array.Fill(isUsedBySCR2, false);
				}

				/* End of scanline */
				cycleCount = 0;
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
				/* LCD sleeping */
				RenderSleep(y, x);
			}
		}

		protected abstract void RenderSleep(int y, int x);
		protected abstract void RenderBackColor(int y, int x);
		protected abstract void RenderSCR1(int y, int x);
		protected abstract void RenderSCR2(int y, int x);
		protected abstract void RenderSprites(int y, int x);

		protected static void ValidateWindowCoordinates(ref int x0, ref int x1, ref int y0, ref int y1)
		{
			/* Thank you for this fix, for the encouragement and hints and advice, for just having been there... Thank you for everything, Near.
			 * https://forum.fobby.net/index.php?t=msg&goto=6085 */

			if (x0 > x1)
				(x1, x0) = (x0, x1);

			if (y0 > y1)
				(y1, y0) = (y0, y1);
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

		protected byte ReadMemory8(uint address) => machine.ReadMemory(address);
		protected ushort ReadMemory16(uint address) => (ushort)(machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));
		protected uint ReadMemory32(uint address) => (uint)(machine.ReadMemory(address + 3) << 24 | machine.ReadMemory(address + 2) << 16 | machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));

		public virtual byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
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

				case 0x02:
					/* REG_LINE_CUR */
					retVal |= (byte)(lineCurrent & 0xFF);
					break;

				case 0x03:
					/* REG_LINE_CMP */
					retVal |= (byte)(lineCompare & 0xFF);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					retVal |= (byte)(sprFirst & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					retVal |= (byte)(sprCount & 0xFF);
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
					retVal |= (byte)(palMonoPools[((port & 0b11) << 1) | 0] << 0);
					retVal |= (byte)(palMonoPools[((port & 0b11) << 1) | 1] << 4);
					break;

				case ushort _ when port >= 0x20 && port <= 0x3F:
					/* REG_PALMONO_x */
					retVal |= (byte)(palMonoData[(port >> 1) & 0b1111][((port & 0b1) << 1) | 0] << 0);
					retVal |= (byte)(palMonoData[(port >> 1) & 0b1111][((port & 0b1) << 1) | 1] << 4);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					ChangeBit(ref retVal, 0, hBlankTimer.Enable);
					ChangeBit(ref retVal, 1, hBlankTimer.Repeating);
					ChangeBit(ref retVal, 2, vBlankTimer.Enable);
					ChangeBit(ref retVal, 3, vBlankTimer.Repeating);
					break;

				case 0xA4:
				case 0xA5:
					/* REG_HTMR_FREQ */
					retVal |= (byte)((hBlankTimer.Frequency >> ((port & 0b1) * 8)) & 0xFF);
					break;

				case 0xA6:
				case 0xA7:
					/* REG_VTMR_FREQ */
					retVal |= (byte)((vBlankTimer.Frequency >> ((port & 0b1) * 8)) & 0xFF);
					break;

				case 0xA8:
				case 0xA9:
					/* REG_HTMR_CTR */
					retVal |= (byte)((hBlankTimer.Counter >> ((port & 0b1) * 8)) & 0xFF);
					break;

				case 0xAA:
				case 0xAB:
					/* REG_VTMR_CTR */
					retVal |= (byte)((vBlankTimer.Counter >> ((port & 0b1) * 8)) & 0xFF);
					break;
			}

			return retVal;
		}

		public virtual void WritePort(ushort port, byte value)
		{
			switch (port)
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

				case 0x03:
					/* REG_LINE_CMP */
					lineCompare = (byte)(value & 0xFF);
					break;

				case 0x05:
					/* REG_SPR_FIRST */
					sprFirst = (byte)(value & 0x7F);
					break;

				case 0x06:
					/* REG_SPR_COUNT */
					sprCount = (byte)(value & 0xFF);
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
					palMonoPools[((port & 0b11) << 1) | 0] = (byte)((value >> 0) & 0b1111);
					palMonoPools[((port & 0b11) << 1) | 1] = (byte)((value >> 4) & 0b1111);
					break;

				case ushort _ when port >= 0x20 && port <= 0x3F:
					/* REG_PALMONO_x */
					palMonoData[(port >> 1) & 0b1111][((port & 0b1) << 1) | 0] = (byte)((value >> 0) & 0b111);
					palMonoData[(port >> 1) & 0b1111][((port & 0b1) << 1) | 1] = (byte)((value >> 4) & 0b111);
					break;

				case 0xA2:
					/* REG_TMR_CTRL */
					hBlankTimer.Enable = IsBitSet(value, 0);
					hBlankTimer.Repeating = IsBitSet(value, 1);
					vBlankTimer.Enable = IsBitSet(value, 2);
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

		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SCR1 enable", 0)]
		public bool Scr1Enable => scr1Enable;
		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SCR2 enable", 1)]
		public bool Scr2Enable => scr2Enable;
		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SPR enable", 2)]
		public bool SprEnable => sprEnable;
		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SPR window enable", 3)]
		public bool SprWindowEnable => sprWindowEnable;
		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SCR2 window mode; display outside?", 4)]
		public bool Scr2WindowDisplayOutside => scr2WindowDisplayOutside;
		[Port("REG_DISP_CTRL", 0x000)]
		[BitDescription("SCR2 window enable", 5)]
		public bool Scr2WindowEnable => scr2WindowEnable;
		[Port("REG_BACK_COLOR", 0x001)]
		[BitDescription("Background color pool index", 0, 2)]
		public virtual byte BackColorIndex => backColorIndex;
		[Port("REG_LINE_CUR", 0x002)]
		[BitDescription("Current line being drawn")]
		public int LineCurrent => lineCurrent;
		[Port("REG_LINE_CMP", 0x003)]
		[BitDescription("Line compare interrupt line")]
		public int LineCompare => lineCompare;
		[Port("REG_SPR_BASE", 0x004)]
		[BitDescription("Sprite table base address", 0, 4)]
		[Format("X4", 9)]
		public virtual int SprBase => sprBase;
		[Port("REG_SPR_FIRST", 0x005)]
		[BitDescription("First sprite to draw", 0, 6)]
		public int SprFirst => sprFirst;
		[Port("REG_SPR_COUNT", 0x006)]
		[BitDescription("Number of sprites to draw")]
		public int SprCount => sprCount;
		[Port("REG_MAP_BASE", 0x007)]
		[BitDescription("SCR1 base address", 0, 2)]
		[Format("X4", 11)]
		public virtual int Scr1Base => scr1Base;
		[Port("REG_MAP_BASE", 0x007)]
		[BitDescription("SCR2 base address", 4, 6)]
		[Format("X4", 11)]
		public virtual int Scr2Base => scr2Base;
		[Port("REG_SCR2_WIN_X0", 0x008)]
		[BitDescription("Top-left X of SCR2 window")]
		public int Scr2WinX0 => scr2WinX0;
		[Port("REG_SCR2_WIN_Y0", 0x009)]
		[BitDescription("Top-left Y of SCR2 window")]
		public int Scr2WinY0 => scr2WinY0;
		[Port("REG_SCR2_WIN_X1", 0x00A)]
		[BitDescription("Bottom-right X of SCR2 window")]
		public int Scr2WinX1 => scr2WinX1;
		[Port("REG_SCR2_WIN_Y1", 0x00B)]
		[BitDescription("Bottom-right Y of SCR2 window")]
		public int Scr2WinY1 => scr2WinY1;
		[Port("REG_SPR_WIN_X0", 0x00C)]
		[BitDescription("Top-left X of SPR window")]
		public int SprWinX0 => sprWinX0;
		[Port("REG_SPR_WIN_Y0", 0x00D)]
		[BitDescription("Top-left Y of SPR window")]
		public int SprWinY0 => sprWinY0;
		[Port("REG_SPR_WIN_X1", 0x00E)]
		[BitDescription("Bottom-right X of SPR window")]
		public int SprWinX1 => sprWinX1;
		[Port("REG_SPR_WIN_Y1", 0x00F)]
		[BitDescription("Bottom-right Y of SPR window")]
		public int SprWinY1 => sprWinY1;
		[Port("REG_SCR1_X", 0x010)]
		[BitDescription("SCR1 X scroll")]
		public int Scr1ScrollX => scr1ScrollX;
		[Port("REG_SCR1_Y", 0x011)]
		[BitDescription("SCR1 Y scroll")]
		public int Scr1ScrollY => scr1ScrollY;
		[Port("REG_SCR2_X", 0x012)]
		[BitDescription("SCR2 X scroll")]
		public int Scr2ScrollX => scr2ScrollX;
		[Port("REG_SCR2_Y", 0x013)]
		[BitDescription("SCR2 Y scroll")]
		public int Scr2ScrollY => scr2ScrollY;
		[Port("REG_LCD_CTRL", 0x014)]
		[BitDescription("LCD sleep mode; is LCD active?", 0)]
		public bool LcdActive => lcdActive;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Sleep indicator", 0)]
		public bool IconSleep => iconSleep;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Vertical orientation indicator", 1)]
		public bool IconVertical => iconVertical;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Horizontal orientation indicator", 2)]
		public bool IconHorizontal => iconHorizontal;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Auxiliary 1 (Small circle)", 3)]
		public bool IconAux1 => iconAux1;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Auxiliary 2 (Medium circle)", 4)]
		public bool IconAux2 => iconAux2;
		[Port("REG_LCD_ICON", 0x015)]
		[BitDescription("Auxiliary 3 (Big circle)", 5)]
		public bool IconAux3 => iconAux3;
		[Port("REG_LCD_VTOTAL", 0x016)]
		[BitDescription("Display VTOTAL")]
		public int VTotal => vtotal;
		[Port("REG_LCD_VSYNC", 0x017)]
		[BitDescription("VSYNC line position")]
		public int VSync => vsync;
		[Port("REG_TMR_CTRL", 0x0A2)]
		[BitDescription("H-blank timer enable", 0)]
		public bool HBlankTimerEnable => hBlankTimer.Enable;
		[Port("REG_TMR_CTRL", 0x0A2)]
		[BitDescription("H-blank timer mode; is repeating?", 1)]
		public bool HBlankTimerRepeating => hBlankTimer.Repeating;
		[Port("REG_TMR_CTRL", 0x0A2)]
		[BitDescription("V-blank timer enable", 2)]
		public bool VBlankTimerEnable => vBlankTimer.Enable;
		[Port("REG_TMR_CTRL", 0x0A2)]
		[BitDescription("V-blank timer mode; is repeating?", 3)]
		public bool VBlankTimerRepeating => vBlankTimer.Repeating;
		[Port("REG_HTMR_FREQ", 0x0A4, 0x0A5)]
		[BitDescription("H-blank timer frequency")]
		public ushort HBlankTimerFrequency => hBlankTimer.Frequency;
		[Port("REG_VTMR_FREQ", 0x0A6, 0x0A7)]
		[BitDescription("V-blank timer frequency")]
		public ushort VBlankTimerFrequency => vBlankTimer.Frequency;
		[Port("REG_HTMR_CTR", 0x0A8, 0x0A9)]
		[BitDescription("H-blank timer counter")]
		public ushort HBlankTimerCounter => hBlankTimer.Counter;
		[Port("REG_VTMR_CTR", 0x0AA, 0x0AB)]
		[BitDescription("V-blank timer counter")]
		public ushort VBlankTimerCounter => vBlankTimer.Counter;

		// TODO: reorganize palmono stuff & add attributes

		public byte[] PalMonoPools => palMonoPools;
		public byte[][] PalMonoData => palMonoData;
	}
}

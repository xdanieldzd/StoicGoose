using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using StoicGoose.Disassembly;
using StoicGoose.Emulation;

namespace StoicGoose.WinForms.Controls
{
	public partial class DisassemblyBox : UserControl
	{
		public delegate byte MemoryReadDelegate(uint address);
		public delegate void MemoryWriteDelegate(uint address, byte value);
		public delegate byte RegisterReadDelegate(ushort register);
		public delegate void RegisterWriteDelegate(ushort register, byte value);

		[Browsable(false)]
		public EmulatorHandler EmulatorHandler { get; set; } = default;

		[DefaultValue(0)]
		public ushort DisasmSegment { get; set; } = 0;
		[DefaultValue(0)]
		public ushort DisasmOffset { get; set; } = 0;

		[DefaultValue(4)]
		public int VisibleDisasmOps { get; set; } = 4;

		readonly StringFormat stringFormat = default;
		readonly Disassembler disassembler = new();

		bool fontHasChanged = true;

		float lineHeight = 0f;
		readonly float[] xPos = new float[3];

		(ushort segment, ushort offset)[] currentDisasmAddresses = Array.Empty<(ushort segment, ushort offset)>();

		int centerLine = -1;
		int currentHighlightLine = -1;

		public DisassemblyBox()
		{
			InitializeComponent();

			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			stringFormat = new StringFormat(StringFormat.GenericDefault);
			stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
		}

		protected override void OnLoad(EventArgs e)
		{
			if (EmulatorHandler == null) return;

			disassembler.ReadDelegate = new Disassembly.MemoryReadDelegate(EmulatorHandler.Machine.ReadMemory);
			disassembler.WriteDelegate = new Disassembly.MemoryWriteDelegate(EmulatorHandler.Machine.WriteMemory);

			base.OnLoad(e);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			VisibleDisasmOps = Height / (Font.Height + 1);

			currentDisasmAddresses = new (ushort segment, ushort offset)[VisibleDisasmOps + 1];
			for (var i = 0; i < currentDisasmAddresses.Length; i++) currentDisasmAddresses[i] = (0xDEAD, 0xBEEF);

			centerLine = VisibleDisasmOps / 2;
			currentHighlightLine = -1;

			UpdateDisassemblyAddresses();

			base.OnSizeChanged(e);
		}

		protected override void OnFontChanged(EventArgs e)
		{
			fontHasChanged = true;

			OnSizeChanged(EventArgs.Empty);

			base.OnFontChanged(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			Invalidate();

			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			Invalidate();

			base.OnLostFocus(e);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			return keyData switch
			{
				Keys.Up or Keys.Down or Keys.Left or Keys.Right => true,
				_ => base.IsInputKey(keyData),
			};
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (EmulatorHandler == null) return;

			/* Setup drawing */
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

			/* Measure things */
			if (fontHasChanged)
			{
				lineHeight = Font.Height + 1f;
				xPos[0] = 16;
				xPos[1] = xPos[0] + e.Graphics.MeasureString(string.Empty.PadRight(10), Font, -1, stringFormat).Width;
				xPos[2] = xPos[1] + e.Graphics.MeasureString(string.Empty.PadRight(24), Font, -1, stringFormat).Width;

				fontHasChanged = false;
			}

			/* Clear control & draw divider */
			e.Graphics.FillRectangle(Brushes.WhiteSmoke, 0, 0, xPos[0], Height);
			e.Graphics.DrawLine(SystemPens.WindowText, xPos[0], 0, xPos[0], Height);

			/* Get current CS/IP for comparing */
			var cpuStatus = EmulatorHandler.Machine.GetProcessorStatus();
			var execCs = cpuStatus["cs"];
			var execIp = cpuStatus["ip"];

			/* Loop over opcodes until view is full */
			for (var i = 0; i < VisibleDisasmOps + 1; i++)
			{
				var (cs, ip) = currentDisasmAddresses[i];
				var isCurrentCsIp = execCs == cs && execIp == ip;
				var (_, _, bytes, disasm, comment) = disassembler.DisassembleInstruction(cs, ip);

				var y = i * lineHeight;
				e.Graphics.DrawString($"{cs:X4}:{ip:X4}", Font, isCurrentCsIp ? Brushes.MediumBlue : SystemBrushes.WindowText, xPos[0], y + 2);
				e.Graphics.DrawString($"{string.Join(" ", bytes.Select(x => ($"{x:X2}"))),-24}", Font, isCurrentCsIp ? Brushes.SteelBlue : SystemBrushes.ControlDark, xPos[1], y + 2);
				e.Graphics.DrawString($"{disasm,-32}{(!string.IsNullOrEmpty(comment) ? $";{comment}" : "")}", Font, isCurrentCsIp ? Brushes.MediumBlue : SystemBrushes.WindowText, xPos[2], y + 2);

				/* Highlight opcode at current CS/IP */
				if (execCs == cs && execIp == ip)
				{
					e.Graphics.DrawLine(SystemPens.WindowText, xPos[0], y, Width, y);
					e.Graphics.DrawLine(SystemPens.WindowText, xPos[0], y + lineHeight, Width, y + lineHeight);

					if (!EmulatorHandler.IsPaused)
					{
						/* Draw ▶️ symbol */
						e.Graphics.FillPolygon(Brushes.Green, new PointF[] { new PointF(4.0f, y), new PointF(12.0f, y + (lineHeight / 2.0f)), new PointF(4.0f, y + lineHeight) });
					}
					else
					{
						/* Draw ⏸️ symbol */
						e.Graphics.FillRectangle(Brushes.Blue, 3.0f, y + 2.0f, 4.0f, lineHeight - 2.0f);
						e.Graphics.FillRectangle(Brushes.Blue, 9.0f, y + 2.0f, 4.0f, lineHeight - 2.0f);
					}
				}
			}
		}

		public void UpdateToCurrentCSIP()
		{
			if (EmulatorHandler == null) return;

			var cpuStatus = EmulatorHandler.Machine.GetProcessorStatus();
			DisasmSegment = cpuStatus["cs"];
			DisasmOffset = cpuStatus["ip"];
		}

		public void UpdateDisassemblyAddresses()
		{
			if (EmulatorHandler == null) return;

			var cpuStatus = EmulatorHandler.Machine.GetProcessorStatus();
			var currentAddress = (seg: cpuStatus["cs"], ofs: cpuStatus["ip"]);

			if (!currentDisasmAddresses.Contains(currentAddress))
			{
				var segment = currentAddress.seg;
				var offset = currentAddress.ofs;

				for (var i = 0; i < currentDisasmAddresses.Length; i++)
				{
					currentDisasmAddresses[i] = (segment, offset);
					offset += (ushort)disassembler.EvaluateInstructionLength(segment, offset);
				}
			}

			currentHighlightLine = Array.IndexOf(currentDisasmAddresses, currentAddress);

			if (currentHighlightLine >= centerLine)
			{
				var difference = currentHighlightLine - centerLine;
				var addressesToKeep = currentDisasmAddresses.Skip(difference).Take(currentDisasmAddresses.Length - difference);

				var (readSegment, readOffset) = addressesToKeep.LastOrDefault();
				var addressesToAdd = new List<(ushort, ushort)>();
				while (addressesToAdd.Count < difference)
				{
					var length = disassembler.EvaluateInstructionLength(readSegment, readOffset);
					readOffset += (ushort)length;
					addressesToAdd.Add((readSegment, readOffset));
				}

				currentDisasmAddresses = addressesToKeep.Concat(addressesToAdd).ToArray();
			}
		}
	}
}

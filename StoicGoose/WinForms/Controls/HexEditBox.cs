using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Controls
{
	public partial class HexEditBox : UserControl
	{
		// TODO: further optimize, update, etc, etc -- plenty of this is 2011/12-ish code

		const int textPadding = 2;

		public delegate byte MemoryReadDelegate(uint address);
		public delegate void MemoryWriteDelegate(uint address, byte value);

		[DefaultValue(0)]
		public int BaseOffset { get; set; } = 0;
		[DefaultValue(0)]
		public int SelectedOffset { get; set; } = 0;
		[DefaultValue(true)]
		public bool ShowOffsetPrefix { get; set; } = true;
		[DefaultValue(2)]
		public int OffsetBytes { get; set; } = 2;
		[DefaultValue(0xFFFF)]
		public uint OffsetMask { get; set; } = 0xFFFF;
		[Browsable(false)]
		public MemoryReadDelegate ReadByte { get; set; } = default;
		[Browsable(false)]
		public MemoryWriteDelegate WriteByte { get; set; } = default;
		[DefaultValue(false)]
		public bool ReadOnly { get; set; } = false;
		[DefaultValue(16)]
		public int BytesPerLine { get; set; } = 16;
		[DefaultValue(false)]
		public bool AllowWidthChange { get; set; } = false;
		[DefaultValue(false)]
		public bool AllowWrapAround { get; set; } = false;

		[Browsable(false)]
		public Point CursorPosition => cursorPosition;

		int maxLines = 0, leftMargin = 0, lineWidth = 0, asciiWidth = 0, fontHeight = 0, maxBytesPerLine = 0, inBytePos = 0;
		bool overBytesPerLineDrag = false, doingBytesPerLineDrag = false;
		Point cursorPosition = Point.Empty, mouseDownPosition = Point.Empty;
		byte editedByte = 0;

		readonly StringFormat stringFormat = default;
		readonly Graphics graphics = default;

		public HexEditBox()
		{
			InitializeComponent();

			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			stringFormat = new StringFormat(StringFormat.GenericDefault);
			stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
			graphics = CreateGraphics();
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

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Up:
					AdvancePosition(0, -1);
					break;
				case Keys.Down:
					AdvancePosition(0, 1);
					break;
				case Keys.Left:
					AdvancePosition(-1, 0);
					break;
				case Keys.Right:
					AdvancePosition(1, 0);
					break;

				case Keys.PageUp:
					AdvancePosition(0, -(maxLines - 1));
					break;
				case Keys.PageDown:
					AdvancePosition(0, maxLines - 1);
					break;
			}

			if (e.KeyData == Keys.Up || e.KeyData == Keys.Down || e.KeyData == Keys.Left || e.KeyData == Keys.Right ||
				 e.KeyData == Keys.PageUp || e.KeyData == Keys.PageDown)
				Invalidate();

			base.OnKeyDown(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (ReadOnly) return;

			if (char.IsDigit(e.KeyChar) || (e.KeyChar >= 'A' && e.KeyChar <= 'F') || (e.KeyChar >= 'a' && e.KeyChar <= 'f'))
			{
				var input = $"{char.ToUpper(e.KeyChar)}";

				if (inBytePos == 0)
				{
					editedByte = (byte)(byte.Parse(input, NumberStyles.HexNumber) << 4);
					inBytePos++;
				}
				else if (inBytePos == 1)
				{
					editedByte += (byte)(byte.Parse(input, NumberStyles.HexNumber) & 0xF);
					inBytePos++;
				}
				else if (inBytePos >= 2)
				{
					WriteEditedByte();
					AdvancePosition(1, 0);
					editedByte = (byte)(byte.Parse(input, NumberStyles.HexNumber) << 4);
					inBytePos = 1;
				}
			}
			else if (e.KeyChar == '\b')
			{
				if (inBytePos >= 2)
				{
					editedByte = (byte)(editedByte & 0xF0);
					inBytePos--;
				}
				else if (inBytePos == 1)
				{
					editedByte = 0;
					inBytePos--;
				}

				if (inBytePos < 0) inBytePos = 0;
			}
			else if (e.KeyChar == '\r')
				WriteEditedByte();
			else
				e.Handled = true;

			Invalidate();

			base.OnKeyPress(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (e.Delta != 0)
			{
				var change = BytesPerLine * -(e.Delta / 120);
				BaseOffset += change;
				if (inBytePos != 0) inBytePos = 0;

				if (change > 0 && cursorPosition.Y > 0)
					AdvancePosition(0, -1);
				else if (change < 0 && cursorPosition.Y < maxLines - 2)
					AdvancePosition(0, 1);
			}

			Invalidate();

			base.OnMouseWheel(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			if (!overBytesPerLineDrag)
			{
				if (e.Location.X > lineWidth ||
					(e.Location.Y - fontHeight) < 0 ||
					(e.Location.X - leftMargin) < 0)
					return;

				var newCursorPosition = new Point(
					(e.Location.X - leftMargin) / (int)(graphics.MeasureString("XX", Font).Width + 1),
					(e.Location.Y - fontHeight) / fontHeight);

				if (newCursorPosition != cursorPosition)
				{
					cursorPosition = newCursorPosition;
					SelectedOffset = (int)((BaseOffset + (cursorPosition.Y * BytesPerLine) + cursorPosition.X) & OffsetMask);
					inBytePos = 0;
				}
			}

			Invalidate();

			base.OnMouseClick(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (AllowWidthChange && overBytesPerLineDrag)
			{
				doingBytesPerLineDrag = true;
				mouseDownPosition = e.Location;
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			overBytesPerLineDrag = false;
			doingBytesPerLineDrag = false;

			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!AllowWidthChange) return;

			if (!doingBytesPerLineDrag)
			{
				if (e.X >= lineWidth - 2 && e.X <= lineWidth + 2)
				{
					Cursor = Cursors.VSplit;
					overBytesPerLineDrag = true;
				}
				else
				{
					Cursor = Cursors.Default;
					overBytesPerLineDrag = false;
				}
			}
			else
			{
				var entryWidth = graphics.MeasureString("XX", Font).Width;
				var distance = (float)(e.Location.X - mouseDownPosition.X);

				if (distance >= entryWidth)
				{
					BytesPerLine++;

					if (BytesPerLine > maxBytesPerLine)
						BytesPerLine = maxBytesPerLine;
					else
						mouseDownPosition.X += (int)entryWidth;
				}
				else if (distance <= -entryWidth)
				{
					BytesPerLine--;

					if (BytesPerLine < 1)
						BytesPerLine = 1;
					else
						mouseDownPosition.X -= (int)entryWidth;
				}

				if (BytesPerLine <= cursorPosition.X)
					cursorPosition.X--;

				Invalidate();
			}

			base.OnMouseMove(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

			var lineInfoTemp = string.Empty.PadRight((OffsetBytes * 2) + (ShowOffsetPrefix ? 2 : 0));
			leftMargin = (int)e.Graphics.MeasureString(lineInfoTemp, Font, -1, stringFormat).Width;

			fontHeight = (int)e.Graphics.MeasureString("X", Font).Height + (textPadding * 2);
			maxLines = ClientSize.Height / fontHeight;
			maxBytesPerLine = (int)Math.Round((decimal)((ClientSize.Width - leftMargin - asciiWidth) / e.Graphics.MeasureString("XX", Font).Width), MidpointRounding.ToEven) - 1;

			SuspendLayout();

			PaintRowInfo(e.Graphics);
			PaintLineInfo(e.Graphics);
			PaintGrid(e.Graphics);
			PaintHex(e.Graphics);
			PaintASCII(e.Graphics);

			ResumeLayout(true);
		}

		private void PaintRowInfo(Graphics g)
		{
			lineWidth = 0;

			var entryWidth = g.MeasureString("XX", Font).Width;

			using var foreColorBrush = new SolidBrush(ForeColor);
			for (int x = 0; x < BytesPerLine; x++)
			{
				g.DrawString(x.ToString("X2"), Font,
					cursorPosition.X == x ? Brushes.Red : foreColorBrush,
					leftMargin + (entryWidth * x),
					textPadding);

				lineWidth = (int)(leftMargin + (entryWidth * x) + entryWidth);
			}
		}

		private void PaintLineInfo(Graphics g)
		{
			using var foreColorBrush = new SolidBrush(ForeColor);
			for (int y = 1; y < maxLines; y++)
			{
				g.DrawString((ShowOffsetPrefix ? "0x" : "") + ((uint)(BaseOffset + ((y - 1) * BytesPerLine)) & OffsetMask).ToString("X" + (OffsetBytes * 2)), Font,
					CursorPosition.Y == (y - 1) ? Brushes.Red : foreColorBrush,
					0,
					(fontHeight * y) + textPadding);
			}
		}

		private void PaintGrid(Graphics g)
		{
			g.DrawLine(SystemPens.ButtonFace, new Point(leftMargin, 0), new Point(leftMargin, ClientSize.Width));
			g.DrawLine(SystemPens.ButtonFace, new Point(0, fontHeight), new Point(ClientSize.Width, fontHeight));
			g.DrawLine(SystemPens.ButtonFace, new Point(lineWidth, 0), new Point(lineWidth, ClientSize.Width));
		}

		private void PaintHex(Graphics g)
		{
			var entryWidth = g.MeasureString("XX", Font).Width;

			for (int y = 1; y < maxLines; y++)
			{
				for (int x = 0; x < BytesPerLine; x++)
				{
					var thisPos = new PointF(leftMargin + (entryWidth * x), (fontHeight * y) + textPadding);
					var brush = (x % 2) == 1 ? SystemBrushes.WindowText : Brushes.MediumBlue;

					var showEditedByte = false;

					if (cursorPosition.Y == (y - 1) && cursorPosition.X == x)
					{
						if (inBytePos == 0)
						{
							if (Focused)
							{
								g.FillRectangle(SystemBrushes.WindowText, thisPos.X, thisPos.Y - textPadding, entryWidth, fontHeight - textPadding);
								brush = SystemBrushes.Window;
							}
							else
								brush = Brushes.Red;
						}
						else
						{
							g.FillRectangle(Brushes.Red, thisPos.X, thisPos.Y - textPadding, entryWidth, fontHeight - textPadding);
							brush = SystemBrushes.Window;

							showEditedByte = true;
						}
					}

					if (!showEditedByte)
					{
						var offset = (uint)((BaseOffset + ((y - 1) * BytesPerLine) + x) & OffsetMask);
						g.DrawString((ReadByte != null ? ReadByte(offset) : 0x00).ToString("X2"), Font, brush, thisPos);
					}
					else
					{
						if (inBytePos == 1)
							g.DrawString(((editedByte & 0xF0) >> 4).ToString("X"), Font, brush, thisPos);
						else if (inBytePos >= 2)
							g.DrawString(editedByte.ToString("X2"), Font, brush, thisPos);
					}
				}
			}
		}

		private void PaintASCII(Graphics g)
		{
			var asciiString = string.Empty;

			for (int y = 1; y < maxLines; y++)
			{
				var thisPos = new PointF(lineWidth, (fontHeight * y) + textPadding);

				asciiString = string.Empty;
				for (int x = 0; x < BytesPerLine; x++)
				{
					var offset = (uint)((BaseOffset + ((y - 1) * BytesPerLine) + x) & OffsetMask);
					var read = (char)(ReadByte != null ? ReadByte(offset) : 0x00);
					if (read < 0x20 || read > 126) read = '.';
					asciiString += read.ToString();
				}

				g.DrawString(asciiString, Font, SystemBrushes.ControlDark, thisPos);
			}

			asciiWidth = (int)g.MeasureString(asciiString, Font, -1, stringFormat).Width;
		}

		private void AdvancePosition(int x, int y)
		{
			if (x != 0)
			{
				cursorPosition.X += x;
				if (cursorPosition.X < 0)
				{
					cursorPosition.X = BytesPerLine - 1;
					AdvancePosition(0, -1);
				}
				else if (cursorPosition.X > BytesPerLine - 1)
				{
					cursorPosition.X = 0;
					AdvancePosition(0, 1);
				}
			}

			if (y != 0)
			{
				cursorPosition.Y += y;
				if (cursorPosition.Y < 0)
				{
					cursorPosition.Y = 0;
					BaseOffset += BytesPerLine * y;
				}
				else if (cursorPosition.Y > maxLines - 2)
				{
					cursorPosition.Y = maxLines - 2;
					BaseOffset += BytesPerLine * y;
				}
			}

			SelectedOffset = (int)((BaseOffset + (cursorPosition.Y * BytesPerLine) + cursorPosition.X) & OffsetMask);
			inBytePos = 0;
		}

		private void WriteEditedByte()
		{
			var byteToWrite = (byte)0;

			if (inBytePos >= 2)
				byteToWrite = editedByte;
			else if (inBytePos == 1)
				byteToWrite = (byte)((editedByte & 0xF0) >> 4);

			if (inBytePos != 0)
			{
				WriteByte((uint)SelectedOffset, byteToWrite);
				inBytePos = 0;
			}
		}
	}
}

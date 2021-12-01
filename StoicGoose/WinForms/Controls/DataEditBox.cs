using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using StoicGoose.Extensions;

namespace StoicGoose.WinForms.Controls
{
	public partial class DataEditBox : UserControl
	{
		// TODO: selection & editing functionality

		[Browsable(false)]
		public Func<Dictionary<string, object>> ReadData { get; set; } = default;
		[Browsable(false)]
		public Func<Dictionary<string, object>> WriteData { get; set; } = default;

		readonly StringFormat stringFormat = default;

		public DataEditBox()
		{
			InitializeComponent();

			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

			stringFormat = new StringFormat(StringFormat.GenericDefault);
			stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
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
			if (ReadData == null) return;

			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

			var lineHeight = Font.Height + 1f;

			var data = ReadData().Where(x => x.Value.IsNumber());
			var keysWidth = e.Graphics.MeasureString(string.Empty.PadRight(data.Max(x => x.Key.Length + 2)), Font, ClientSize.Width, stringFormat).Width;

			var y = 1f;

			foreach (var (key, valueObject) in data)
			{
				e.Graphics.DrawString(key, Font, SystemBrushes.WindowText, 1f, y, stringFormat);

				var formatString = Type.GetTypeCode(valueObject.GetType()) switch
				{
					TypeCode.Byte or TypeCode.SByte => "0x{0:X2}",
					TypeCode.Int16 or TypeCode.UInt16 => "0x{0:X4}",
					TypeCode.Int32 or TypeCode.UInt32 => "0x{0:X8}",
					TypeCode.Int64 or TypeCode.UInt64 => "0x{0:X16}",
					TypeCode.Single or TypeCode.Double => "{0:0.00}",
					_ => "{0}",
				};
				e.Graphics.DrawString(string.Format(formatString, valueObject), Font, SystemBrushes.WindowText, 1f + keysWidth, y);

				y += lineHeight;
			}
		}
	}
}

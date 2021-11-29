using System;
using System.Windows.Forms;

using StoicGoose.WinForms.Controls;

namespace StoicGoose
{
	public partial class MemoryEditorForm : Form
	{
		public MemoryEditorForm(Func<uint, byte> memoryRead, Action<uint, byte> memoryWrite)
		{
			InitializeComponent();

			hexEditBox.ReadByte = new HexEditBox.MemoryReadDelegate(memoryRead);
			hexEditBox.WriteByte = new HexEditBox.MemoryWriteDelegate(memoryWrite);
		}

		private void tmrUpdate_Tick(object sender, EventArgs e)
		{
			hexEditBox.Invalidate();
		}

		private void vsbLocation_Scroll(object sender, ScrollEventArgs e)
		{
			hexEditBox.BaseOffset = (e.NewValue << 4) & 0xFFFF0;
			hexEditBox.Invalidate();
		}

		private void hexEditBox_KeyDown(object sender, KeyEventArgs e)
		{
			vsbLocation.Value = (hexEditBox.BaseOffset & 0xFFFF0) >> 4;
		}
	}
}

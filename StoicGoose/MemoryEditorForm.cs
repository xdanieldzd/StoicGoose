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

			hexEditBox.ReadMemory = new HexEditBox.MemoryReadDelegate(memoryRead);
			hexEditBox.WriteMemory = new HexEditBox.MemoryWriteDelegate(memoryWrite);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				Hide();
				e.Cancel = true;
			}

			base.OnFormClosing(e);
		}

		private void tmrUpdate_Tick(object sender, EventArgs e)
		{
			if (!Visible) return;

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

		private void btnClose_Click(object sender, EventArgs e)
		{
			Hide();
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using StoicGoose.Emulation;

namespace StoicGoose
{
	public partial class DebuggerMainForm : Form
	{
		readonly EmulatorHandler emulatorHandler = default;
		readonly Action pauseEmulation = default;
		readonly Action unpauseEmulation = default;
		readonly Action resetEmulation = default;

		readonly MemoryEditorForm memoryEditorForm = default;

		public DebuggerMainForm(EmulatorHandler handler, Action pause, Action unpause, Action reset)
		{
			InitializeComponent();

			emulatorHandler = disassemblyBox.EmulatorHandler = handler;
			pauseEmulation = pause;
			unpauseEmulation = unpause;
			resetEmulation = reset;

			memoryEditorForm = new MemoryEditorForm(emulatorHandler.Machine.ReadMemory, emulatorHandler.Machine.WriteMemory);

			debRegisters.ReadData += () => emulatorHandler.Machine.GetProcessorStatus().ToDictionary(x => x.Key, x => (object)x.Value);

			chkTrace.Checked = true;
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

			if (emulatorHandler.IsPaused)
				chkPause.Checked = true;

			if (chkTrace.Checked)
				disassemblyBox.UpdateToCurrentCSIP();

			disassemblyBox.UpdateDisassemblyAddresses();

			disassemblyBox.Invalidate();
			debRegisters.Invalidate();
		}

		private void btnStep_Click(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsPaused)
				chkPause.Checked = true;

			emulatorHandler.Machine.RunStep();
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			resetEmulation();
		}

		private void chkPause_CheckedChanged(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsRunning) return;

			if (sender is CheckBox checkBox)
			{
				if (checkBox.Checked)
					pauseEmulation();
				else
					unpauseEmulation();
			}
		}

		private void chkTrace_CheckedChanged(object sender, EventArgs e)
		{
			if (!emulatorHandler.IsRunning) return;

			if (sender is CheckBox checkBox)
			{
				tmrUpdate.Enabled = checkBox.Checked;
			}
		}

		private void btnMemoryEditor_Click(object sender, EventArgs e)
		{
			memoryEditorForm.Show();
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Hide();
		}
	}
}

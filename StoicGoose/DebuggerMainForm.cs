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
		public DebuggerMainForm(EmulatorHandler emulatorHandler)
		{
			InitializeComponent();

			disassemblyBox.EmulatorHandler = emulatorHandler;
		}

		private void tmrUpdate_Tick(object sender, EventArgs e)
		{
			disassemblyBox.UpdateToCurrentCSIP();
			disassemblyBox.Invalidate();
		}
	}
}

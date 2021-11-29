
namespace StoicGoose
{
	partial class DebuggerMainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.disassemblyBox = new StoicGoose.WinForms.Controls.DisassemblyBox();
			this.tmrUpdate = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// disassemblyBox
			// 
			this.disassemblyBox.BackColor = System.Drawing.SystemColors.Window;
			this.disassemblyBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.disassemblyBox.DisasmOffset = ((ushort)(0));
			this.disassemblyBox.DisasmSegment = ((ushort)(0));
			this.disassemblyBox.EmulatorHandler = null;
			this.disassemblyBox.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.disassemblyBox.Location = new System.Drawing.Point(12, 12);
			this.disassemblyBox.Name = "disassemblyBox";
			this.disassemblyBox.Size = new System.Drawing.Size(720, 437);
			this.disassemblyBox.TabIndex = 0;
			this.disassemblyBox.VisibleDisasmOps = 31;
			// 
			// tmrUpdate
			// 
			this.tmrUpdate.Enabled = true;
			this.tmrUpdate.Interval = 15;
			this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
			// 
			// DebuggerMainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(784, 461);
			this.Controls.Add(this.disassemblyBox);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DebuggerMainForm";
			this.ShowIcon = false;
			this.Text = "Debugger";
			this.ResumeLayout(false);

		}

		#endregion

		private WinForms.Controls.DisassemblyBox disassemblyBox;
		private System.Windows.Forms.Timer tmrUpdate;
	}
}

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
			this.chkPause = new System.Windows.Forms.CheckBox();
			this.chkTrace = new System.Windows.Forms.CheckBox();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnStep = new System.Windows.Forms.Button();
			this.btnReset = new System.Windows.Forms.Button();
			this.btnMemoryEditor = new System.Windows.Forms.Button();
			this.debRegisters = new StoicGoose.WinForms.Controls.DataEditBox();
			this.SuspendLayout();
			// 
			// disassemblyBox
			// 
			this.disassemblyBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.disassemblyBox.BackColor = System.Drawing.SystemColors.Window;
			this.disassemblyBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.disassemblyBox.DisasmOffset = ((ushort)(0));
			this.disassemblyBox.DisasmSegment = ((ushort)(0));
			this.disassemblyBox.EmulatorHandler = null;
			this.disassemblyBox.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.disassemblyBox.Location = new System.Drawing.Point(12, 12);
			this.disassemblyBox.Margin = new System.Windows.Forms.Padding(3, 3, 9, 3);
			this.disassemblyBox.Name = "disassemblyBox";
			this.disassemblyBox.Size = new System.Drawing.Size(823, 486);
			this.disassemblyBox.TabIndex = 0;
			this.disassemblyBox.VisibleDisasmOps = 34;
			// 
			// tmrUpdate
			// 
			this.tmrUpdate.Enabled = true;
			this.tmrUpdate.Interval = 15;
			this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
			// 
			// chkPause
			// 
			this.chkPause.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.chkPause.Appearance = System.Windows.Forms.Appearance.Button;
			this.chkPause.Location = new System.Drawing.Point(224, 504);
			this.chkPause.Name = "chkPause";
			this.chkPause.Size = new System.Drawing.Size(100, 25);
			this.chkPause.TabIndex = 3;
			this.chkPause.Text = "Paused";
			this.chkPause.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.chkPause.UseVisualStyleBackColor = true;
			this.chkPause.CheckedChanged += new System.EventHandler(this.chkPause_CheckedChanged);
			// 
			// chkTrace
			// 
			this.chkTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.chkTrace.Appearance = System.Windows.Forms.Appearance.Button;
			this.chkTrace.Location = new System.Drawing.Point(735, 504);
			this.chkTrace.Name = "chkTrace";
			this.chkTrace.Size = new System.Drawing.Size(100, 25);
			this.chkTrace.TabIndex = 4;
			this.chkTrace.Text = "Trace";
			this.chkTrace.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.chkTrace.UseVisualStyleBackColor = true;
			this.chkTrace.CheckedChanged += new System.EventHandler(this.chkTrace_CheckedChanged);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.Location = new System.Drawing.Point(847, 504);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(125, 25);
			this.btnClose.TabIndex = 7;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnStep
			// 
			this.btnStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnStep.Location = new System.Drawing.Point(12, 504);
			this.btnStep.Name = "btnStep";
			this.btnStep.Size = new System.Drawing.Size(100, 25);
			this.btnStep.TabIndex = 1;
			this.btnStep.Text = "Step";
			this.btnStep.UseVisualStyleBackColor = true;
			this.btnStep.Click += new System.EventHandler(this.btnStep_Click);
			// 
			// btnReset
			// 
			this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnReset.Location = new System.Drawing.Point(118, 504);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(100, 25);
			this.btnReset.TabIndex = 2;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// btnMemoryEditor
			// 
			this.btnMemoryEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnMemoryEditor.Location = new System.Drawing.Point(847, 218);
			this.btnMemoryEditor.Name = "btnMemoryEditor";
			this.btnMemoryEditor.Size = new System.Drawing.Size(125, 25);
			this.btnMemoryEditor.TabIndex = 6;
			this.btnMemoryEditor.Text = "Memory Editor";
			this.btnMemoryEditor.UseVisualStyleBackColor = true;
			this.btnMemoryEditor.Click += new System.EventHandler(this.btnMemoryEditor_Click);
			// 
			// debRegisters
			// 
			this.debRegisters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.debRegisters.BackColor = System.Drawing.SystemColors.Window;
			this.debRegisters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.debRegisters.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.debRegisters.Location = new System.Drawing.Point(847, 12);
			this.debRegisters.Name = "debRegisters";
			this.debRegisters.ReadData = null;
			this.debRegisters.Size = new System.Drawing.Size(125, 200);
			this.debRegisters.TabIndex = 5;
			this.debRegisters.WriteData = null;
			// 
			// DebuggerMainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(984, 541);
			this.Controls.Add(this.debRegisters);
			this.Controls.Add(this.btnMemoryEditor);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.btnStep);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.chkTrace);
			this.Controls.Add(this.chkPause);
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
		private System.Windows.Forms.CheckBox chkPause;
		private System.Windows.Forms.CheckBox chkTrace;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnStep;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.Button btnMemoryEditor;
		private WinForms.Controls.DataEditBox debRegisters;
	}
}
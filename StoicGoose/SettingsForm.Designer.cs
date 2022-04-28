
namespace StoicGoose
{
	partial class SettingsForm
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
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tvSettings = new StoicGoose.WinForms.Controls.TreeViewEx();
			this.tlpSettings = new StoicGoose.WinForms.Controls.TableLayoutPanelEx();
			this.lblNothing = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Location = new System.Drawing.Point(444, 425);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(86, 24);
			this.btnOk.TabIndex = 101;
			this.btnOk.Text = "&OK";
			this.btnOk.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(536, 425);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(86, 24);
			this.btnCancel.TabIndex = 102;
			this.btnCancel.Text = "&Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// tvSettings
			// 
			this.tvSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.tvSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.tvSettings.FullRowSelect = true;
			this.tvSettings.HideSelection = false;
			this.tvSettings.Location = new System.Drawing.Point(12, 12);
			this.tvSettings.Name = "tvSettings";
			this.tvSettings.ShowLines = false;
			this.tvSettings.Size = new System.Drawing.Size(150, 407);
			this.tvSettings.TabIndex = 0;
			this.tvSettings.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvSettings_BeforeSelect);
			// 
			// tlpSettings
			// 
			this.tlpSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tlpSettings.AutoSize = true;
			this.tlpSettings.ColumnCount = 2;
			this.tlpSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
			this.tlpSettings.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
			this.tlpSettings.ImeMode = System.Windows.Forms.ImeMode.On;
			this.tlpSettings.Location = new System.Drawing.Point(168, 12);
			this.tlpSettings.Name = "tlpSettings";
			this.tlpSettings.Padding = new System.Windows.Forms.Padding(3);
			this.tlpSettings.RowCount = 1;
			this.tlpSettings.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpSettings.Size = new System.Drawing.Size(454, 20);
			this.tlpSettings.TabIndex = 1;
			// 
			// lblNothing
			// 
			this.lblNothing.AutoSize = true;
			this.lblNothing.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.lblNothing.Location = new System.Drawing.Point(168, 12);
			this.lblNothing.Name = "lblNothing";
			this.lblNothing.Padding = new System.Windows.Forms.Padding(5, 8, 0, 0);
			this.lblNothing.Size = new System.Drawing.Size(27, 23);
			this.lblNothing.TabIndex = 104;
			this.lblNothing.Text = "---";
			// 
			// SettingsForm
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(634, 461);
			this.Controls.Add(this.tlpSettings);
			this.Controls.Add(this.tvSettings);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.lblNothing);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SettingsForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Settings";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private StoicGoose.WinForms.Controls.TreeViewEx tvSettings;
		private StoicGoose.WinForms.Controls.TableLayoutPanelEx tlpSettings;
		private System.Windows.Forms.Label lblNothing;
	}
}
namespace StoicGoose.WinForms
{
	partial class CheatEditForm
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
			this.chkEnabled = new System.Windows.Forms.CheckBox();
			this.lblAddress = new System.Windows.Forms.Label();
			this.txtAddress = new System.Windows.Forms.TextBox();
			this.cmbCondition = new System.Windows.Forms.ComboBox();
			this.lblCondition = new System.Windows.Forms.Label();
			this.lblCompareValue = new System.Windows.Forms.Label();
			this.txtCompareValue = new System.Windows.Forms.TextBox();
			this.lblPatchedValue = new System.Windows.Forms.Label();
			this.txtPatchedValue = new System.Windows.Forms.TextBox();
			this.lblDescription = new System.Windows.Forms.Label();
			this.txtDescription = new System.Windows.Forms.TextBox();
			this.lblExplanation = new System.Windows.Forms.Label();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnConfirm = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// chkEnabled
			// 
			this.chkEnabled.AutoSize = true;
			this.chkEnabled.Location = new System.Drawing.Point(12, 12);
			this.chkEnabled.Name = "chkEnabled";
			this.chkEnabled.Size = new System.Drawing.Size(73, 19);
			this.chkEnabled.TabIndex = 0;
			this.chkEnabled.Text = "Enabled?";
			this.chkEnabled.UseVisualStyleBackColor = true;
			// 
			// lblAddress
			// 
			this.lblAddress.AutoSize = true;
			this.lblAddress.Location = new System.Drawing.Point(12, 40);
			this.lblAddress.Name = "lblAddress";
			this.lblAddress.Size = new System.Drawing.Size(49, 15);
			this.lblAddress.TabIndex = 1;
			this.lblAddress.Text = "Address";
			// 
			// txtAddress
			// 
			this.txtAddress.Location = new System.Drawing.Point(132, 37);
			this.txtAddress.Name = "txtAddress";
			this.txtAddress.Size = new System.Drawing.Size(240, 23);
			this.txtAddress.TabIndex = 2;
			// 
			// cmbCondition
			// 
			this.cmbCondition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbCondition.FormattingEnabled = true;
			this.cmbCondition.Location = new System.Drawing.Point(132, 66);
			this.cmbCondition.Name = "cmbCondition";
			this.cmbCondition.Size = new System.Drawing.Size(240, 23);
			this.cmbCondition.TabIndex = 3;
			// 
			// lblCondition
			// 
			this.lblCondition.AutoSize = true;
			this.lblCondition.Location = new System.Drawing.Point(12, 69);
			this.lblCondition.Name = "lblCondition";
			this.lblCondition.Size = new System.Drawing.Size(60, 15);
			this.lblCondition.TabIndex = 4;
			this.lblCondition.Text = "Condition";
			// 
			// lblCompareValue
			// 
			this.lblCompareValue.AutoSize = true;
			this.lblCompareValue.Location = new System.Drawing.Point(12, 98);
			this.lblCompareValue.Name = "lblCompareValue";
			this.lblCompareValue.Size = new System.Drawing.Size(87, 15);
			this.lblCompareValue.TabIndex = 5;
			this.lblCompareValue.Text = "Compare Value";
			// 
			// txtCompareValue
			// 
			this.txtCompareValue.Location = new System.Drawing.Point(132, 95);
			this.txtCompareValue.Name = "txtCompareValue";
			this.txtCompareValue.Size = new System.Drawing.Size(100, 23);
			this.txtCompareValue.TabIndex = 6;
			// 
			// lblPatchedValue
			// 
			this.lblPatchedValue.AutoSize = true;
			this.lblPatchedValue.Location = new System.Drawing.Point(12, 127);
			this.lblPatchedValue.Name = "lblPatchedValue";
			this.lblPatchedValue.Size = new System.Drawing.Size(81, 15);
			this.lblPatchedValue.TabIndex = 7;
			this.lblPatchedValue.Text = "Patched Value";
			// 
			// txtPatchedValue
			// 
			this.txtPatchedValue.Location = new System.Drawing.Point(132, 124);
			this.txtPatchedValue.Name = "txtPatchedValue";
			this.txtPatchedValue.Size = new System.Drawing.Size(100, 23);
			this.txtPatchedValue.TabIndex = 8;
			// 
			// lblDescription
			// 
			this.lblDescription.AutoSize = true;
			this.lblDescription.Location = new System.Drawing.Point(12, 156);
			this.lblDescription.Name = "lblDescription";
			this.lblDescription.Size = new System.Drawing.Size(67, 15);
			this.lblDescription.TabIndex = 9;
			this.lblDescription.Text = "Description";
			// 
			// txtDescription
			// 
			this.txtDescription.Location = new System.Drawing.Point(132, 153);
			this.txtDescription.Name = "txtDescription";
			this.txtDescription.Size = new System.Drawing.Size(240, 23);
			this.txtDescription.TabIndex = 10;
			// 
			// lblExplanation
			// 
			this.lblExplanation.Location = new System.Drawing.Point(12, 179);
			this.lblExplanation.Name = "lblExplanation";
			this.lblExplanation.Padding = new System.Windows.Forms.Padding(0, 7, 0, 0);
			this.lblExplanation.Size = new System.Drawing.Size(360, 44);
			this.lblExplanation.TabIndex = 11;
			this.lblExplanation.Text = "---\r\n---\r\n";
			// 
			// btnClose
			// 
			this.btnClose.Location = new System.Drawing.Point(297, 226);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 12;
			this.btnClose.Text = "&Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnConfirm
			// 
			this.btnConfirm.Location = new System.Drawing.Point(12, 226);
			this.btnConfirm.Name = "btnConfirm";
			this.btnConfirm.Size = new System.Drawing.Size(75, 23);
			this.btnConfirm.TabIndex = 13;
			this.btnConfirm.Text = "---";
			this.btnConfirm.UseVisualStyleBackColor = true;
			this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
			// 
			// CheatEditForm
			// 
			this.AcceptButton = this.btnConfirm;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(384, 261);
			this.Controls.Add(this.btnConfirm);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.lblExplanation);
			this.Controls.Add(this.txtDescription);
			this.Controls.Add(this.lblDescription);
			this.Controls.Add(this.txtPatchedValue);
			this.Controls.Add(this.lblPatchedValue);
			this.Controls.Add(this.txtCompareValue);
			this.Controls.Add(this.lblCompareValue);
			this.Controls.Add(this.lblCondition);
			this.Controls.Add(this.cmbCondition);
			this.Controls.Add(this.txtAddress);
			this.Controls.Add(this.lblAddress);
			this.Controls.Add(this.chkEnabled);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CheatEditForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "---";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkEnabled;
		private System.Windows.Forms.Label lblAddress;
		private System.Windows.Forms.TextBox txtAddress;
		private System.Windows.Forms.ComboBox cmbCondition;
		private System.Windows.Forms.Label lblCondition;
		private System.Windows.Forms.Label lblCompareValue;
		private System.Windows.Forms.TextBox txtCompareValue;
		private System.Windows.Forms.Label lblPatchedValue;
		private System.Windows.Forms.TextBox txtPatchedValue;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.TextBox txtDescription;
		private System.Windows.Forms.Label lblExplanation;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnConfirm;
	}
}
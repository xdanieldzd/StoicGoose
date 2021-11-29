
namespace StoicGoose
{
	partial class MemoryEditorForm
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
			this.hexEditBox = new StoicGoose.WinForms.Controls.HexEditBox();
			this.tmrUpdate = new System.Windows.Forms.Timer(this.components);
			this.btnClose = new System.Windows.Forms.Button();
			this.vsbLocation = new System.Windows.Forms.VScrollBar();
			this.SuspendLayout();
			// 
			// hexEditBox
			// 
			this.hexEditBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.hexEditBox.AutoScroll = true;
			this.hexEditBox.BackColor = System.Drawing.SystemColors.Window;
			this.hexEditBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.hexEditBox.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.hexEditBox.Location = new System.Drawing.Point(12, 12);
			this.hexEditBox.Name = "hexEditBox";
			this.hexEditBox.OffsetBytes = 3;
			this.hexEditBox.OffsetMask = ((uint)(1048575u));
			this.hexEditBox.ReadByte = null;
			this.hexEditBox.Size = new System.Drawing.Size(540, 308);
			this.hexEditBox.TabIndex = 0;
			this.hexEditBox.WriteByte = null;
			this.hexEditBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.hexEditBox_KeyDown);
			// 
			// tmrUpdate
			// 
			this.tmrUpdate.Enabled = true;
			this.tmrUpdate.Interval = 15;
			this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.Location = new System.Drawing.Point(478, 326);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(94, 23);
			this.btnClose.TabIndex = 2;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// vsbLocation
			// 
			this.vsbLocation.LargeChange = 16;
			this.vsbLocation.Location = new System.Drawing.Point(555, 12);
			this.vsbLocation.Maximum = 65535;
			this.vsbLocation.Name = "vsbLocation";
			this.vsbLocation.Size = new System.Drawing.Size(17, 308);
			this.vsbLocation.TabIndex = 3;
			this.vsbLocation.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vsbLocation_Scroll);
			// 
			// MemoryEditorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(584, 361);
			this.Controls.Add(this.vsbLocation);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.hexEditBox);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MemoryEditorForm";
			this.ShowIcon = false;
			this.Text = "Memory Editor";
			this.ResumeLayout(false);

		}

		#endregion

		private WinForms.Controls.HexEditBox hexEditBox;
		private System.Windows.Forms.Timer tmrUpdate;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.VScrollBar vsbLocation;
	}
}

namespace StoicGoose
{
	partial class SoundRecorderForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SoundRecorderForm));
			this.btnStart = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.ftbWaveFile = new StoicGoose.WinForms.Controls.FileTextBox();
			this.btnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnStart
			// 
			this.btnStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnStart.Enabled = false;
			this.btnStart.Location = new System.Drawing.Point(12, 42);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(75, 23);
			this.btnStart.TabIndex = 1;
			this.btnStart.Text = "&Start";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// btnStop
			// 
			this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnStop.Enabled = false;
			this.btnStop.Location = new System.Drawing.Point(93, 42);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(75, 23);
			this.btnStop.TabIndex = 2;
			this.btnStop.Text = "S&top";
			this.btnStop.UseVisualStyleBackColor = true;
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// ftbWaveFile
			// 
			this.ftbWaveFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ftbWaveFile.DialogMode = StoicGoose.WinForms.Controls.FileTextBoxDialogMode.Save;
			this.ftbWaveFile.FileName = "";
			this.ftbWaveFile.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
			this.ftbWaveFile.InitialDirectory = "";
			this.ftbWaveFile.Location = new System.Drawing.Point(12, 12);
			this.ftbWaveFile.Name = "ftbWaveFile";
			this.ftbWaveFile.Size = new System.Drawing.Size(300, 24);
			this.ftbWaveFile.TabIndex = 4;
			this.ftbWaveFile.FileSelected += new System.EventHandler(this.ftbWaveFile_FileSelected);
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.Location = new System.Drawing.Point(237, 42);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 5;
			this.btnClose.Text = "&Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// SoundRecorderForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(324, 77);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.ftbWaveFile);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.btnStart);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "SoundRecorderForm";
			this.Text = "Sound Recorder";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SoundRecorderForm_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.Button btnStop;
		private WinForms.Controls.FileTextBox ftbWaveFile;
		private System.Windows.Forms.Button btnClose;
	}
}
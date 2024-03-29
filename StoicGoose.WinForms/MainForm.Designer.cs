﻿namespace StoicGoose.WinForms
{
	partial class MainForm
	{
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openROMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWAVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.emulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.screenSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.shadersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.muteSoundToolStripMenuItem = new StoicGoose.WinForms.Windows.BindableToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.limitFPSToolStripMenuItem = new StoicGoose.WinForms.Windows.BindableToolStripMenuItem();
			this.rotateScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cheatsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableCheatsToolStripMenuItem = new StoicGoose.WinForms.Windows.BindableToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.cheatListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.tsslEmulationStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.ofdOpenRom = new System.Windows.Forms.OpenFileDialog();
			this.sfdSaveWav = new System.Windows.Forms.SaveFileDialog();
			this.renderControl = new StoicGoose.WinForms.OpenGL.RenderControl();
			this.openDataFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.menuStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.emulationToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.cheatsToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip.Size = new System.Drawing.Size(448, 24);
			this.menuStrip.TabIndex = 2;
			this.menuStrip.Text = "---";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openROMToolStripMenuItem,
            this.saveWAVToolStripMenuItem,
            this.toolStripSeparator1,
            this.recentFilesToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openROMToolStripMenuItem
			// 
			this.openROMToolStripMenuItem.Name = "openROMToolStripMenuItem";
			this.openROMToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openROMToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.openROMToolStripMenuItem.Text = "&Open ROM...";
			this.openROMToolStripMenuItem.Click += new System.EventHandler(this.loadROMToolStripMenuItem_Click);
			// 
			// saveWAVToolStripMenuItem
			// 
			this.saveWAVToolStripMenuItem.Name = "saveWAVToolStripMenuItem";
			this.saveWAVToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.saveWAVToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.saveWAVToolStripMenuItem.Text = "&Save WAV...";
			this.saveWAVToolStripMenuItem.Click += new System.EventHandler(this.saveWAVToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(184, 6);
			// 
			// recentFilesToolStripMenuItem
			// 
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.recentFilesToolStripMenuItem.Text = "&Recent Files";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(184, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// emulationToolStripMenuItem
			// 
			this.emulationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pauseToolStripMenuItem,
            this.resetToolStripMenuItem});
			this.emulationToolStripMenuItem.Name = "emulationToolStripMenuItem";
			this.emulationToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.emulationToolStripMenuItem.Size = new System.Drawing.Size(73, 20);
			this.emulationToolStripMenuItem.Text = "&Emulation";
			// 
			// pauseToolStripMenuItem
			// 
			this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
			this.pauseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.pauseToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.pauseToolStripMenuItem.Text = "&Pause";
			this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
			// 
			// resetToolStripMenuItem
			// 
			this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
			this.resetToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
			this.resetToolStripMenuItem.Size = new System.Drawing.Size(148, 22);
			this.resetToolStripMenuItem.Text = "&Reset";
			this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.screenSizeToolStripMenuItem,
            this.shadersToolStripMenuItem,
            this.toolStripSeparator3,
            this.muteSoundToolStripMenuItem,
            this.toolStripSeparator4,
            this.limitFPSToolStripMenuItem,
            this.rotateScreenToolStripMenuItem,
            this.toolStripSeparator5,
            this.settingsToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// screenSizeToolStripMenuItem
			// 
			this.screenSizeToolStripMenuItem.Name = "screenSizeToolStripMenuItem";
			this.screenSizeToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.screenSizeToolStripMenuItem.Text = "S&creen Size";
			// 
			// shadersToolStripMenuItem
			// 
			this.shadersToolStripMenuItem.Name = "shadersToolStripMenuItem";
			this.shadersToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.shadersToolStripMenuItem.Text = "&Shaders";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(143, 6);
			// 
			// muteSoundToolStripMenuItem
			// 
			this.muteSoundToolStripMenuItem.CheckOnClick = true;
			this.muteSoundToolStripMenuItem.Name = "muteSoundToolStripMenuItem";
			this.muteSoundToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.muteSoundToolStripMenuItem.Text = "&Mute Sound";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(143, 6);
			// 
			// limitFPSToolStripMenuItem
			// 
			this.limitFPSToolStripMenuItem.CheckOnClick = true;
			this.limitFPSToolStripMenuItem.Name = "limitFPSToolStripMenuItem";
			this.limitFPSToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.limitFPSToolStripMenuItem.Text = "&Limit FPS";
			// 
			// rotateScreenToolStripMenuItem
			// 
			this.rotateScreenToolStripMenuItem.Name = "rotateScreenToolStripMenuItem";
			this.rotateScreenToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.rotateScreenToolStripMenuItem.Text = "&Rotate Screen";
			this.rotateScreenToolStripMenuItem.Click += new System.EventHandler(this.rotateScreenToolStripMenuItem_Click);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(143, 6);
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.settingsToolStripMenuItem.Text = "Se&ttings";
			this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
			// 
			// cheatsToolStripMenuItem
			// 
			this.cheatsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.enableCheatsToolStripMenuItem,
            this.toolStripSeparator6,
            this.cheatListToolStripMenuItem});
			this.cheatsToolStripMenuItem.Name = "cheatsToolStripMenuItem";
			this.cheatsToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
			this.cheatsToolStripMenuItem.Text = "&Cheats";
			// 
			// enableCheatsToolStripMenuItem
			// 
			this.enableCheatsToolStripMenuItem.CheckOnClick = true;
			this.enableCheatsToolStripMenuItem.Name = "enableCheatsToolStripMenuItem";
			this.enableCheatsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.enableCheatsToolStripMenuItem.Text = "Enable &Cheats";
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(177, 6);
			// 
			// cheatListToolStripMenuItem
			// 
			this.cheatListToolStripMenuItem.Name = "cheatListToolStripMenuItem";
			this.cheatListToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.cheatListToolStripMenuItem.Text = "&Cheat List";
			this.cheatListToolStripMenuItem.Click += new System.EventHandler(this.cheatListToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openDataFolderToolStripMenuItem,
            this.toolStripMenuItem1,
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.aboutToolStripMenuItem.Text = "&About...";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus,
            this.tsslEmulationStatus});
			this.statusStrip.Location = new System.Drawing.Point(0, 312);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this.statusStrip.Size = new System.Drawing.Size(448, 22);
			this.statusStrip.TabIndex = 3;
			this.statusStrip.Text = "statusStrip1";
			// 
			// tsslStatus
			// 
			this.tsslStatus.Name = "tsslStatus";
			this.tsslStatus.Size = new System.Drawing.Size(409, 17);
			this.tsslStatus.Spring = true;
			this.tsslStatus.Text = "---";
			this.tsslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tsslEmulationStatus
			// 
			this.tsslEmulationStatus.Name = "tsslEmulationStatus";
			this.tsslEmulationStatus.Size = new System.Drawing.Size(22, 17);
			this.tsslEmulationStatus.Text = "---";
			this.tsslEmulationStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// sfdSaveWav
			// 
			this.sfdSaveWav.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
			// 
			// renderControl
			// 
			this.renderControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
			this.renderControl.APIVersion = new System.Version(2, 1, 0, 0);
			this.renderControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.renderControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
			this.renderControl.IsEventDriven = true;
			this.renderControl.Location = new System.Drawing.Point(0, 24);
			this.renderControl.Name = "renderControl";
			this.renderControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
			this.renderControl.Size = new System.Drawing.Size(448, 288);
			this.renderControl.TabIndex = 4;
			// 
			// openDataFolderToolStripMenuItem
			// 
			this.openDataFolderToolStripMenuItem.Name = "openDataFolderToolStripMenuItem";
			this.openDataFolderToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.openDataFolderToolStripMenuItem.Text = "Open &Data Folder...";
			this.openDataFolderToolStripMenuItem.Click += new System.EventHandler(this.openDataFolderToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(177, 6);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(448, 334);
			this.Controls.Add(this.renderControl);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.MinimumSize = new System.Drawing.Size(464, 373);
			this.Name = "MainForm";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openROMToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWAVToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog ofdOpenRom;
		private System.Windows.Forms.SaveFileDialog sfdSaveWav;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel tsslEmulationStatus;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem screenSizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem emulationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pauseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rotateScreenToolStripMenuItem;
		private StoicGoose.WinForms.Windows.BindableToolStripMenuItem limitFPSToolStripMenuItem;
		private StoicGoose.WinForms.OpenGL.RenderControl renderControl;
		private System.Windows.Forms.ToolStripMenuItem shadersToolStripMenuItem;
		private StoicGoose.WinForms.Windows.BindableToolStripMenuItem muteSoundToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cheatsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cheatListToolStripMenuItem;
		private StoicGoose.WinForms.Windows.BindableToolStripMenuItem enableCheatsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem openDataFolderToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
	}
}


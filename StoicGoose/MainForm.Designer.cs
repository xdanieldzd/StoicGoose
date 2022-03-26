﻿namespace StoicGoose
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
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.emulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.screenSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.shadersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.muteSoundToolStripMenuItem = new StoicGoose.WinForms.BindableToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.limitFPSToolStripMenuItem = new StoicGoose.WinForms.BindableToolStripMenuItem();
			this.rotateScreenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cheatsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enableCheatsToolStripMenuItem = new StoicGoose.WinForms.BindableToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			this.cheatListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.systemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cPUToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.displayControllerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.logWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
			this.traceLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.tsslEmulationStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.ofdOpenRom = new System.Windows.Forms.OpenFileDialog();
			this.renderControl = new StoicGoose.OpenGL.RenderControl();
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
            this.debugToolStripMenuItem,
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
            this.toolStripMenuItem1,
            this.recentFilesToolStripMenuItem,
            this.toolStripMenuItem2,
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
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(184, 6);
			// 
			// recentFilesToolStripMenuItem
			// 
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.recentFilesToolStripMenuItem.Text = "&Recent Files";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(184, 6);
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
            this.toolStripMenuItem3,
            this.muteSoundToolStripMenuItem,
            this.toolStripMenuItem4,
            this.limitFPSToolStripMenuItem,
            this.rotateScreenToolStripMenuItem,
            this.toolStripMenuItem5,
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
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(143, 6);
			// 
			// muteSoundToolStripMenuItem
			// 
			this.muteSoundToolStripMenuItem.CheckOnClick = true;
			this.muteSoundToolStripMenuItem.Name = "muteSoundToolStripMenuItem";
			this.muteSoundToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.muteSoundToolStripMenuItem.Text = "&Mute Sound";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(143, 6);
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
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(143, 6);
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
            this.toolStripMenuItem7,
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
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			this.toolStripMenuItem7.Size = new System.Drawing.Size(177, 6);
			// 
			// cheatListToolStripMenuItem
			// 
			this.cheatListToolStripMenuItem.Name = "cheatListToolStripMenuItem";
			this.cheatListToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.cheatListToolStripMenuItem.Text = "&Cheat List";
			this.cheatListToolStripMenuItem.Click += new System.EventHandler(this.cheatListToolStripMenuItem_Click);
			// 
			// debugToolStripMenuItem
			// 
			this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusWindowToolStripMenuItem,
            this.logWindowToolStripMenuItem,
            this.toolStripMenuItem6,
            this.traceLogToolStripMenuItem});
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.debugToolStripMenuItem.Text = "&Debug";
			// 
			// statusWindowToolStripMenuItem
			// 
			this.statusWindowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.systemToolStripMenuItem,
            this.cPUToolStripMenuItem,
            this.displayControllerToolStripMenuItem});
			this.statusWindowToolStripMenuItem.Name = "statusWindowToolStripMenuItem";
			this.statusWindowToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
			this.statusWindowToolStripMenuItem.Text = "&Status Window...";
			// 
			// systemToolStripMenuItem
			// 
			this.systemToolStripMenuItem.Name = "systemToolStripMenuItem";
			this.systemToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.systemToolStripMenuItem.Text = "&System";
			this.systemToolStripMenuItem.Click += new System.EventHandler(this.systemToolStripMenuItem_Click);
			// 
			// cPUToolStripMenuItem
			// 
			this.cPUToolStripMenuItem.Name = "cPUToolStripMenuItem";
			this.cPUToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.cPUToolStripMenuItem.Text = "&CPU";
			this.cPUToolStripMenuItem.Click += new System.EventHandler(this.cPUToolStripMenuItem_Click);
			// 
			// displayControllerToolStripMenuItem
			// 
			this.displayControllerToolStripMenuItem.Name = "displayControllerToolStripMenuItem";
			this.displayControllerToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
			this.displayControllerToolStripMenuItem.Text = "&Display Controller";
			this.displayControllerToolStripMenuItem.Click += new System.EventHandler(this.displayControllerToolStripMenuItem_Click);
			// 
			// logWindowToolStripMenuItem
			// 
			this.logWindowToolStripMenuItem.Name = "logWindowToolStripMenuItem";
			this.logWindowToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
			this.logWindowToolStripMenuItem.Text = "&Log Window";
			this.logWindowToolStripMenuItem.Click += new System.EventHandler(this.logWindowToolStripMenuItem_Click);
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			this.toolStripMenuItem6.Size = new System.Drawing.Size(159, 6);
			// 
			// traceLogToolStripMenuItem
			// 
			this.traceLogToolStripMenuItem.Name = "traceLogToolStripMenuItem";
			this.traceLogToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
			this.traceLogToolStripMenuItem.Text = "&Trace Log";
			this.traceLogToolStripMenuItem.Click += new System.EventHandler(this.traceLogToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.AsNeeded;
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
			this.aboutToolStripMenuItem.Text = "&About...";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus,
            this.tsslEmulationStatus});
			this.statusStrip.Location = new System.Drawing.Point(0, 279);
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
			// renderControl
			// 
			this.renderControl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
			this.renderControl.APIVersion = new System.Version(4, 6, 0, 0);
			this.renderControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.renderControl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
			this.renderControl.IsEventDriven = true;
			this.renderControl.Location = new System.Drawing.Point(0, 24);
			this.renderControl.Name = "renderControl";
			this.renderControl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
			this.renderControl.Size = new System.Drawing.Size(448, 255);
			this.renderControl.TabIndex = 4;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(448, 301);
			this.Controls.Add(this.renderControl);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "MainForm";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.Layout += new System.Windows.Forms.LayoutEventHandler(this.MainForm_Layout);
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
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog ofdOpenRom;
		private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel tsslEmulationStatus;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem screenSizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem emulationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pauseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rotateScreenToolStripMenuItem;
		private StoicGoose.WinForms.BindableToolStripMenuItem limitFPSToolStripMenuItem;
		private StoicGoose.OpenGL.RenderControl renderControl;
		private System.Windows.Forms.ToolStripMenuItem shadersToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private StoicGoose.WinForms.BindableToolStripMenuItem muteSoundToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem traceLogToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem statusWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem systemToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cPUToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem displayControllerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem logWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
		private System.Windows.Forms.ToolStripMenuItem cheatsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cheatListToolStripMenuItem;
		private StoicGoose.WinForms.BindableToolStripMenuItem enableCheatsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
	}
}


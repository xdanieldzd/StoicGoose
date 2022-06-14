namespace StoicGoose.WinForms
{
	partial class CheatsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CheatsForm));
			this.btnClose = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnDelete = new System.Windows.Forms.Button();
			this.clvCheats = new StoicGoose.WinForms.Windows.Controls.CheatsListView();
			this.SuspendLayout();
			// 
			// btnClose
			// 
			this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnClose.Location = new System.Drawing.Point(713, 415);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 6;
			this.btnClose.Text = "&Close";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnAdd.Location = new System.Drawing.Point(12, 415);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(75, 23);
			this.btnAdd.TabIndex = 7;
			this.btnAdd.Text = "&Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnEdit.Location = new System.Drawing.Point(93, 415);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(75, 23);
			this.btnEdit.TabIndex = 8;
			this.btnEdit.Text = "&Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDelete.Location = new System.Drawing.Point(174, 415);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(75, 23);
			this.btnDelete.TabIndex = 9;
			this.btnDelete.Text = "&Delete";
			this.btnDelete.UseVisualStyleBackColor = true;
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// clvCheats
			// 
			this.clvCheats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.clvCheats.FullRowSelect = true;
			this.clvCheats.Location = new System.Drawing.Point(12, 12);
			this.clvCheats.MultiSelect = false;
			this.clvCheats.Name = "clvCheats";
			this.clvCheats.Size = new System.Drawing.Size(776, 397);
			this.clvCheats.TabIndex = 10;
			this.clvCheats.UseCompatibleStateImageBehavior = false;
			this.clvCheats.View = System.Windows.Forms.View.Details;
			this.clvCheats.SelectedIndexChanged += new System.EventHandler(this.clvCheats_SelectedIndexChanged);
			this.clvCheats.DoubleClick += new System.EventHandler(this.clvCheats_DoubleClick);
			// 
			// CheatsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.clvCheats);
			this.Controls.Add(this.btnDelete);
			this.Controls.Add(this.btnEdit);
			this.Controls.Add(this.btnAdd);
			this.Controls.Add(this.btnClose);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "CheatsForm";
			this.Text = "Cheats";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CheatsForm_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnDelete;
		private Windows.Controls.CheatsListView clvCheats;
	}
}
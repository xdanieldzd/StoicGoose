using System;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Controls
{
	public partial class FileTextBox : UserControl
	{
		public string FileName { get; set; } = string.Empty;
		public string InitialDirectory { get; set; } = string.Empty;
		public string Filter { get; set; } = string.Empty;

		public FileTextBox()
		{
			InitializeComponent();

			/* force to correct size */
			txtPath.AutoSize = false;
			txtPath.Height = btnBrowse.Height;

			txtPath.DataBindings.Add(nameof(TextBox.Text), this, nameof(FileName), false, DataSourceUpdateMode.OnPropertyChanged);
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			ofdOpen.FileName = FileName;
			ofdOpen.InitialDirectory = InitialDirectory;
			ofdOpen.Filter = Filter;

			if (ofdOpen.ShowDialog() == DialogResult.OK)
				txtPath.Text = ofdOpen.FileName;
		}
	}
}

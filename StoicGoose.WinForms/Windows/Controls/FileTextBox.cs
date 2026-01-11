using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Windows.Controls
{
    public enum FileTextBoxDialogMode { Open, Save }

    public partial class FileTextBox : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FileTextBoxDialogMode DialogMode { get; set; } = FileTextBoxDialogMode.Open;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FileName { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InitialDirectory { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Filter { get; set; } = string.Empty;

        public bool IsFileSelected => !string.IsNullOrEmpty(FileName);

        public event EventHandler FileSelected;
        public void OnFileSelected(EventArgs e) => FileSelected?.Invoke(this, e);

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
            FileDialog fileDialog = DialogMode switch
            {
                FileTextBoxDialogMode.Open => ofdOpen,
                FileTextBoxDialogMode.Save => sfdSave,
                _ => throw new Exception("Invalid dialog mode"),
            };
            fileDialog.FileName = FileName;
            fileDialog.InitialDirectory = InitialDirectory;
            fileDialog.Filter = Filter;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fileDialog.FileName;
                OnFileSelected(EventArgs.Empty);
            }
        }
    }
}

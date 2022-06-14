using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace StoicGoose.WinForms
{
	public partial class CheatsForm : Form
	{
		readonly CheatEditForm cheatEditForm = new();

		public Action<Cheat[]> Callback { get; set; } = default;

		public CheatsForm()
		{
			InitializeComponent();

			UpdateControls();
		}

		private void CheatsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			Callback?.Invoke(clvCheats.Items.Cast<ListViewItem>().Select(x => (Cheat)x.Tag).ToArray());
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			Hide();

			Callback?.Invoke(clvCheats.Items.Cast<ListViewItem>().Select(x => (Cheat)x.Tag).ToArray());
		}

		private void clvCheats_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateControls();
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			cheatEditForm.SetFormAddMode(true);
			cheatEditForm.SetCheat(new());

			if (cheatEditForm.ShowDialog() == DialogResult.OK)
				clvCheats.Add(cheatEditForm.Cheat);

			UpdateControls();
		}

		private void btnEdit_Click(object sender, EventArgs e)
		{
			var selectedCheat = clvCheats.GetSelectedItems().First();

			cheatEditForm.SetFormAddMode(false);
			cheatEditForm.SetCheat(selectedCheat);

			if (cheatEditForm.ShowDialog() == DialogResult.OK)
				clvCheats.Replace(selectedCheat, cheatEditForm.Cheat);

			UpdateControls();
		}

		private void btnDelete_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Do you really want to delete this cheat?", "Delete Cheat", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				var selected = clvCheats.GetSelectedItems().First();
				clvCheats.Remove(selected);
			}

			UpdateControls();
		}

		public void SetCheatList(Cheat[] cheats)
		{
			clvCheats.RemoveAll();
			clvCheats.AddRange(cheats);

			UpdateControls();
		}

		private void UpdateControls()
		{
			btnDelete.Enabled = clvCheats.Items.Count != 0 && clvCheats.SelectedItems.Count != 0;
			btnEdit.Enabled = clvCheats.SelectedItems.Count != 0;
			btnAdd.Enabled = true;
		}

		private void clvCheats_DoubleClick(object sender, EventArgs e)
		{
			if (clvCheats.SelectedIndices.Count != 0)
				btnEdit_Click(btnEdit, EventArgs.Empty);
		}
	}
}

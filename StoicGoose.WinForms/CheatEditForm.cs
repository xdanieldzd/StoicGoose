using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace StoicGoose.WinForms
{
	public partial class CheatEditForm : Form
	{
		readonly Dictionary<CheatCondition, string> cheatConditionDescriptive = new()
		{
			{ CheatCondition.Always, "^ (always)" },
			{ CheatCondition.LessThan, "< (less than)" },
			{ CheatCondition.LessThanOrEqual, "<= (less or equal)" },
			{ CheatCondition.GreaterThanOrEqual, "=> (greater or equal)" },
			{ CheatCondition.GreaterThan, "> (greater than)" },
		};

		readonly string[] cheatConditionNames = new string[] { "always", "less than", "less or equal", "greater or equal", "greater than" };

		readonly ConvertEventHandler addressFormatter = default, compareValueFormatter = default, patchedValueFormatter = default;
		readonly ConvertEventHandler addressParser = default, compareValueParser = default, patchedValueParser = default;

		public Cheat Cheat { get; private set; } = new();

		bool isAddMode = false;

		public CheatEditForm()
		{
			InitializeComponent();

			addressFormatter = (s, e) => { if (e.DesiredType == typeof(string)) { e.Value = $"0x{e.Value:X6}"; UpdateExplanation(); } };
			addressParser = (s, e) => { if (e.DesiredType == typeof(uint)) { if (uint.TryParse(((string)e.Value).Replace("0x", ""), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint result)) e.Value = result; } };

			cmbCondition.DataSource = Enum.GetValues(typeof(CheatCondition)).Cast<CheatCondition>().Select(x => new { Value = x, Description = cheatConditionDescriptive[x] }).ToList();
			cmbCondition.DisplayMember = "Description";
			cmbCondition.ValueMember = "Value";
			cmbCondition.SelectedValueChanged += (s, e) => UpdateExplanation();

			compareValueFormatter = (s, e) => { if (e.DesiredType == typeof(string)) { e.Value = $"0x{e.Value:X2}"; UpdateExplanation(); } };
			compareValueParser = (s, e) => { if (e.DesiredType == typeof(byte)) { if (byte.TryParse(((string)e.Value).Replace("0x", ""), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte result)) e.Value = result; } };

			patchedValueFormatter = (s, e) => { if (e.DesiredType == typeof(string)) { e.Value = $"0x{e.Value:X2}"; UpdateExplanation(); } };
			patchedValueParser = (s, e) => { if (e.DesiredType == typeof(byte)) { if (byte.TryParse(((string)e.Value).Replace("0x", ""), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte result)) e.Value = result; } };
		}

		private void btnConfirm_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void btnClose_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void InitializeDataBindings()
		{
			SetBinding(chkEnabled, nameof(CheckBox.Checked), nameof(Cheat.IsEnabled), false);
			SetBinding(txtAddress, nameof(TextBox.Text), nameof(Cheat.Address), true, addressFormatter, addressParser);
			SetBinding(cmbCondition, nameof(ComboBox.SelectedValue), nameof(Cheat.Condition), false);
			SetBinding(txtCompareValue, nameof(TextBox.Text), nameof(Cheat.CompareValue), true, compareValueFormatter, compareValueParser);
			SetBinding(txtPatchedValue, nameof(TextBox.Text), nameof(Cheat.PatchedValue), true, patchedValueFormatter, patchedValueParser);
			SetBinding(txtDescription, nameof(TextBox.Text), nameof(Cheat.Description), false);
		}

		private void SetBinding(Control control, string propertyName, string dataMember, bool formattingEnabled, ConvertEventHandler format = null, ConvertEventHandler parse = null)
		{
			control.DataBindings.Clear();
			var binding = new Binding(propertyName, Cheat, dataMember, formattingEnabled, DataSourceUpdateMode.OnPropertyChanged);
			if (formattingEnabled) binding.Format += format;
			binding.Parse += parse;

			control.DataBindings.Add(binding);
		}

		private void UpdateExplanation()
		{
			if (Cheat.Condition != CheatCondition.Always)
				lblExplanation.Text = $"If value at 0x{Cheat.Address:X6} is {cheatConditionNames[(int)Cheat.Condition]} 0x{Cheat.CompareValue:X2}, patch to 0x{Cheat.PatchedValue:X2}.";
			else
				lblExplanation.Text = $"Always patch value at 0x{Cheat.Address:X6} to 0x{Cheat.PatchedValue:X2}.";
		}

		public void SetFormAddMode(bool isAdd)
		{
			isAddMode = isAdd;

			Text = $"{(isAddMode ? "Add" : "Edit")} Cheat";
			btnConfirm.Text = isAddMode ? "Add" : "Edit";
		}

		public void SetCheat(Cheat cheat)
		{
			Cheat = cheat;

			InitializeDataBindings();
			UpdateExplanation();
		}
	}
}

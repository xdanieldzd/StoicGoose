using System;
using System.Collections.Generic;

namespace StoicGoose.WinForms.Windows.Controls
{
	public class CheatsListView : ListViewEx<Cheat>
	{
		readonly static string[] cheatConditionSymbols = new string[] { "^", "<", "<=", "=>", ">" };

		readonly static List<(string ColumnName, Func<Cheat, object> ValueLookup, Func<Cheat, string> DisplayStringLookup)> columnMappingForCheat = new()
		{
			("Address", c => c.Address, c => $"0x{c.Address:X6}"),
			("Condition", c => c.Condition, c => cheatConditionSymbols[(int)c.Condition]),
			("Compare", c => c.CompareValue, c => $"0x{c.CompareValue:X2}"),
			(string.Empty, c => 0, c => "="),
			("Patch", c => c.PatchedValue, c => $"0x{c.PatchedValue:X2}"),
			("Description", c => c.Description, c => c.Description)
		};

		public CheatsListView() : base(columnMappingForCheat) { }
	}
}

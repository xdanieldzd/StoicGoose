using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Controls
{
	// Based on https://stackoverflow.com/a/63778553

	public abstract class ListViewEx<T> : ListView
	{
		IList<(string ColumnName, Func<T, object> ValueLookup, Func<T, string> DisplayStringLookup)> ColumnInfo { get; }

		public ListViewEx(IList<(string ColumnName, Func<T, object> ValueLookup, Func<T, string> DisplayStringLookup)> columnInfo) : base()
		{
			ColumnInfo = columnInfo;
			DoubleBuffered = true;

			columnInfo.Select(ci => ci.ColumnName).ToList().ForEach(columnName =>
			{
				var col = Columns.Add(columnName);
				col.Width = -2;
			});
		}

		public void Add(T item)
		{
			var lvi = Items.Add("");
			lvi.Tag = item;

			RefreshContent();
		}

		public void AddRange(IList<T> items)
		{
			foreach (var item in items)
				Add(item);
		}

		public void RemoveAll()
		{
			foreach (var item in Items.Cast<ListViewItem>().Select(lvi => (T)lvi.Tag))
				Remove(item);
		}

		public void Remove(T item)
		{
			if (item == null) return;

			var listviewItem = Items.Cast<ListViewItem>().Select(lvi => new { ListViewItem = lvi, Obj = (T)lvi.Tag }).FirstOrDefault(lvi => item.Equals(lvi.Obj)).ListViewItem;
			Items.Remove(listviewItem);

			RefreshContent();
		}

		public void Replace(T oldItem, T newItem)
		{
			if (oldItem == null || newItem == null) return;

			var oldIndex = SelectedIndices.Cast<int>().First();
			if (oldIndex != -1)
			{
				Items.RemoveAt(oldIndex);
				var lvi = Items.Insert(oldIndex, "");
				lvi.Tag = newItem;

				SelectedIndices.Clear();
				SelectedIndices.Add(oldIndex);
			}

			RefreshContent();
		}

		public List<T> GetSelectedItems()
		{
			return SelectedItems.OfType<ListViewItem>().Select(lvi => (T)lvi.Tag).ToList();
		}

		public void RefreshContent()
		{
			var columnsChanged = new List<int>();

			Items.Cast<ListViewItem>().Select(lvi => new { ListViewItem = lvi, Obj = (T)lvi.Tag }).ToList().ForEach(lvi =>
			{
				ColumnInfo.Select((column, index) => new { Column = column, Index = index }).ToList().ForEach(col =>
				{
					var newDisplayValue = col.Column.DisplayStringLookup(lvi.Obj);
					if (lvi.ListViewItem.SubItems.Count <= col.Index)
						lvi.ListViewItem.SubItems.Add("");

					var subitem = lvi.ListViewItem.SubItems[col.Index];
					var oldDisplayValue = subitem.Text ?? "";

					if (!oldDisplayValue.Equals(newDisplayValue))
					{
						subitem.Text = newDisplayValue;
						columnsChanged.Add(col.Index);
					}
				});
			});

			columnsChanged.ForEach(col => { Columns[col].Width = -2; });
		}
	}
}

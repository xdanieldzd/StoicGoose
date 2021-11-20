using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using StoicGoose.WinForms.Controls;

namespace StoicGoose.WinForms
{
	public static class ControlHelpers
	{
		public class SettingsPage
		{
			readonly TreeNode node = new();
			readonly Dictionary<string, Control> controlList = new();
			readonly List<SettingsPage> subPages = new();

			public SettingsPage(object obj, string propertyName)
			{
				node.Text = GetDisplayName(obj.GetType(), propertyName);
			}

			public Control this[string name]
			{
				get { return controlList[name]; }
				set { controlList.Add(name, value); }
			}

			public void Append(params Control[] controls)
			{
				foreach (var control in controls)
					controlList.Add(control.Name, control);
			}

			public void Append(SettingsPage page)
			{
				subPages.Add(page);
			}

			public void Attach(TreeView treeView)
			{
				if (controlList.Count > 0) node.Tag = controlList.Select(x => x.Value).ToArray();
				treeView.Nodes.Add(node);
				foreach (var page in subPages) page.Attach(node);
			}

			private void Attach(TreeNode parent)
			{
				parent.Expand();
				if (controlList.Count > 0) node.Tag = controlList.Select(x => x.Value).ToArray();
				parent.Nodes.Add(node);
			}
		}

		public static TreeNode CreateNode(object obj, string propertyName, params Control[] controls)
		{
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(TreeNode)}",
				Text = GetDisplayName(obj.GetType(), propertyName),
				ToolTipText = GetDescription(obj.GetType(), propertyName),
				Tag = controls
			};
		}

		private static LabelEx CreateLabel(object obj, string propertyName)
		{
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(LabelEx)}",
				Text = $"{GetDisplayName(obj.GetType(), propertyName)}:",
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(3)
			};
		}

		private static TextBox CreateTextbox(object obj, string propertyName)
		{
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(TextBox)}",
				Dock = DockStyle.Fill
			};
		}

		private static CheckBox CreateCheckBox(object obj, string propertyName)
		{
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(CheckBox)}",
				Text = GetDisplayName(obj.GetType(), propertyName),
				Dock = DockStyle.Fill,
				Tag = 2
			};
		}

		private static FileTextBox CreateFileTextBox(object obj, string propertyName)
		{
			var value = obj.GetType().GetProperty(propertyName).GetValue(obj) as string;
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(FileTextBox)}",
				Dock = DockStyle.Fill,
				FileName = Path.GetFileName(value),
				InitialDirectory = Path.GetDirectoryName(value)
			};
		}

		public static Control[] CreatePathSelector(object obj, string propertyName)
		{
			var label = CreateLabel(obj, propertyName);
			var fileTextBox = CreateFileTextBox(obj, propertyName);
			fileTextBox.DataBindings.Add(nameof(FileTextBox.FileName), obj, propertyName);
			return new Control[] { label, fileTextBox };
		}

		public static Control[] CreateToggle(object obj, string propertyName)
		{
			var checkBox = CreateCheckBox(obj, propertyName);
			checkBox.DataBindings.Add(nameof(CheckBox.Checked), obj, propertyName);
			return new Control[] { checkBox };
		}

		private static TrackBar CreateTrackbar(object obj, string propertyName)
		{
			return new()
			{
				Name = $"{obj}_{propertyName}_{nameof(TrackBar)}",
				Dock = DockStyle.Fill,
				TickStyle = TickStyle.Both,
				TickFrequency = 10
			};
		}

		public static Control[] CreateSlider<T>(object obj, string propertyName) where T : struct
		{
			var label = CreateLabel(obj, propertyName);
			var trackBar = CreateTrackbar(obj, propertyName);
			var (minimum, maximum) = GetRange<T>(obj.GetType(), propertyName);
			trackBar.Minimum = (int)(object)minimum;
			trackBar.Maximum = (int)(object)maximum;
			trackBar.SmallChange = 5;
			trackBar.LargeChange = 20;
			trackBar.DataBindings.Add(nameof(TrackBar.Value), obj, propertyName);
			return new Control[] { label, trackBar };
		}

		private static string GetDisplayName(Type type, string propertyName)
		{
			return (type.GetProperty(propertyName).GetCustomAttribute(typeof(DisplayNameAttribute), false) as DisplayNameAttribute)?.DisplayName ?? propertyName;
		}

		private static string GetDescription(Type type, string propertyName)
		{
			return (type.GetProperty(propertyName).GetCustomAttribute(typeof(DescriptionAttribute), false) as DescriptionAttribute)?.Description ?? $"{propertyName} has no description";
		}

		private static (T, T) GetRange<T>(Type type, string propertyName)
		{
			if (type.GetProperty(propertyName).GetCustomAttribute(typeof(RangeAttribute), false) is not RangeAttribute rangeAttrib || rangeAttrib.OperandType != typeof(T)) return (default(T), default(T));
			return ((T)rangeAttrib.Minimum, (T)rangeAttrib.Maximum);
		}
	}
}

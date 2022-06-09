using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using WinKeys = System.Windows.Forms.Keys;
using OTKKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

using StoicGoose.Handlers;
using StoicGoose.WinForms.Controls;
using StoicGoose.XInput;

namespace StoicGoose.WinForms
{
	public static class ControlHelpers
	{
		readonly static Dictionary<WinKeys, OTKKeys> winToOpenTkKeyTranslator = new()
		{
			{ WinKeys.Space, OTKKeys.Space },
			{ WinKeys.Oemcomma, OTKKeys.Comma },
			{ WinKeys.OemMinus, OTKKeys.Minus },
			{ WinKeys.OemPeriod, OTKKeys.Period },
			{ WinKeys.D0, OTKKeys.D0 },
			{ WinKeys.D1, OTKKeys.D1 },
			{ WinKeys.D2, OTKKeys.D2 },
			{ WinKeys.D3, OTKKeys.D3 },
			{ WinKeys.D4, OTKKeys.D4 },
			{ WinKeys.D5, OTKKeys.D5 },
			{ WinKeys.D6, OTKKeys.D6 },
			{ WinKeys.D7, OTKKeys.D7 },
			{ WinKeys.D8, OTKKeys.D8 },
			{ WinKeys.D9, OTKKeys.D9 },
			{ WinKeys.OemSemicolon, OTKKeys.Semicolon },
			{ WinKeys.A, OTKKeys.A },
			{ WinKeys.B, OTKKeys.B },
			{ WinKeys.C, OTKKeys.C },
			{ WinKeys.D, OTKKeys.D },
			{ WinKeys.E, OTKKeys.E },
			{ WinKeys.F, OTKKeys.F },
			{ WinKeys.G, OTKKeys.G },
			{ WinKeys.H, OTKKeys.H },
			{ WinKeys.I, OTKKeys.I },
			{ WinKeys.J, OTKKeys.J },
			{ WinKeys.K, OTKKeys.K },
			{ WinKeys.L, OTKKeys.L },
			{ WinKeys.M, OTKKeys.M },
			{ WinKeys.N, OTKKeys.N },
			{ WinKeys.O, OTKKeys.O },
			{ WinKeys.P, OTKKeys.P },
			{ WinKeys.Q, OTKKeys.Q },
			{ WinKeys.R, OTKKeys.R },
			{ WinKeys.S, OTKKeys.S },
			{ WinKeys.T, OTKKeys.T },
			{ WinKeys.U, OTKKeys.U },
			{ WinKeys.V, OTKKeys.V },
			{ WinKeys.W, OTKKeys.W },
			{ WinKeys.X, OTKKeys.X },
			{ WinKeys.Y, OTKKeys.Y },
			{ WinKeys.Z, OTKKeys.Z },
			{ WinKeys.OemOpenBrackets, OTKKeys.LeftBracket },
			{ WinKeys.OemBackslash, OTKKeys.Backslash },
			{ WinKeys.OemCloseBrackets, OTKKeys.RightBracket },
			{ WinKeys.Escape, OTKKeys.Escape },
			{ WinKeys.Enter, OTKKeys.Enter },
			{ WinKeys.Tab, OTKKeys.Tab },
			{ WinKeys.Back, OTKKeys.Backspace },
			{ WinKeys.Insert, OTKKeys.Insert },
			{ WinKeys.Delete, OTKKeys.Delete },
			{ WinKeys.Right, OTKKeys.Right },
			{ WinKeys.Left, OTKKeys.Left },
			{ WinKeys.Down, OTKKeys.Down },
			{ WinKeys.Up, OTKKeys.Up },
			{ WinKeys.PageUp, OTKKeys.PageUp },
			{ WinKeys.PageDown, OTKKeys.PageDown },
			{ WinKeys.Home, OTKKeys.Home },
			{ WinKeys.End, OTKKeys.End },
			{ WinKeys.Pause, OTKKeys.Pause },
			{ WinKeys.F1, OTKKeys.F1 },
			{ WinKeys.F2, OTKKeys.F2 },
			{ WinKeys.F3, OTKKeys.F3 },
			{ WinKeys.F4, OTKKeys.F4 },
			{ WinKeys.F5, OTKKeys.F5 },
			{ WinKeys.F6, OTKKeys.F6 },
			{ WinKeys.F7, OTKKeys.F7 },
			{ WinKeys.F8, OTKKeys.F8 },
			{ WinKeys.F9, OTKKeys.F9 },
			{ WinKeys.F10, OTKKeys.F10 },
			{ WinKeys.F11, OTKKeys.F11 },
			{ WinKeys.F12, OTKKeys.F12 },
			{ WinKeys.NumPad0, OTKKeys.KeyPad0 },
			{ WinKeys.NumPad1, OTKKeys.KeyPad1 },
			{ WinKeys.NumPad2, OTKKeys.KeyPad2 },
			{ WinKeys.NumPad3, OTKKeys.KeyPad3 },
			{ WinKeys.NumPad4, OTKKeys.KeyPad4 },
			{ WinKeys.NumPad5, OTKKeys.KeyPad5 },
			{ WinKeys.NumPad6, OTKKeys.KeyPad6 },
			{ WinKeys.NumPad7, OTKKeys.KeyPad7 },
			{ WinKeys.NumPad8, OTKKeys.KeyPad8 },
			{ WinKeys.NumPad9, OTKKeys.KeyPad9 },
			{ WinKeys.Decimal, OTKKeys.KeyPadDecimal },
		};

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

		public static Control[] CreateKeyInput(object obj, string propertyName, string keyName)
		{
			var dict = obj.GetType().GetProperty(propertyName).GetValue(obj) as Dictionary<string, List<string>>;

			var label = new LabelEx()
			{
				Name = $"{dict}_{keyName}_{nameof(LabelEx)}",
				Text = keyName,
				Dock = DockStyle.Fill,
				TextAlign = ContentAlignment.MiddleLeft,
				Margin = new Padding(3)
			};
			var textBoxEx = new TextBoxEx()
			{
				Name = $"{dict}_{keyName}_{nameof(TextBoxEx)}",
				Dock = DockStyle.Fill,
				ReadOnly = true,
				BackColor = SystemColors.Window,
				TabIndex = Array.IndexOf(dict.Keys.ToArray(), keyName),
				Tag = dict[keyName],
				TimerInterval = 100
			};
			textBoxEx.TimerTick += (s, e) =>
			{
				if (s is TextBoxEx textBoxEx && textBoxEx.Tag is List<string> keyValues)
				{
					if (!textBoxEx.Focused) return;

					for (var i = 0; i < ControllerManager.MaxControllers; i++)
					{
						var controller = ControllerManager.GetController(i);
						controller.Update();

						if (!controller.IsConnected) continue;

						var doUpdate = false;

						if (controller.Buttons != Buttons.None && (((int)controller.Buttons & ((int)controller.Buttons - 1)) == 0))
						{
							keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{controller.Buttons}").OrderBy(x => x).Distinct().ToList();
							doUpdate = true;
						}
						if (controller.LeftThumbstick != ThumbstickPosition.Zero)
						{
							if (controller.LeftThumbstick.X < -0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.LeftThumbstick)}Left").OrderBy(x => x).Distinct().ToList();
							else if (controller.LeftThumbstick.X > 0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.LeftThumbstick)}ThumbRight").OrderBy(x => x).Distinct().ToList();
							if (controller.LeftThumbstick.Y < -0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.LeftThumbstick)}ThumbDown").OrderBy(x => x).Distinct().ToList();
							else if (controller.LeftThumbstick.Y > 0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.LeftThumbstick)}ThumbUp").OrderBy(x => x).Distinct().ToList();
							doUpdate = true;
						}
						if (controller.RightThumbstick != ThumbstickPosition.Zero)
						{
							if (controller.RightThumbstick.X < -0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.RightThumbstick)}ThumbLeft").OrderBy(x => x).Distinct().ToList();
							else if (controller.RightThumbstick.X > 0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.RightThumbstick)}ThumbRight").OrderBy(x => x).Distinct().ToList();
							if (controller.RightThumbstick.Y < -0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.RightThumbstick)}ThumbDown").OrderBy(x => x).Distinct().ToList();
							else if (controller.RightThumbstick.Y > 0.5f) keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.RightThumbstick)}ThumbUp").OrderBy(x => x).Distinct().ToList();
							doUpdate = true;
						}
						if (controller.LeftTrigger > 0.5f)
						{
							keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.LeftTrigger)}").OrderBy(x => x).Distinct().ToList();
							doUpdate = true;
						}
						if (controller.RightTrigger > 0.5f)
						{
							keyValues = keyValues.Append($"{InputHandler.GamepadPrefix}{i}+{nameof(controller.RightTrigger)}").OrderBy(x => x).Distinct().ToList();
							doUpdate = true;
						}

						if (doUpdate)
						{
							textBoxEx.Text = string.Join(", ", keyValues.Select(x => $"{x.Split('+')[1]} ({x.Split('+')[0]})"));
							textBoxEx.Tag = keyValues;
						}
					}
				}
			};
			textBoxEx.KeyDown += (s, e) =>
			{
				if (s is TextBoxEx textBoxEx && textBoxEx.Tag is List<string> keyValues && winToOpenTkKeyTranslator.ContainsKey(e.KeyCode))
				{
					keyValues = keyValues.Append($"{InputHandler.KeyboardPrefix}+{winToOpenTkKeyTranslator[e.KeyCode]}").OrderBy(x => x).Distinct().ToList();
					textBoxEx.Text = string.Join(", ", keyValues.Select(x => $"{x.Split('+')[1]} ({x.Split('+')[0]})"));
					textBoxEx.Tag = keyValues;
				}
			};
			textBoxEx.MouseDown += (s, e) =>
			{
				if (s is TextBoxEx textBoxEx && textBoxEx.Tag is List<string> keyValues && e.Button == MouseButtons.Middle)
				{
					textBoxEx.Text = string.Empty;
					keyValues.Clear();
				}
			};
			textBoxEx.GotFocus += (s, e) =>
			{
				if (s is TextBoxEx textBoxEx)
				{
					textBoxEx.StartTimer();
					textBoxEx.SelectionStart = textBoxEx.SelectionLength = 0;
				}
			};
			textBoxEx.LostFocus += (s, e) =>
			{
				if (s is TextBoxEx textBoxEx)
				{
					textBoxEx.StopTimer();
					textBoxEx.SelectionStart = textBoxEx.SelectionLength = 0;
				}
			};

			var binding = new Binding(nameof(TextBoxEx.Text), obj, propertyName);
			binding.Parse += (s, e) =>
			{
				if (textBoxEx.Tag is List<string> keyValues)
					dict[keyName] = keyValues;
			};
			binding.Format += (s, e) =>
			{
				if (e.Value is Dictionary<string, List<string>> dict)
					e.Value = string.Join(", ", dict[keyName].Select(x => $"{x.Split('+')[1]} ({x.Split('+')[0]})"));
			};
			textBoxEx.DataBindings.Add(binding);

			return new Control[] { label, textBoxEx };
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

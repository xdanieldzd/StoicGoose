using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;

using StoicGoose.WinForms.Windows;
using StoicGoose.WinForms.XInput;

namespace StoicGoose.WinForms.Handlers
{
	public class InputHandler
	{
		public const string KeyboardPrefix = "Keyboard";
		public const string GamepadPrefix = "Gamepad";

		readonly INativeInput nativeInput = default;

		readonly Dictionary<string, List<Keys>> keyMapping = new();
		readonly Dictionary<string, List<(int id, string input)>> buttonMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();

		readonly List<string> lastPollHeld = new();

		public bool IsVerticalOrientation { get; set; } = false;

		public InputHandler(GLControl glControl)
		{
			nativeInput = glControl.EnableNativeInput();
		}

		public void SetKeyMapping(params Dictionary<string, List<string>>[] keyConfigs)
		{
			keyMapping.Clear();
			buttonMapping.Clear();

			foreach (var keyConfig in keyConfigs)
			{
				foreach (var (key, values) in keyConfig)
				{
					if (!keyMapping.ContainsKey(key)) keyMapping.Add(key, new());
					if (!buttonMapping.ContainsKey(key)) buttonMapping.Add(key, new());

					foreach (var value in values.Where(x => !string.IsNullOrEmpty(x)))
					{
						if (value.StartsWith(KeyboardPrefix))
							keyMapping[key].Add((Keys)Enum.Parse(typeof(Keys), value[(value.IndexOf('+') + 1)..]));

						if (value.StartsWith(GamepadPrefix))
						{
							var split = value.Split('+', StringSplitOptions.TrimEntries);
							buttonMapping[key].Add((int.Parse(split[0].Replace(GamepadPrefix, string.Empty)), split[1]));
						}
					}
				}
			}
		}

		public void SetVerticalRemapping(Dictionary<string, string> remapDict)
		{
			verticalRemapping.Clear();
			foreach (var (key, value) in remapDict.Where(x => !string.IsNullOrEmpty(x.Value)))
				verticalRemapping.Add(key, value);
		}

		public List<string> GetMappedKeysHeld() => keyMapping.Where(x => IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]].Any(y => nativeInput.IsKeyDown(y)) : x.Value.Any(y => nativeInput.IsKeyDown(y))).Select(x => x.Key).ToList();
		public List<string> GetMappedKeysPressed() => keyMapping.Where(x => IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]].Any(y => nativeInput.IsKeyDown(y)) : x.Value.Any(y => nativeInput.IsKeyDown(y)) && !lastPollHeld.Contains(x.Key)).Select(x => x.Key).ToList();

		public List<string> GetMappedButtonsHeld()
		{
			var list = new List<string>();

			foreach (var (key, value) in buttonMapping)
			{
				var keyName = IsVerticalOrientation && verticalRemapping.ContainsKey(key) ? verticalRemapping[key] : key;
				foreach (var (id, input) in value)
				{
					var controller = ControllerManager.GetController(id);
					if (!controller.IsConnected) continue;

					if (Enum.TryParse(input, out Buttons result) && (controller.Buttons & result) != Buttons.None) list.Add(keyName);
					if (input == $"{nameof(controller.LeftThumbstick)}Left" && controller.LeftThumbstick.X < -0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.LeftThumbstick)}Right" && controller.LeftThumbstick.X > 0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.LeftThumbstick)}Down" && controller.LeftThumbstick.Y < -0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.LeftThumbstick)}Up" && controller.LeftThumbstick.Y > 0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.RightThumbstick)}Left" && controller.RightThumbstick.X < -0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.RightThumbstick)}Right" && controller.RightThumbstick.X > 0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.RightThumbstick)}Down" && controller.RightThumbstick.Y < -0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.RightThumbstick)}Up" && controller.RightThumbstick.Y > 0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.LeftTrigger)}" && controller.LeftTrigger > 0.5f) list.Add(keyName);
					if (input == $"{nameof(controller.RightTrigger)}" && controller.RightTrigger > 0.5f) list.Add(keyName);
				}
			}

			return list;
		}

		public List<string> GetMappedButtonsPressed()
		{
			var list = new List<string>();

			foreach (var (_, _) in buttonMapping)
			{
				// TODO
			}

			return list;
		}

		public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
		{
			if (buttonMapping.Count > 0)
				ControllerManager.Update();

			buttonsHeld.AddRange(GetMappedKeysHeld().Concat(GetMappedButtonsHeld()).Distinct());
			buttonsPressed.AddRange(GetMappedKeysPressed().Concat(GetMappedButtonsPressed()).Distinct());

			lastPollHeld.Clear();
			lastPollHeld.AddRange(buttonsHeld);
		}
	}
}

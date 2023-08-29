using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;

using StoicGoose.WinForms.XInput;

namespace StoicGoose.WinForms.Handlers
{
	public class InputHandler
	{
		public const string KeyboardPrefix = "Keyboard";
		public const string GamepadPrefix = "Gamepad";

		readonly INativeInput nativeInput = default;

		readonly Dictionary<string, List<Keys>> keyboardMapping = new();
		readonly Dictionary<string, List<(int id, string input)>> gamepadMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();

		readonly List<string> lastPollHeldKeyboard = new();
		readonly List<string> lastPollHeldGamepad = new();

		public bool IsVerticalOrientation { get; set; } = false;

		public InputHandler(GLControl glControl)
		{
			nativeInput = glControl.EnableNativeInput();
		}

		public void SetKeyMapping(params Dictionary<string, List<string>>[] keyConfigs)
		{
			keyboardMapping.Clear();
			gamepadMapping.Clear();

			foreach (var keyConfig in keyConfigs)
			{
				foreach (var (key, values) in keyConfig)
				{
					if (!keyboardMapping.ContainsKey(key)) keyboardMapping.Add(key, new());
					if (!gamepadMapping.ContainsKey(key)) gamepadMapping.Add(key, new());

					foreach (var value in values.Where(x => !string.IsNullOrEmpty(x)))
					{
						if (value.StartsWith(KeyboardPrefix))
							keyboardMapping[key].Add((Keys)Enum.Parse(typeof(Keys), value[(value.IndexOf('+') + 1)..]));

						if (value.StartsWith(GamepadPrefix))
						{
							var split = value.Split('+', StringSplitOptions.TrimEntries);
							gamepadMapping[key].Add((int.Parse(split[0].Replace(GamepadPrefix, string.Empty)), split[1]));
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

		public List<string> GetMappedKeyboardInputsHeld() => keyboardMapping.Where(x => IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyboardMapping[verticalRemapping[x.Key]].Any(y => nativeInput.IsKeyDown(y)) : x.Value.Any(y => nativeInput.IsKeyDown(y))).Select(x => x.Key).ToList();
		public List<string> GetMappedKeyboardInputsPressed() => keyboardMapping.Where(x => IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyboardMapping[verticalRemapping[x.Key]].Any(y => nativeInput.IsKeyDown(y)) : x.Value.Any(y => nativeInput.IsKeyDown(y)) && !lastPollHeldKeyboard.Contains(x.Key)).Select(x => x.Key).ToList();

		public List<string> GetMappedGamepadInputsHeld()
		{
			var list = new List<string>();

			foreach (var (key, value) in gamepadMapping)
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

		public List<string> GetMappedGamepadInputsPressed()
		{
			var list = new List<string>();

			foreach (var (key, value) in gamepadMapping)
			{
				var keyName = IsVerticalOrientation && verticalRemapping.ContainsKey(key) ? verticalRemapping[key] : key;
				foreach (var (id, input) in value)
				{
					var controller = ControllerManager.GetController(id);
					if (!controller.IsConnected) continue;

					if (!lastPollHeldGamepad.Contains(keyName) && Enum.TryParse(input, out Buttons result) && (controller.Buttons & result) != Buttons.None) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.LeftThumbstick)}Left" && controller.LeftThumbstick.X < -0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.LeftThumbstick)}Right" && controller.LeftThumbstick.X > 0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.LeftThumbstick)}Down" && controller.LeftThumbstick.Y < -0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.LeftThumbstick)}Up" && controller.LeftThumbstick.Y > 0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.RightThumbstick)}Left" && controller.RightThumbstick.X < -0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.RightThumbstick)}Right" && controller.RightThumbstick.X > 0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.RightThumbstick)}Down" && controller.RightThumbstick.Y < -0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.RightThumbstick)}Up" && controller.RightThumbstick.Y > 0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.LeftTrigger)}" && controller.LeftTrigger > 0.5f) list.Add(keyName);
					if (!lastPollHeldGamepad.Contains(keyName) && input == $"{nameof(controller.RightTrigger)}" && controller.RightTrigger > 0.5f) list.Add(keyName);
				}
			}

			return list;
		}

		public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
		{
			if (gamepadMapping.Count > 0)
				ControllerManager.Update();

			var keyboardHeldNow = GetMappedKeyboardInputsHeld();
			var gamepadHeldNow = GetMappedGamepadInputsHeld();

			buttonsHeld.AddRange(keyboardHeldNow.Concat(gamepadHeldNow).Distinct());
			buttonsPressed.AddRange(GetMappedKeyboardInputsPressed().Concat(GetMappedGamepadInputsPressed()).Distinct());

			lastPollHeldKeyboard.Clear();
			lastPollHeldKeyboard.AddRange(keyboardHeldNow);
			lastPollHeldGamepad.Clear();
			lastPollHeldGamepad.AddRange(gamepadHeldNow);
		}
	}
}

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;
using StoicGoose.WinForms.XInput;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StoicGoose.WinForms.Handlers
{
    public class InputHandler(GLControl glControl)
    {
        public const string KeyboardPrefix = "Keyboard";
        public const string GamepadPrefix = "Gamepad";

        readonly INativeInput nativeInput = glControl.EnableNativeInput();

        readonly Dictionary<string, List<Keys>> keyboardMapping = [];
        readonly Dictionary<string, List<(int id, string input)>> gamepadMapping = [];
        readonly Dictionary<string, string> verticalRemapping = [];

        readonly List<string> lastPollHeldKeyboard = [];
        readonly List<string> lastPollHeldGamepad = [];

        bool isVerticalOrientation, enableRemapping;

        public void SetKeyMapping(params Dictionary<string, List<string>>[] keyConfigs)
        {
            keyboardMapping.Clear();
            gamepadMapping.Clear();

            foreach (var keyConfig in keyConfigs)
            {
                foreach (var (key, values) in keyConfig)
                {
                    if (!keyboardMapping.ContainsKey(key)) keyboardMapping.Add(key, []);
                    if (!gamepadMapping.ContainsKey(key)) gamepadMapping.Add(key, []);

                    foreach (var value in values.Where(x => !string.IsNullOrEmpty(x)))
                    {
                        if (value.StartsWith(KeyboardPrefix))
                            keyboardMapping[key].Add(Enum.Parse<Keys>(value[(value.IndexOf('+') + 1)..]));

                        if (value.StartsWith(GamepadPrefix))
                        {
                            var split = value.Split('+', StringSplitOptions.TrimEntries);
                            gamepadMapping[key].Add((int.Parse(split[0].Replace(GamepadPrefix, string.Empty)), split[1]));
                        }
                    }
                }
            }
        }

        public void SetVerticalOrientation(bool vertical)
        {
            isVerticalOrientation = vertical;
        }

        public void SetEnableRemapping(bool enable)
        {
            enableRemapping = enable;
        }

        public void SetVerticalRemapping(Dictionary<string, string> remapDict)
        {
            verticalRemapping.Clear();
            foreach (var (key, value) in remapDict.Where(x => !string.IsNullOrEmpty(x.Value)))
                verticalRemapping.Add(key, value);
        }

        public List<string> GetMappedKeyboardInputs() => GetMappedKeyboardInputs((_) => true);
        public List<string> GetMappedKeyboardInputs(Func<string, bool> checkCondition)
        {
            var list = new List<string>();

            foreach (var (key, value) in keyboardMapping)
            {
                var keyName = isVerticalOrientation && enableRemapping && verticalRemapping.TryGetValue(key, out string value1) ? value1 : key;
                foreach (var input in value)
                {
                    if (checkCondition(keyName) && nativeInput.IsKeyDown(input))
                        list.Add(keyName);
                }
            }

            return list;
        }

        public List<string> GetMappedGamepadInputs() => GetMappedGamepadInputs((_) => true);
        public List<string> GetMappedGamepadInputs(Func<string, bool> checkCondition)
        {
            var list = new List<string>();

            foreach (var (key, value) in gamepadMapping)
            {
                var keyName = isVerticalOrientation && enableRemapping && verticalRemapping.TryGetValue(key, out string value1) ? value1 : key;
                foreach (var (id, input) in value)
                {
                    var controller = ControllerManager.GetController(id);
                    if (!controller.IsConnected) continue;

                    if (checkCondition(keyName) && Enum.TryParse(input, out Buttons result) && (controller.Buttons & result) != Buttons.None) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.LeftThumbstick)}Left" && controller.LeftThumbstick.X < -0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.LeftThumbstick)}Right" && controller.LeftThumbstick.X > 0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.LeftThumbstick)}Down" && controller.LeftThumbstick.Y < -0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.LeftThumbstick)}Up" && controller.LeftThumbstick.Y > 0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.RightThumbstick)}Left" && controller.RightThumbstick.X < -0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.RightThumbstick)}Right" && controller.RightThumbstick.X > 0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.RightThumbstick)}Down" && controller.RightThumbstick.Y < -0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.RightThumbstick)}Up" && controller.RightThumbstick.Y > 0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.LeftTrigger)}" && controller.LeftTrigger > 0.5f) list.Add(keyName);
                    if (checkCondition(keyName) && input == $"{nameof(controller.RightTrigger)}" && controller.RightTrigger > 0.5f) list.Add(keyName);
                }
            }

            return list;
        }

        public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
        {
            if (gamepadMapping.Count > 0)
                ControllerManager.Update();

            var keyboardHeldNow = GetMappedKeyboardInputs();
            var gamepadHeldNow = GetMappedGamepadInputs();

            buttonsHeld.AddRange(keyboardHeldNow.Concat(gamepadHeldNow).Distinct());
            buttonsPressed.AddRange(GetMappedKeyboardInputs((x) => !lastPollHeldKeyboard.Contains(x)).Concat(GetMappedGamepadInputs((x) => !lastPollHeldGamepad.Contains(x))).Distinct());

            lastPollHeldKeyboard.Clear();
            lastPollHeldKeyboard.AddRange(keyboardHeldNow);
            lastPollHeldGamepad.Clear();
            lastPollHeldGamepad.AddRange(gamepadHeldNow);
        }
    }
}

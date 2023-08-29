﻿using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace StoicGoose.GLWindow
{
	public class InputHandler
	{
		readonly Dictionary<string, Keys> keyboardMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();
		readonly List<string> lastPollHeld = new();

		GameWindow gameWindow = default;

		bool isVerticalOrientation, enableRemapping;

		public void SetGameWindow(GameWindow window) => gameWindow = window;

		public void SetKeyMapping(params Dictionary<string, string>[] keyConfigs)
		{
			keyboardMapping.Clear();
			foreach (var keyConfig in keyConfigs)
				foreach (var (key, value) in keyConfig.Where(x => !string.IsNullOrEmpty(x.Value)))
					keyboardMapping.Add(key, (Keys)Enum.Parse(typeof(Keys), value));
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
				var keyName = isVerticalOrientation && enableRemapping && verticalRemapping.ContainsKey(key) ? verticalRemapping[key] : key;
				{
					if (checkCondition(keyName) && gameWindow.IsKeyDown(value))
						list.Add(keyName);
				}
			}

			return list;
		}

		public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
		{
			buttonsHeld.AddRange(GetMappedKeyboardInputs().Distinct());
			buttonsPressed.AddRange(GetMappedKeyboardInputs((x) => !lastPollHeld.Contains(x)).Distinct());

			lastPollHeld.Clear();
			lastPollHeld.AddRange(buttonsHeld);
		}
	}
}

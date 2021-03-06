using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace StoicGoose.GLWindow
{
	public class InputHandler
	{
		readonly Dictionary<string, Keys> keyMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();
		readonly List<string> lastPollHeld = new();

		GameWindow gameWindow = default;

		public bool IsVerticalOrientation { get; set; } = false;

		public void SetGameWindow(GameWindow window) => gameWindow = window;

		public void SetKeyMapping(params Dictionary<string, string>[] keyConfigs)
		{
			keyMapping.Clear();
			foreach (var keyConfig in keyConfigs)
				foreach (var (key, value) in keyConfig.Where(x => !string.IsNullOrEmpty(x.Value)))
					keyMapping.Add(key, (Keys)Enum.Parse(typeof(Keys), value));
		}

		public void SetVerticalRemapping(Dictionary<string, string> remapDict)
		{
			verticalRemapping.Clear();
			foreach (var (key, value) in remapDict.Where(x => !string.IsNullOrEmpty(x.Value)))
				verticalRemapping.Add(key, value);
		}

		public List<string> GetMappedKeysHeld() => keyMapping.Where(x => gameWindow.IsKeyDown(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value)).Select(x => x.Key).ToList();
		public List<string> GetMappedKeysPressed() => keyMapping.Where(x => gameWindow.IsKeyDown(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value) && !lastPollHeld.Contains(x.Key)).Select(x => x.Key).ToList();

		public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
		{
			buttonsHeld.AddRange(GetMappedKeysHeld().Distinct());
			buttonsPressed.AddRange(GetMappedKeysPressed().Distinct());

			lastPollHeld.Clear();
			lastPollHeld.AddRange(buttonsHeld);
		}
	}
}

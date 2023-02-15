using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace StoicGoose.WPF
{
	public class InputHandler
	{
		readonly List<Key> keysDown = new();

		readonly Dictionary<string, Key> keyMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();
		readonly List<string> lastPollHeld = new();

		public bool IsVerticalOrientation { get; set; } = false;

		public void KeyDownEventHandler(object sender, KeyEventArgs eventArgs)
		{
			if (eventArgs.IsDown && !keysDown.Contains(eventArgs.Key))
				keysDown.Add(eventArgs.Key);
		}

		public void KeyUpEventHandler(object sender, KeyEventArgs eventArgs)
		{
			if (eventArgs.IsUp)
				keysDown.Remove(eventArgs.Key);
		}

		public void SetKeyMapping(params Dictionary<string, string>[] keyConfigs)
		{
			keyMapping.Clear();
			foreach (var keyConfig in keyConfigs)
				foreach (var (key, value) in keyConfig.Where(x => !string.IsNullOrEmpty(x.Value)))
					keyMapping.Add(key, (Key)Enum.Parse(typeof(Key), value));
		}

		public void SetVerticalRemapping(Dictionary<string, string> remapDict)
		{
			verticalRemapping.Clear();
			foreach (var (key, value) in remapDict.Where(x => !string.IsNullOrEmpty(x.Value)))
				verticalRemapping.Add(key, value);
		}

		public List<string> GetMappedKeysHeld() => keyMapping.Where(x => keysDown.Contains(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value)).Select(x => x.Key).ToList();
		public List<string> GetMappedKeysPressed() => keyMapping.Where(x => keysDown.Contains(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value) && !lastPollHeld.Contains(x.Key)).Select(x => x.Key).ToList();

		public void PollInput(ref List<string> buttonsPressed, ref List<string> buttonsHeld)
		{
			buttonsHeld.AddRange(GetMappedKeysHeld().Distinct());
			buttonsPressed.AddRange(GetMappedKeysPressed().Distinct());

			lastPollHeld.Clear();
			lastPollHeld.AddRange(buttonsHeld);
		}
	}
}

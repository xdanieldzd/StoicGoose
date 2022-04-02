using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.WinForms;

using StoicGoose.WinForms;

namespace StoicGoose.Handlers
{
	public class InputHandler
	{
		readonly INativeInput nativeInput = default;
		readonly Dictionary<string, Keys> keyMapping = new();
		readonly Dictionary<string, string> verticalRemapping = new();

		readonly List<string> lastFramePressed = new();

		public bool IsVerticalOrientation { get; set; } = false;

		public InputHandler(GLControl glControl)
		{
			nativeInput = glControl.EnableNativeInput();
		}

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

		public List<string> GetMappedKeysHeld() => keyMapping.Where(x => nativeInput.IsKeyDown(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value)).Select(x => x.Key).ToList();
		public List<string> GetMappedKeysPressed() => keyMapping.Where(x => nativeInput.IsKeyDown(IsVerticalOrientation && verticalRemapping.ContainsKey(x.Key) ? keyMapping[verticalRemapping[x.Key]] : x.Value) && !lastFramePressed.Contains(x.Key)).Select(x => x.Key).ToList();

		public void PollInput(object sender, PollInputEventArgs e)
		{
			e.ButtonsHeld.Clear();

			e.ButtonsHeld.AddRange(GetMappedKeysHeld());
			e.ButtonsPressed.AddRange(GetMappedKeysPressed());

			// TODO: fix volume control
			//if (nativeInput.IsKeyDown(keyMapping["volume"])) e.ButtonsHeld.Add("volume");
			//if (nativeInput.IsKeyPressed(keyMapping["volume"]) && !lastFramePressed.Contains("volume")) e.ButtonsPressed.Add("volume");

			lastFramePressed.Clear();
			lastFramePressed.AddRange(e.ButtonsHeld);
		}
	}
}

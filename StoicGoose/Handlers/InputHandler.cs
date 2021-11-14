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
		readonly Dictionary<string, Keys> keyMapping = new Dictionary<string, Keys>();

		readonly List<string> lastFramePressed = new List<string>();

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

		public List<string> GetMappedKeysHeld() => keyMapping.Where(x => nativeInput.IsKeyDown(x.Value)).Select(x => x.Key).ToList();
		public List<string> GetMappedKeysPressed() => keyMapping.Where(x => nativeInput.IsKeyDown(x.Value) && !lastFramePressed.Contains(x.Key)).Select(x => x.Key).ToList();

		public void PollInput(object sender, PollInputEventArgs e)
		{
			e.ButtonsHeld.Clear();

			if (!IsVerticalOrientation)
			{
				e.ButtonsHeld.AddRange(GetMappedKeysHeld());
				e.ButtonsPressed.AddRange(GetMappedKeysPressed());
			}
			else
			{
				// TODO: more elegant way of doing vertical-orientation input?
				// also add ButtonsPressed support?
				if (nativeInput.IsKeyDown(keyMapping["start"])) e.ButtonsHeld.Add("start");
				if (nativeInput.IsKeyDown(keyMapping["b"])) e.ButtonsHeld.Add("b");
				if (nativeInput.IsKeyDown(keyMapping["a"])) e.ButtonsHeld.Add("a");

				if (nativeInput.IsKeyDown(keyMapping["x1"])) e.ButtonsHeld.Add("y2");
				if (nativeInput.IsKeyDown(keyMapping["x2"])) e.ButtonsHeld.Add("y3");
				if (nativeInput.IsKeyDown(keyMapping["x3"])) e.ButtonsHeld.Add("y4");
				if (nativeInput.IsKeyDown(keyMapping["x4"])) e.ButtonsHeld.Add("y1");

				if (nativeInput.IsKeyDown(keyMapping["y1"])) e.ButtonsHeld.Add("x2");
				if (nativeInput.IsKeyDown(keyMapping["y2"])) e.ButtonsHeld.Add("x3");
				if (nativeInput.IsKeyDown(keyMapping["y3"])) e.ButtonsHeld.Add("x4");
				if (nativeInput.IsKeyDown(keyMapping["y4"])) e.ButtonsHeld.Add("x1");

				//if (nativeInput.IsKeyDown(keyMapping["volume"])) e.ButtonsHeld.Add("volume");
				//if (nativeInput.IsKeyPressed(keyMapping["volume"]) && !lastFramePressed.Contains("volume")) e.ButtonsPressed.Add("volume");
			}

			lastFramePressed.Clear();
			lastFramePressed.AddRange(e.ButtonsHeld);
		}
	}
}

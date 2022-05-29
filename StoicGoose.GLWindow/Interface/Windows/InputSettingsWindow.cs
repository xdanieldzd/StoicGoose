using System;
using System.Collections.Generic;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;
using OTKKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class InputSettingsWindow : WindowBase
	{
		public InputSettingsWindow() : base("Input Settings", new(300f, 420f), ImGuiCond.Always) { }

		protected override void DrawWindow(object userData)
		{
			if (userData is not (Dictionary<string, string> gameControls, Dictionary<string, string> systemControls)) return;

			var gameControlChanges = new Dictionary<string, string>();
			var systemControlChanges = new Dictionary<string, string>();

			static void drawControls(Dictionary<string, string> controls, ref Dictionary<string, string> controlChanges)
			{
				foreach (var (input, key) in controls)
				{
					var popupId = $"Change Key##key-change-{input}";

					var style = ImGui.GetStyle();
					var cursorPos = ImGui.GetCursorPos();

					ImGui.SetCursorPos(new(cursorPos.X, cursorPos.Y + style.FramePadding.Y));
					ImGui.Text($"{input}:");

					ImGui.SetCursorPos(new(cursorPos.X + 100f, cursorPos.Y));
					if (ImGui.Button($"{key}##key-{input}", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f))) ImGui.OpenPopup(popupId);

					if (ImGui.IsPopupOpen(popupId))
					{
						var labelPadding = 20f;

						var viewportCenter = ImGui.GetMainViewport().GetCenter();
						ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

						var popupDummy = true;
						if (ImGui.BeginPopupModal(popupId, ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
						{
							ImGui.Dummy(new NumericsVector2(0f, labelPadding));
							ImGui.Dummy(new NumericsVector2(labelPadding, 0f)); ImGui.SameLine();
							ImGui.Text($"Please press the new key for '{input}'."); ImGui.SameLine();
							ImGui.Dummy(new NumericsVector2(labelPadding, 0f));
							ImGui.Dummy(new NumericsVector2(0f, labelPadding));

							ImGui.Dummy(new NumericsVector2(0f, 2f));
							ImGui.Separator();
							ImGui.Dummy(new NumericsVector2(0f, 2f));

							if (ImGui.Button("Cancel", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
								ImGui.CloseCurrentPopup();
							else
							{
								var io = ImGui.GetIO();
								for (var i = 0; i < io.KeysData.Count; i++)
								{
									var keyData = io.KeysData[i];
									if (keyData.Down != 0 && ((OTKKeys)io.KeyMap[i]) != OTKKeys.Unknown)
									{
										controlChanges.Add(input, ((OTKKeys)io.KeyMap[i]).ToString());
										ImGui.CloseCurrentPopup();
										break;
									}
								}
							}
							ImGui.EndPopup();
						}
					}
				}
			}

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoResize))
			{
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

				var style = ImGui.GetStyle();

				drawControls(gameControls, ref gameControlChanges);
				drawControls(systemControls, ref systemControlChanges);

				foreach (var (input, newKey) in gameControlChanges) gameControls[input] = newKey;
				foreach (var (input, newKey) in systemControlChanges) systemControls[input] = newKey;

				ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - ((style.FramePadding.Y * 3) + (ImGui.GetTextLineHeight() * 2)));

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				if (ImGui.BeginChild("##controls-frame", NumericsVector2.Zero))
				{
					if (ImGui.Button($"Close##close", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
						isWindowOpen = false;

					ImGui.EndChild();
				}

				ImGui.PopStyleVar();

				ImGui.End();
			}
		}
	}
}

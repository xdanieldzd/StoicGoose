﻿using System;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.ImGuiCommon.Handlers
{
	public class MessageBoxHandler
	{
		readonly List<MessageBox> messageBoxes = new();

		public bool IsAnyMessageBoxOpen => messageBoxes.Any(x => x.IsOpen);

		public MessageBoxHandler(params MessageBox[] messageBoxes)
		{
			this.messageBoxes.AddRange(messageBoxes);
		}

		public void AddMessageBox(MessageBox messageBox)
		{
			messageBoxes.Add(messageBox);
		}

		public void Draw()
		{
			foreach (var messageBox in messageBoxes.Where(x => x.IsOpen))
				ImGui.OpenPopup(messageBox.Title);

			for (var i = 0; i < messageBoxes.Count; i++)
			{
				if (!messageBoxes[i].IsOpen) continue;

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				if (ImGui.BeginPopupModal(messageBoxes[i].Title, ref messageBoxes[i].IsOpen, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));
					ImGui.Text(messageBoxes[i].Message);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					var buttonWidth = (ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().ItemSpacing.X * (messageBoxes[i].Buttons.Length - 1))) / messageBoxes[i].Buttons.Length;
					for (var j = 0; j < messageBoxes[i].Buttons.Length; j++)
					{
						if (ImGui.Button(messageBoxes[i].Buttons[j], new NumericsVector2(buttonWidth, 0f)))
						{
							ImGui.CloseCurrentPopup();
							messageBoxes[i].ReturnValue = j;
							messageBoxes[i].IsOpen = false;
							break;
						}
						ImGui.SameLine();
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}
			}
		}
	}

	public class MessageBox
	{
		public string Title { get; set; } = string.Empty;
		public string Message { get; set; } = string.Empty;
		public string[] Buttons { get; set; } = Array.Empty<string>();
		public int ReturnValue { get; set; } = -1;

		public bool IsOpen = false;

		public MessageBox(string title, string message, params string[] buttons)
		{
			Title = title;
			Message = message;
			Buttons = buttons;
		}
	}
}

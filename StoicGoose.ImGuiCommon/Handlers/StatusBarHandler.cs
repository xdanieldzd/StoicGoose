﻿using System;

using ImGuiNET;

namespace StoicGoose.ImGuiCommon.Handlers
{
	public class StatusBarHandler
	{
		public static void Draw(params StatusBarItem[] items)
		{
			var viewport = ImGui.GetMainViewport();
			var frameHeight = ImGui.GetFrameHeight();

			ImGui.SetNextWindowPos(new(viewport.Pos.X, viewport.Pos.Y + viewport.Size.Y - frameHeight));
			ImGui.SetNextWindowSize(new(viewport.Size.X, frameHeight));

			var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse |
				 ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.MenuBar;

			var framePadding = ImGui.GetStyle().FramePadding.X;
			var itemPadding = framePadding * 4f;

			if (ImGui.Begin("##statusbar", flags))
			{
				if (ImGui.BeginMenuBar())
				{
					var drawList = ImGui.GetWindowDrawList();
					var windowPos = ImGui.GetWindowPos();

					var cursorFromLeft = framePadding * 2f;
					var cursorFromRight = ImGui.GetWindowWidth() - framePadding * 2f;

					foreach (var item in items)
					{
						var labelWidth = ImGui.CalcTextSize(item.Label).X;
						var itemWidth = Math.Max(labelWidth, item.Width);

						if (item.ItemAlignment == StatusBarItemAlign.Left)
						{
							ImGui.SetCursorPosX(cursorFromLeft);
							cursorFromLeft += itemWidth + itemPadding;
							if (item.ShowSeparator)
								drawList.AddLine(
									new(windowPos.X + cursorFromLeft - itemPadding / 2f, windowPos.Y),
									new(windowPos.X + cursorFromLeft - itemPadding / 2f, windowPos.Y + frameHeight),
									ImGui.GetColorU32(ImGuiCol.TextDisabled));
						}
						else
						{
							ImGui.SetCursorPosX(cursorFromRight - itemWidth);
							cursorFromRight -= itemWidth + itemPadding;
							if (item.ShowSeparator)
								drawList.AddLine(
									new(windowPos.X + cursorFromRight + itemPadding / 2f, windowPos.Y),
									new(windowPos.X + cursorFromRight + itemPadding / 2f, windowPos.Y + frameHeight),
									ImGui.GetColorU32(ImGuiCol.TextDisabled));
						}

						if (item.TextAlignment != StatusBarItemTextAlign.Left)
						{
							var cursorPos = ImGui.GetCursorPosX();

							if (item.TextAlignment == StatusBarItemTextAlign.Right)
								ImGui.SetCursorPosX(cursorPos + itemWidth - labelWidth);
							else if (item.TextAlignment == StatusBarItemTextAlign.Center)
								ImGui.SetCursorPosX(cursorPos + (itemWidth / 2f - labelWidth / 2f));

							if (item.IsEnabled) ImGui.Text(item.Label);
							else ImGui.TextDisabled(item.Label);

							ImGui.SetCursorPosX(cursorPos);
						}
						else
						{
							if (item.IsEnabled) ImGui.Text(item.Label);
							else ImGui.TextDisabled(item.Label);
						}
					}

					ImGui.EndMenuBar();
				}
			}
		}
	}

	public enum StatusBarItemAlign { Left, Right }
	public enum StatusBarItemTextAlign { Left, Right, Center }

	public class StatusBarItem
	{
		public string Label { get; set; } = string.Empty;
		public float Width { get; set; } = 0f;
		public StatusBarItemAlign ItemAlignment { get; set; } = StatusBarItemAlign.Left;
		public StatusBarItemTextAlign TextAlignment { get; set; } = StatusBarItemTextAlign.Left;
		public bool ShowSeparator { get; set; } = true;
		public bool IsEnabled { get; set; } = true;

		public StatusBarItem(string label = "") => Label = label;
	}
}

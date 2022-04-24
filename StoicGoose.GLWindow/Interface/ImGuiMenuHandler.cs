using System;
using System.Collections.Generic;

using ImGuiNET;

namespace StoicGoose.GLWindow.Interface
{
	public class ImGuiMenuHandler
	{
		readonly List<MenuItem> menuItems = new();

		public ImGuiMenuHandler(params MenuItem[] menuItems)
		{
			this.menuItems.AddRange(menuItems);
		}

		public void AddMenu(MenuItem menuItem)
		{
			menuItems.Add(menuItem);
		}

		public void Draw()
		{
			if (ImGui.BeginMainMenuBar())
			{
				foreach (var menuItem in menuItems)
					DrawMenu(menuItem);

				ImGui.EndMainMenuBar();
			}
		}

		private void DrawMenu(MenuItem menuItem)
		{
			if (menuItem.Label == "-")
				ImGui.Separator();
			else
			{
				if (menuItem.Action == null || menuItem.SubItems.Length > 0)
					DrawSubMenus(menuItem);
				else
				{
					if (ImGui.MenuItem(menuItem.Label) && menuItem.Action != null)
						menuItem.Action();
				}
			}
		}

		private void DrawSubMenus(MenuItem menuItem)
		{
			if (ImGui.BeginMenu(menuItem.Label))
			{
				foreach (var subItem in menuItem.SubItems)
					DrawMenu(subItem);

				ImGui.EndMenu();
			}
		}
	}

	public class MenuItem
	{
		public string Label { get; set; } = string.Empty;
		public Action Action { get; set; } = default;
		public MenuItem[] SubItems { get; set; } = Array.Empty<MenuItem>();

		public MenuItem(string label, Action action = null)
		{
			Label = label;
			Action = action;
		}
	}
}

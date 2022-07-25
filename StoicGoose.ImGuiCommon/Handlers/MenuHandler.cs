using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ImGuiNET;

namespace StoicGoose.ImGuiCommon.Handlers
{
	public class MenuHandler
	{
		readonly List<MenuItem> menuItems = new();

		public MenuHandler(params MenuItem[] menuItems)
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
				if (menuItem.ClickAction == null || menuItem.SubItems.Length > 0)
					DrawSubMenus(menuItem);
				else
				{
					menuItem.UpdateAction?.Invoke(menuItem);
					if (ImGui.MenuItem(menuItem.Label, null, menuItem.IsChecked, menuItem.IsEnabled) && menuItem.ClickAction != null)
						menuItem.ClickAction(menuItem);
				}
			}
		}

		private void DrawSubMenus(MenuItem menuItem)
		{
			if (ImGui.BeginMenu(menuItem.Label ?? nameof(MenuItem)))
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
		public string Localization { get; set; } = string.Empty;
		public Action<MenuItem> ClickAction { get; set; } = default;
		public Action<MenuItem> UpdateAction { get; set; } = default;
		public MenuItem[] SubItems { get; set; } = Array.Empty<MenuItem>();
		public bool IsEnabled { get; set; } = true;
		public bool IsChecked { get; set; } = false;

		public MenuItem(string label = "", string localization = "", Action<MenuItem> clickAction = null, Action<MenuItem> updateAction = null)
		{
			Label = label;
			Localization = localization;
			ClickAction = clickAction;
			UpdateAction = updateAction;
		}
	}
}

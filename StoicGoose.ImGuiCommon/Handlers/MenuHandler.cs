using ImGuiNET;
using System;
using System.Collections.Generic;

namespace StoicGoose.ImGuiCommon.Handlers
{
    public class MenuHandler
    {
        readonly List<MenuItem> menuItems = [];

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

    public class MenuItem(string label = "", string localization = "", Action<MenuItem> clickAction = null, Action<MenuItem> updateAction = null)
    {
        public string Label { get; set; } = label;
        public string Localization { get; set; } = localization;
        public Action<MenuItem> ClickAction { get; set; } = clickAction;
        public Action<MenuItem> UpdateAction { get; set; } = updateAction;
        public MenuItem[] SubItems { get; set; } = [];
        public bool IsEnabled { get; set; } = true;
        public bool IsChecked { get; set; } = false;
    }
}

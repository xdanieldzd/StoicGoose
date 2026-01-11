using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.ImGuiCommon.Windows
{
    public abstract class WindowBase
    {
        protected bool isWindowOpen = false;
        protected bool isFirstOpen = true;

        public bool IsWindowOpen { get => isWindowOpen; set => isWindowOpen = value; }

        public string WindowTitle { get; } = string.Empty;
        public NumericsVector2 InitialWindowSize { get; } = NumericsVector2.Zero;
        public ImGuiCond SizingCondition { get; } = ImGuiCond.None;

        public bool IsFocused { get; private set; } = default;

        public WindowBase(string title)
        {
            WindowTitle = title;
        }

        public WindowBase(string title, NumericsVector2 size, ImGuiCond condition)
        {
            WindowTitle = title;
            InitialWindowSize = size;
            SizingCondition = condition;
        }

        public virtual void Draw(object userData)
        {
            if (!isWindowOpen) return;

            if (isFirstOpen)
            {
                InitializeWindow(userData);
                isFirstOpen = false;
            }

            ImGui.SetNextWindowSize(InitialWindowSize, SizingCondition);

            DrawWindow(userData);

            if (ImGui.Begin(WindowTitle))
            {
                IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
                ImGui.End();
            }
        }

        protected virtual void InitializeWindow(object userData) { }

        protected abstract void DrawWindow(object userData);
    }
}

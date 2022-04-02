using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public abstract class ImGuiWindowBase
	{
		protected bool isWindowOpen = false;

		public bool IsWindowOpen { get => isWindowOpen; set => isWindowOpen = value; }

		public string WindowTitle { get; } = string.Empty;
		public NumericsVector2 InitialWindowSize { get; } = NumericsVector2.Zero;
		public ImGuiCond SizingCondition { get; } = ImGuiCond.None;

		public ImGuiWindowBase(string title)
		{
			WindowTitle = title;
		}

		public ImGuiWindowBase(string title, NumericsVector2 size, ImGuiCond condition)
		{
			WindowTitle = title;
			InitialWindowSize = size;
			SizingCondition = condition;
		}

		public virtual void Draw(object userData)
		{
			if (!isWindowOpen) return;

			ImGui.SetNextWindowSize(InitialWindowSize, SizingCondition);

			DrawWindow(userData);
		}

		protected abstract void DrawWindow(object userData);
	}
}

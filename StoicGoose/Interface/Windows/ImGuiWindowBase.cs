using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ImGuiNET;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public abstract class ImGuiWindowBase
	{
		protected bool isWindowOpen = false;

		public bool IsWindowOpen { get => isWindowOpen; set => isWindowOpen = value; }

		public string WindowTitle { get; } = string.Empty;
		public NumericsVector2 WindowSize { get; } = NumericsVector2.Zero;
		public ImGuiCond SizingCondition { get; } = ImGuiCond.None;

		public ImGuiWindowBase(string title)
		{
			WindowTitle = title;
		}

		public ImGuiWindowBase(string title, NumericsVector2 size, ImGuiCond condition)
		{
			WindowTitle = title;
			WindowSize = size;
			SizingCondition = condition;
		}

		public virtual void Draw(params object[] args)
		{
			if (!isWindowOpen) return;

			ImGui.SetNextWindowSize(WindowSize, SizingCondition);

			DrawWindow(args);
		}

		protected abstract void DrawWindow(params object[] args);
	}
}

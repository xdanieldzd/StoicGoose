using System;

using ImGuiNET;

using StoicGoose.Common.OpenGL;
using StoicGoose.ImGuiCommon.Windows;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class DisplayWindow : WindowBase
	{
		public DisplayWindow() : base("Display") { }

		int windowScale = 1;

		public int WindowScale
		{
			get => windowScale;
			set => windowScale = value;
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not (Texture texture, bool vertical)) return;

			var textureSize = new NumericsVector2(
				!vertical ? texture.Size.X : texture.Size.Y,
				!vertical ? texture.Size.Y : texture.Size.X)
				* windowScale;

			var childBorderSize = new NumericsVector2(ImGui.GetStyle().ChildBorderSize);

			ImGui.SetNextWindowContentSize(textureSize + (childBorderSize * 2f));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, NumericsVector2.Zero);
			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				var drawList = ImGui.GetWindowDrawList();
				var screenPos = ImGui.GetCursorScreenPos();

				drawList.AddImage(new IntPtr(texture.Handle),
					screenPos + childBorderSize,
					screenPos + textureSize + childBorderSize);

				if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("context");

				ImGui.PopStyleVar();

				if (ImGui.BeginPopup("context"))
				{
					ImGui.SliderInt("##size", ref windowScale, 1, 5, "%dx");
					ImGui.EndPopup();
				}

				ImGui.End();
			}
			else
				ImGui.PopStyleVar();
		}
	}
}

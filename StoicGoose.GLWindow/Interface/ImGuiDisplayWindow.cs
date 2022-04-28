using System;

using ImGuiNET;

using StoicGoose.Common.OpenGL;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class ImGuiDisplayWindow : ImGuiWindowBase
	{
		public ImGuiDisplayWindow() : base("Display") { }

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

			ImGui.SetNextWindowContentSize(textureSize);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, NumericsVector2.Zero);
			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				var drawList = ImGui.GetWindowDrawList();
				var screenPos = ImGui.GetCursorScreenPos();

				var pos = new NumericsVector2[4];
				var uvs = new NumericsVector2[4];

				if (vertical)
				{
					pos[0] = screenPos + new NumericsVector2(textureSize.X, 0f);
					pos[3] = screenPos + new NumericsVector2(textureSize.X, textureSize.Y);
					pos[2] = screenPos + new NumericsVector2(0f, textureSize.Y);
					pos[1] = screenPos + new NumericsVector2(0f, 0f);

					uvs[0] = new NumericsVector2(1f, 1f);
					uvs[1] = new NumericsVector2(1f, 0f);
					uvs[2] = new NumericsVector2(0f, 0f);
					uvs[3] = new NumericsVector2(0f, 1f);
				}
				else
				{
					pos[0] = screenPos + new NumericsVector2(0f, 0f);
					pos[1] = screenPos + new NumericsVector2(0f, textureSize.Y);
					pos[2] = screenPos + new NumericsVector2(textureSize.X, textureSize.Y);
					pos[3] = screenPos + new NumericsVector2(textureSize.X, 0f);

					uvs[0] = new NumericsVector2(0f, 0f);
					uvs[1] = new NumericsVector2(0f, 1f);
					uvs[2] = new NumericsVector2(1f, 1f);
					uvs[3] = new NumericsVector2(1f, 0f);
				}

				drawList.AddImageQuad(
					new IntPtr(texture.Handle),
					pos[0], pos[1], pos[2], pos[3],
					uvs[0], uvs[1], uvs[2], uvs[3]);

				if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("context");

				ImGui.PopStyleVar();

				if (ImGui.BeginPopup("context"))
				{
					ImGui.SliderInt("##size", ref windowScale, 1, 5, "Scale %dx");
					ImGui.EndPopup();
				}

				ImGui.End();
			}
			else
				ImGui.PopStyleVar();
		}
	}
}

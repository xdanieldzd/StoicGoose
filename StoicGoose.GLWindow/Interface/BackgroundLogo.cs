using System;

using OpenTK.Graphics.OpenGL4;

using ImGuiNET;

using StoicGoose.Common.Drawing;
using StoicGoose.Common.OpenGL;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class BackgroundLogo
	{
		readonly NumericsVector2 bottomRightOffset = new(32f);
		readonly byte imageAlpha = 64;

		readonly Texture backgroundTexture = default;
		readonly NumericsVector2 imageSize = default;

		public BackgroundLogo(RgbaFile background)
		{
			backgroundTexture = new(background);
			backgroundTexture.SetTextureFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
			imageSize = new(backgroundTexture.Size.X, backgroundTexture.Size.Y);
		}

		public void Draw()
		{
			var position = ImGui.GetMainViewport().Size - imageSize - bottomRightOffset;
			var backgroundDrawList = ImGui.GetBackgroundDrawList();
			backgroundDrawList.AddImage(new IntPtr(backgroundTexture.Handle), position, position + imageSize, NumericsVector2.Zero, NumericsVector2.One, (uint)(imageAlpha << 24));
		}
	}
}

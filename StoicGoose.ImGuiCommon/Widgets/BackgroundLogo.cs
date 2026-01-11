using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using StoicGoose.Common.Drawing;
using StoicGoose.Common.OpenGL;
using System;
using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.ImGuiCommon.Widgets
{
    public class BackgroundLogo
    {
        public Texture Texture { get; set; } = default;
        public NumericsVector2 Size => new((float)Texture?.Size.X, (float)Texture?.Size.Y);

        public BackgroundLogoPositioning Positioning { get; set; } = BackgroundLogoPositioning.Center;
        public NumericsVector2 Offset { get; set; } = NumericsVector2.Zero;
        public NumericsVector2 Scale { get; set; } = NumericsVector2.One;
        public byte Alpha { get; set; } = 255;

        readonly bool debug = false;

        public void SetImage(RgbaFile background)
        {
            Texture = new(background);
            Texture.SetTextureFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            Texture.SetTextureWrapMode(TextureWrapMode.Repeat, TextureWrapMode.Repeat);
        }

        public void Draw()
        {
            if (Texture == default) return;

            var scaledSize = Size * Scale;
            var position = Positioning switch
            {
                BackgroundLogoPositioning.Center => ImGui.GetMainViewport().Size / 2f - scaledSize / 2f + Offset,
                BackgroundLogoPositioning.TopLeft => Offset,
                BackgroundLogoPositioning.TopRight => new(ImGui.GetMainViewport().Size.X - scaledSize.X + Offset.X, Offset.Y),
                BackgroundLogoPositioning.BottomLeft => new(Offset.X, ImGui.GetMainViewport().Size.Y - scaledSize.Y + Offset.Y),
                BackgroundLogoPositioning.BottomRight => ImGui.GetMainViewport().Size - scaledSize + Offset,
                _ => throw new Exception("Invalid positioning mode"),
            };

            var backgroundDrawList = ImGui.GetBackgroundDrawList();
            backgroundDrawList.AddImage(new IntPtr(Texture.Handle), position, position + scaledSize, NumericsVector2.Zero, NumericsVector2.One, (uint)(Alpha << 24));

            if (debug) backgroundDrawList.AddRect(position, position + scaledSize, 0xFF0000FF);
        }
    }

    public enum BackgroundLogoPositioning { Center, TopLeft, TopRight, BottomLeft, BottomRight }
}

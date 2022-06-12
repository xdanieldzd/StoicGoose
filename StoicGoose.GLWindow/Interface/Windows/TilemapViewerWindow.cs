using System;

using OpenTK.Graphics.OpenGL4;

using ImGuiNET;

using StoicGoose.Common.OpenGL;
using StoicGoose.Common.Utilities;
using StoicGoose.Core.Display;
using StoicGoose.Core.Interfaces;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class TilemapViewerWindow : WindowBase
	{
		const float zoom = 2f;

		readonly static (int x, int y) tileSize = (8, 8);
		readonly static (int x, int y) mapSize = (32, 32);

		uint hoveredHighlightColor = 0, selectedHighlightColor = 0;
		(int x, int y) hoveredTile = (0, 0), selectedTile = (0, 0);

		readonly ushort[] tileAttribs = new ushort[mapSize.x * mapSize.y];
		int scrNumber = 0;

		readonly byte[] mapTextureData = new byte[mapSize.x * tileSize.x * mapSize.y * tileSize.y * 4];
		readonly Texture mapTexture = new(mapSize.x * tileSize.x, mapSize.y * tileSize.y);

		public TilemapViewerWindow() : base("Tilemap Viewer", new NumericsVector2(750f, 580f), ImGuiCond.Always) { }

		protected override void InitializeWindow(object userData)
		{
			if (userData is not IMachine) return;

			hoveredHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.Border) & 0x00FFFFFF);
			selectedHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);

			mapTexture.SetTextureFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
			mapTexture.SetTextureWrapMode(TextureWrapMode.Repeat, TextureWrapMode.Repeat);
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not IMachine machine) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				UpdateTilemapTexture(machine);

				var tilemapRenderSize = new NumericsVector2(mapSize.x * tileSize.x * zoom, mapSize.y * tileSize.y * zoom);

				var childBorderSize = new NumericsVector2(ImGui.GetStyle().ChildBorderSize);

				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, NumericsVector2.Zero);
				if (ImGui.BeginChild("##tilemap", tilemapRenderSize + (childBorderSize * 2f), true))
				{
					/* Prevent window from being dragged around if inside tilemap view */
					ImGui.InvisibleButton("##tilemap-dummybutton", tilemapRenderSize);

					var drawList = ImGui.GetWindowDrawList();
					var tilemapPos = ImGui.GetWindowPos();

					mapTexture.Bind();
					drawList.AddImage(
						new IntPtr(mapTexture.Handle),
						tilemapPos + childBorderSize,
						tilemapPos + tilemapRenderSize + childBorderSize);

					var rectSize = new NumericsVector2(tileSize.x * zoom, tileSize.y * zoom);
					var mousePosition = ImGui.GetMousePos();

					for (var x = 0; x < mapSize.x; x++)
					{
						for (var y = 0; y < mapSize.y; y++)
						{
							var rectPos = tilemapPos + new NumericsVector2(x * tileSize.x * zoom, y * tileSize.y * zoom) + childBorderSize;

							if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && Helpers.IsPointInsideRectangle(mousePosition, rectPos, rectSize))
							{
								hoveredTile = (x, y);
								if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
									selectedTile = hoveredTile;
							}

							if (selectedTile == (x, y))
								drawList.AddRectFilled(rectPos, rectPos + rectSize, selectedHighlightColor);
							else if (hoveredTile == (x, y))
								drawList.AddRectFilled(rectPos, rectPos + rectSize, hoveredHighlightColor);
						}
					}

					ImGui.EndChild();
				}
				ImGui.SameLine();

				ImGui.PopStyleVar();

				if (ImGui.BeginChild("##attribs", new NumericsVector2(0f, mapSize.y * tileSize.y * zoom)))
				{
					var coordsOffset = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(new string('X', 12)).X;
					var valueOffset = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(new string('X', 6)).X;

					ImGui.Text("Coordinates"); ImGui.SameLine(); ImGui.SameLine(coordsOffset); ImGui.Text($"X: {selectedTile.x,2}, Y: {selectedTile.y,2}");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					var attribs = tileAttribs[(selectedTile.y << 5) + selectedTile.x];
					ImGui.Text("Attributes"); ImGui.SameLine(); ImGui.SameLine(valueOffset); ImGui.Text($"0x{attribs:X4}");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					var tileNum = (ushort)(attribs & 0x01FF);
					ImGui.BulletText("Tile"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B0-8)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.Text($"0x{tileNum:X4}");
					var tilePal = (byte)((attribs >> 9) & 0b1111);
					ImGui.BulletText("Palette"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B9-12)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.Text($"{tilePal,6}");

					if (machine.DisplayController is SphinxDisplayController sphinxDisplay)
					{
						var tileBank = (sphinxDisplay.DisplayColorFlagSet || sphinxDisplay.Display4bppFlagSet ? ((attribs >> 13) & 0b1) : 0);
						ImGui.BulletText("Tile Bank"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B13)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.Text($"{tileBank,6}");
					}

					var tileFlipH = ((attribs >> 15) & 0b1) == 0b1;
					ImGui.BulletText("Horz. flip"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B14)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.BeginDisabled(!tileFlipH); ImGui.Text($"{tileFlipH,6}"); ImGui.EndDisabled();
					var tileFlipV = ((attribs >> 14) & 0b1) == 0b1;
					ImGui.BulletText("Vert. flip"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B15)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.BeginDisabled(!tileFlipV); ImGui.Text($"{tileFlipV,6}"); ImGui.EndDisabled();

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					var uv0 = new NumericsVector2(selectedTile.x / (float)mapSize.x, selectedTile.y / (float)mapSize.y);
					var uv1 = new NumericsVector2((selectedTile.x + 1) / (float)mapSize.x, (selectedTile.y + 1) / (float)mapSize.y);

					mapTexture.Bind();
					ImGui.Image(new IntPtr(mapTexture.Handle), new NumericsVector2(tileSize.x * 24f, tileSize.y * 24f), uv0, uv1);

					ImGui.EndChild();
				}

				if (ImGui.BeginChild("##options"))
				{
					ImGui.RadioButton("SCR1", ref scrNumber, 0); ImGui.SameLine();
					ImGui.RadioButton("SCR2", ref scrNumber, 1);
					ImGui.EndChild();
				}

				ImGui.End();
			}
		}

		private void UpdateTilemapTexture(IMachine machine)
		{
			var sphinxDisplay = machine.DisplayController as SphinxDisplayController;
			var colorMode = sphinxDisplay?.DisplayColorFlagSet == true || sphinxDisplay?.Display4bppFlagSet == true;

			for (var y = 0; y < mapSize.y * tileSize.y; y++)
			{
				for (var x = 0; x < mapSize.x * tileSize.x; x++)
				{
					var mapBase = scrNumber == 0 ? machine.DisplayController.Scr1Base : machine.DisplayController.Scr2Base;
					var mapOffset = (uint)((mapBase << 11) | ((y >> 3) << 6) | ((x >> 3) << 1));

					var attribs = tileAttribs[((y >> 3) << 5) + (x >> 3)] = ReadMemory16(machine, mapOffset);
					var tileNum = (ushort)(attribs & 0x01FF);
					tileNum |= (ushort)(colorMode ? (((attribs >> 13) & 0b1) << 9) : 0);

					var tilePal = (byte)((attribs >> 9) & 0b1111);

					var color = GetPixelColor(machine, tileNum, y ^ (((attribs >> 15) & 0b1) * 7), x ^ (((attribs >> 14) & 0b1) * 7));

					if (color != 0 || (!colorMode && color == 0 && !BitHandling.IsBitSet(tilePal, 2)))
					{
						if (colorMode)
							WriteToTexture(y, x, tilePal, color, machine);
						else
							WriteToTexture(y, x, (byte)(15 - machine.DisplayController.PalMonoPools[machine.DisplayController.PalMonoData[tilePal][color & 0b11]]));
					}
					else
					{
						if (colorMode)
							WriteToTexture(y, x, sphinxDisplay.BackColorPalette, sphinxDisplay.BackColorIndex, machine);
						else
							WriteToTexture(y, x, (byte)(15 - machine.DisplayController.PalMonoPools[machine.DisplayController.BackColorIndex & 0b0111]));
					}
				}
			}
			mapTexture.Update(mapTextureData);
		}

		private static ushort ReadMemory16(IMachine machine, uint address) => (ushort)(machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));
		private static uint ReadMemory32(IMachine machine, uint address) => (uint)(machine.ReadMemory(address + 3) << 24 | machine.ReadMemory(address + 2) << 16 | machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));

		private static byte GetPixelColor(IMachine machine, ushort tile, int y, int x)
		{
			if (machine.DisplayController is SphinxDisplayController sphinxDisplay && sphinxDisplay.DisplayColorFlagSet && sphinxDisplay.Display4bppFlagSet)
			{
				if (!machine.DisplayController.DisplayPackedFormatSet)
				{
					var data = ReadMemory32(machine, (uint)(0x4000 + ((tile & 0x03FF) << 5) + ((y % 8) << 2)));
					return (byte)((((data >> 31 - (x % 8)) & 0b1) << 3 | ((data >> 23 - (x % 8)) & 0b1) << 2 | ((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b1111);
				}
				else
				{
					var data = machine.ReadMemory((ushort)(0x4000 + ((tile & 0x03FF) << 5) + ((y % 8) << 2) + ((x % 8) >> 1)));
					return (byte)((data >> 4 - (((x % 8) & 0b1) << 2)) & 0b1111);
				}
			}
			else
			{
				if (!machine.DisplayController.DisplayPackedFormatSet)
				{
					var data = ReadMemory16(machine, (uint)(0x2000 + (tile << 4) + ((y % 8) << 1)));
					return (byte)((((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b11);
				}
				else
				{
					var data = machine.ReadMemory((uint)(0x2000 + (tile << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
					return (byte)((data >> 6 - (((x % 8) & 0b11) << 1)) & 0b11);
				}
			}
		}

		private void WriteToTexture(int y, int x, byte pixel)
		{
			byte r, g, b;
			r = g = b = (byte)((pixel << 4) | pixel);
			WriteToTexture(y, x, r, g, b);
		}

		private void WriteToTexture(int y, int x, byte palette, byte color, IMachine machine)
		{
			var pixel = ReadMemory16(machine, (uint)(0x0FE00 + (palette << 5) + (color << 1)));
			int b = (pixel >> 0) & 0b1111;
			int g = (pixel >> 4) & 0b1111;
			int r = (pixel >> 8) & 0b1111;
			WriteToTexture(y, x, (byte)(r | r << 4), (byte)(g | g << 4), (byte)(b | b << 4));
		}

		private void WriteToTexture(int y, int x, byte r, byte g, byte b)
		{
			var address = ((y * mapSize.x * tileSize.x) + x) * 4;
			mapTextureData[address + 0] = r;
			mapTextureData[address + 1] = g;
			mapTextureData[address + 2] = b;
			mapTextureData[address + 3] = 255;
		}
	}
}

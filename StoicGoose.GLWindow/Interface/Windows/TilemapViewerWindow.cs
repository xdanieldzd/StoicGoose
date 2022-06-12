using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL4;

using ImGuiNET;

using StoicGoose.Common.OpenGL;
using StoicGoose.Common.Utilities;
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

			hoveredHighlightColor = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.Border) & 0x00FFFFFF);
			selectedHighlightColor = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);

			mapTexture.SetTextureFilter(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
			mapTexture.SetTextureWrapMode(TextureWrapMode.Repeat, TextureWrapMode.Repeat);
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not IMachine machine) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				ushort readMemory16(uint address) => (ushort)(machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));
				byte getPixelColor(ushort tile, int y, int x)
				{
					if (!machine.DisplayController.DisplayPackedFormatSet)
					{
						var data = readMemory16((uint)(0x2000 + (tile << 4) + ((y % 8) << 1)));
						return (byte)((((data >> 15 - (x % 8)) & 0b1) << 1 | ((data >> 7 - (x % 8)) & 0b1)) & 0b11);
					}
					else
					{
						var data = machine.ReadMemory((uint)(0x2000 + (tile << 4) + ((y % 8) << 1) + ((x % 8) >> 2)));
						return (byte)((data >> 6 - (((x % 8) & 0b11) << 1)) & 0b11);
					}
				}

				void writeToTexturePixel(int y, int x, byte pixel) => writeToTextureRgb(y, x, (byte)((pixel << 4) | pixel), (byte)((pixel << 4) | pixel), (byte)((pixel << 4) | pixel));
				void writeToTextureRgb(int y, int x, byte r, byte g, byte b)
				{
					var address = ((y * mapSize.x * tileSize.x) + x) * 4;
					mapTextureData[address + 0] = r;
					mapTextureData[address + 1] = g;
					mapTextureData[address + 2] = b;
					mapTextureData[address + 3] = 255;
				}

				for (var y = 0; y < mapSize.y * tileSize.y; y++)
				{
					for (var x = 0; x < mapSize.x * tileSize.x; x++)
					{
						var mapBase = scrNumber == 0 ? machine.DisplayController.Scr1Base : machine.DisplayController.Scr2Base;
						var mapOffset = (uint)((mapBase << 11) | ((y >> 3) << 6) | ((x >> 3) << 1));

						var attribs = tileAttribs[((y >> 3) << 5) + (x >> 3)] = readMemory16(mapOffset);
						var tileNum = (ushort)(attribs & 0x01FF);
						var tilePal = (byte)((attribs >> 9) & 0b1111);

						var color = getPixelColor(tileNum, y ^ (((attribs >> 15) & 0b1) * 7), x ^ (((attribs >> 14) & 0b1) * 7));
						if (color != 0 || (color == 0 && !BitHandling.IsBitSet(tilePal, 2)))
							writeToTexturePixel(y, x, (byte)(15 - machine.DisplayController.PalMonoPools[machine.DisplayController.PalMonoData[tilePal][color & 0b11]]));
						else
							writeToTexturePixel(y, x, (byte)(15 - machine.DisplayController.PalMonoPools[machine.DisplayController.BackColorIndex & 0b0111]));
					}
				}
				mapTexture.Update(mapTextureData);

				var gridSize = new NumericsVector2((mapSize.x * tileSize.x * zoom) + 2f, (mapSize.y * tileSize.y * zoom) + 2f);

				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, NumericsVector2.Zero);
				if (ImGui.BeginChild("##grid", gridSize, true, ImGuiWindowFlags.None))
				{
					/* Prevent window from being dragged around if inside map grid */
					ImGui.InvisibleButton("##grid-dummybutton", gridSize);

					var drawList = ImGui.GetWindowDrawList();
					var gridPos = ImGui.GetWindowPos();

					mapTexture.Bind();
					drawList.AddImage(new IntPtr(mapTexture.Handle), gridPos, gridPos + gridSize);

					var rectSize = new NumericsVector2(tileSize.x * zoom, tileSize.y * zoom);
					var mousePosition = ImGui.GetMousePos();

					for (var x = 0; x < mapSize.x; x++)
					{
						for (var y = 0; y < mapSize.y; y++)
						{
							var rectPos = new NumericsVector2(gridPos.X + (x * tileSize.x * zoom) + 1f, gridPos.Y + (y * tileSize.y * zoom) + 1f);

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
				ImGui.PopStyleVar();
				ImGui.SameLine();

				if (ImGui.BeginChild("##attribs", new NumericsVector2(0f, mapSize.y * tileSize.y * zoom)))
				{
					var coordsOffset = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(new string('X', 12)).X;
					var valueOffset = ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(new string('X', 6)).X;

					var attribs = tileAttribs[(selectedTile.y << 5) + selectedTile.x];
					var tileNum = (ushort)(attribs & 0x01FF);
					var tilePal = (byte)((attribs >> 9) & 0b1111);
					var tileFlipH = ((attribs >> 15) & 0b1) == 0b1;
					var tileFlipV = ((attribs >> 14) & 0b1) == 0b1;

					ImGui.Text("Coordinates"); ImGui.SameLine(); ImGui.SameLine(coordsOffset); ImGui.Text($"X: {selectedTile.x,2}, Y: {selectedTile.y,2}");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.Text("Attributes"); ImGui.SameLine(); ImGui.SameLine(valueOffset); ImGui.Text($"0x{attribs:X4}");

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.BulletText("Tile"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B0-8)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.Text($"0x{tileNum:X4}");
					ImGui.BulletText("Palette"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B9-12)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.Text($"{tilePal,6}");
					ImGui.BulletText("Horz. flip"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B14)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.BeginDisabled(!tileFlipH); ImGui.Text($"{tileFlipH,6}"); ImGui.EndDisabled();
					ImGui.BulletText("Vert. flip"); ImGui.SameLine(); ImGui.BeginDisabled(); ImGui.Text("(B15)"); ImGui.EndDisabled(); ImGui.SameLine(valueOffset); ImGui.BeginDisabled(!tileFlipV); ImGui.Text($"{tileFlipV,6}"); ImGui.EndDisabled();

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
	}
}

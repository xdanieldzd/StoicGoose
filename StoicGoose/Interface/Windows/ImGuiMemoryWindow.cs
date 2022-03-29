using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ImGuiNET;

using StoicGoose.Emulation.Machines;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiMemoryWindow : ImGuiWindowBase
	{
		/* Ported/adapted from https://github.com/ocornut/imgui_club/blob/d4cd9896e15a03e92702a578586c3f91bbde01e8/imgui_memory_editor/imgui_memory_editor.h */

		const int defaultMemorySize = 0x100000;

		enum DataFormat { Bin, Dec, Hex }

		bool readOnly = false;
		int cols = 16;
		bool optShowOptions = true;
		bool optShowDataPreview = true;
		bool optShowHexIi = false;
		bool optShowAscii = true;
		bool optGrayOutZeroes = true;
		bool optUpperCaseHex = true;
		int optMidColsCount = 8;
		int optAddrDigitsCount = 0;
		float optFooterExtraHeight = 0f;
		uint highlightColor = 0xFFFFFF32;

		bool contentsWidthChanged = false;
		int dataPreviewAddr = -1;
		int dataEditingAddr = -1;
		bool dataEditingTakeFocus = false;
		string dataInputBuf = new('\0', 32);
		string addrInputBuf = new('\0', 32);
		int gotoAddr = -1;
		int highlightMin = -1, highlightMax = -1;
		int previewEndianness = 0;
		ImGuiDataType previewDataType = ImGuiDataType.S32;

		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;

		int addrDigitsCount = 0;
		float lineHeight = 0f;
		float glyphWidth = 0f;
		float hexCellWidth = 0f;
		float spacingBetweenMidCols = 0f;
		float posHexStart = 0f;
		float posHexEnd = 0f;
		float posAsciiStart = 0f;
		float posAsciiEnd = 0f;
		float windowWidth = 0f;

		ImFontPtr japaneseFont = default;

		public ImGuiMemoryWindow() : base("Memory Editor", new NumericsVector2(10f, 10f), ImGuiCond.Always)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			clipperObject = new();
			clipperHandle = GCHandle.Alloc(clipperObject, GCHandleType.Pinned);
			clipperPointer = clipperHandle.AddrOfPinnedObject();
		}

		~ImGuiMemoryWindow()
		{
			clipperHandle.Free();
		}

		void GotoAddrAndHighlight(int addr_min, int addr_max)
		{
			gotoAddr = addr_min;
			highlightMin = addr_min;
			highlightMax = addr_max;
		}

		void CalcSizes(int mem_size, int base_display_addr)
		{
			var style = ImGui.GetStyle();

			addrDigitsCount = optAddrDigitsCount;
			if (addrDigitsCount == 0)
				for (var n = base_display_addr + mem_size - 1; n > 0; n >>= 4)
					addrDigitsCount++;

			lineHeight = ImGui.GetTextLineHeight();
			glyphWidth = ImGui.CalcTextSize("F").X + 1;
			hexCellWidth = (int)(glyphWidth * 2.5f);
			spacingBetweenMidCols = (int)(hexCellWidth * 0.25f);
			posHexStart = (addrDigitsCount + 2) * glyphWidth;
			posHexEnd = posHexStart + (hexCellWidth * cols);
			posAsciiStart = posAsciiEnd = posHexEnd;

			if (optShowAscii)
			{
				posAsciiStart = posHexEnd + glyphWidth;
				if (optMidColsCount > 0)
					posAsciiStart += ((cols + optMidColsCount - 1) / optMidColsCount) * spacingBetweenMidCols;
				posAsciiEnd = posAsciiStart + cols * glyphWidth;
			}

			windowWidth = posAsciiEnd + style.ScrollbarSize + style.WindowPadding.X * 2 + glyphWidth;
		}

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not IMachine machine) return;
			if (machine.Cartridge.Metadata == null) return;

			if (japaneseFont.Equals(default(ImFontPtr)))
				japaneseFont = ImGui.GetIO().Fonts.Fonts[1];

			CalcSizes(defaultMemorySize, 0);

			ImGui.SetNextWindowSize(new NumericsVector2(windowWidth, windowWidth * 0.6f), ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(NumericsVector2.Zero, new NumericsVector2(windowWidth, float.MaxValue));

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoScrollbar))
			{
				if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("context");

				DrawContents(machine, defaultMemorySize, 0);

				if (contentsWidthChanged)
				{
					CalcSizes(defaultMemorySize, 0);
					ImGui.SetNextWindowSize(new NumericsVector2(windowWidth, ImGui.GetWindowSize().Y));
				}

				ImGui.End();
			}
		}

		private void DrawContents(IMachine machine, int mem_size, int base_display_addr)
		{
			if (cols < 1) cols = 1;

			var style = ImGui.GetStyle();

			var height_separator = style.ItemSpacing.Y;
			var footer_height = optFooterExtraHeight;
			if (optShowOptions)
				footer_height += height_separator + ImGui.GetFrameHeightWithSpacing();
			if (optShowDataPreview)
				footer_height += height_separator + ImGui.GetFrameHeightWithSpacing() + (ImGui.GetTextLineHeightWithSpacing() * 4);

			ImGui.BeginChild("##scrolling", new NumericsVector2(0f, -footer_height), false, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNav);
			{
				var draw_list = ImGui.GetWindowDrawList();

				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
				ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

				var line_count_total = (mem_size + cols - 1) / cols;

				var clipper = new ImGuiListClipperPtr(clipperPointer);
				clipper.Begin(line_count_total, lineHeight);
				{
					var data_next = false;

					if (readOnly || dataEditingAddr >= mem_size)
						dataEditingAddr = -1;
					if (dataPreviewAddr >= mem_size)
						dataPreviewAddr = -1;

					var preview_data_type_size = optShowDataPreview ? DataTypeGetSize(previewDataType) : 0;

					var data_editing_addr_next = -1;
					if (dataEditingAddr != -1)
					{
						if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && dataEditingAddr >= cols) { data_editing_addr_next = dataEditingAddr - cols; }
						else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow) && dataEditingAddr < mem_size - cols) { data_editing_addr_next = dataEditingAddr + cols; }
						else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow) && dataEditingAddr > 0) { data_editing_addr_next = dataEditingAddr - 1; }
						else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow) && dataEditingAddr < mem_size - 1) { data_editing_addr_next = dataEditingAddr + 1; }
					}

					var window_pos = ImGui.GetWindowPos();
					if (optShowAscii)
						draw_list.AddLine(new NumericsVector2(window_pos.X + posAsciiStart - glyphWidth, window_pos.Y), new NumericsVector2(window_pos.X + posAsciiStart - glyphWidth, window_pos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

					var color_text = ImGui.GetColorU32(ImGuiCol.Text);
					var color_disabled = optGrayOutZeroes ? ImGui.GetColorU32(ImGuiCol.TextDisabled) : color_text;

					while (clipper.Step())
					{
						for (var line_i = clipper.DisplayStart; line_i < clipper.DisplayEnd; line_i++)
						{
							var addr = line_i * cols;

							ImGui.Text(string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}{addrDigitsCount}}}: ", base_display_addr + addr));

							for (var n = 0; n < cols && addr < mem_size; n++, addr++)
							{
								var byte_pos_x = posHexStart + hexCellWidth * n;
								if (optMidColsCount > 0)
									byte_pos_x += (n / optMidColsCount) * spacingBetweenMidCols;

								ImGui.SameLine(byte_pos_x);

								bool is_highlight_from_user_range = (addr >= highlightMin && addr < highlightMax);
								bool is_highlight_from_preview = (addr >= dataPreviewAddr && addr < dataPreviewAddr + preview_data_type_size) && dataPreviewAddr != -1;

								if (is_highlight_from_user_range || is_highlight_from_preview)
								{
									var pos = ImGui.GetCursorScreenPos();
									var highlight_width = glyphWidth * 2;
									var is_next_byte_highlighted = (addr + 1 < mem_size) && highlightMax != -1 && addr + 1 < highlightMax;
									if (is_next_byte_highlighted || (n + 1 == cols))
									{
										highlight_width = hexCellWidth;
										if (optMidColsCount > 0 && n > 0 && (n + 1) < cols && ((n + 1) % optMidColsCount) == 0)
											highlight_width += spacingBetweenMidCols;
									}

									draw_list.AddRectFilled(pos, new NumericsVector2(pos.X + highlight_width, pos.Y + lineHeight), highlightColor);
								}

								if (dataEditingAddr == addr)
								{
									unsafe
									{
										var data_write = false;
										ImGui.PushID(addr);
										if (dataEditingTakeFocus)
										{
											ImGui.SetKeyboardFocusHere(0);
											addrInputBuf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}{addrDigitsCount}}}", base_display_addr + addr);
											dataInputBuf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}}", machine.ReadMemory((uint)addr));
										}

										var user_data = new UserData()
										{
											CursorPos = -1,
											CurrentBufOverwrite = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}}", machine.ReadMemory((uint)addr))
										};

										int callback(ImGuiInputTextCallbackData* data)
										{
											var dataPtr = new ImGuiInputTextCallbackDataPtr(data);

											if (!dataPtr.HasSelection())
												user_data.CursorPos = data->CursorPos;

											if (dataPtr.SelectionStart == 0 && dataPtr.SelectionEnd == dataPtr.BufTextLen)
											{
												dataPtr.DeleteChars(0, dataPtr.BufTextLen);
												dataPtr.InsertChars(0, user_data.CurrentBufOverwrite);
												dataPtr.SelectionStart = 0;
												dataPtr.SelectionEnd = 2;
												dataPtr.CursorPos = 0;
											}

											return 0;
										}

										var flags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.AlwaysOverwrite;

										ImGui.SetNextItemWidth(glyphWidth * 2);
										if (ImGui.InputText("##data", ref dataInputBuf, 32, flags, callback))
											data_write = data_next = true;
										else if (!dataEditingTakeFocus && !ImGui.IsItemActive())
											dataEditingAddr = data_editing_addr_next = -1;

										dataEditingTakeFocus = false;
										if (user_data.CursorPos >= 2)
											data_write = data_next = true;
										if (data_editing_addr_next != -1)
											data_write = data_next = false;

										if (data_write)
											machine.WriteMemory((uint)addr, byte.Parse(dataInputBuf, System.Globalization.NumberStyles.HexNumber));

										ImGui.PopID();
									}
								}
								else
								{
									var b = machine.ReadMemory((uint)addr);

									if (optShowHexIi)
									{
										if (b >= 32 && b < 128)
											ImGui.Text($"{(char)b} ");
										else if (b == 0xFF && optGrayOutZeroes)
											ImGui.TextDisabled("## ");
										else if (b == 0x00)
											ImGui.Text("   ");
										else
											ImGui.Text(string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}} ", b));
									}
									else
									{
										if (b == 0 && optGrayOutZeroes)
											ImGui.TextDisabled("00 ");
										else
											ImGui.Text(string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}} ", b));
									}

									if (!readOnly && ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
									{
										dataEditingTakeFocus = true;
										data_editing_addr_next = addr;
									}
								}
							}

							if (optShowAscii)
							{
								ImGui.SameLine(posAsciiStart);

								var pos = ImGui.GetCursorScreenPos();
								addr = line_i * cols;

								ImGui.PushID(line_i);
								if (ImGui.InvisibleButton("ascii", new NumericsVector2(posAsciiEnd - posAsciiStart, lineHeight)))
								{
									dataEditingAddr = dataPreviewAddr = addr + (int)((ImGui.GetIO().MousePos.X - pos.X) / glyphWidth);
									dataEditingTakeFocus = true;
								}
								ImGui.PopID();

								for (var n = 0; n < cols && addr < mem_size; n++, addr++)
								{
									if (addr == dataEditingAddr)
									{
										draw_list.AddRectFilled(pos, new NumericsVector2(pos.X + glyphWidth, pos.Y + lineHeight), ImGui.GetColorU32(ImGuiCol.FrameBg));
										draw_list.AddRectFilled(pos, new NumericsVector2(pos.X + glyphWidth, pos.Y + lineHeight), ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
									}

									var c = (char)machine.ReadMemory((uint)addr);
									var display_c = (c < 32 || c >= 128) ? '.' : c;

									draw_list.AddText(pos, (display_c == c) ? color_text : color_disabled, new string(new char[] { display_c }));
									pos.X += glyphWidth;
								}
							}
						}
					}
					clipper.End();

					ImGui.PopStyleVar(2);
					ImGui.EndChild();

					ImGui.SetCursorPosX(windowWidth);

					if (data_next && dataEditingAddr + 1 < mem_size)
					{
						dataEditingAddr = dataPreviewAddr = dataEditingAddr + 1;
						dataEditingTakeFocus = true;
					}
					else if (data_editing_addr_next != -1)
					{
						dataEditingAddr = dataPreviewAddr = data_editing_addr_next;
						dataEditingTakeFocus = true;
					}

					if (optShowOptions)
					{
						ImGui.Separator();
						DrawOptionsLine(mem_size, base_display_addr);
					}

					if (optShowDataPreview)
					{
						ImGui.Separator();
						DrawPreviewLine(machine, mem_size, base_display_addr);
					}
				}
			}
		}

		private void DrawOptionsLine(int mem_size, int base_display_addr)
		{
			var style = ImGui.GetStyle();

			if (ImGui.Button("Options"))
				ImGui.OpenPopup("context");
			if (ImGui.BeginPopup("context"))
			{
				ImGui.SetNextItemWidth(glyphWidth * 7f + style.FramePadding.X * 2f);
				if (ImGui.DragInt("##cols", ref cols, 0.2f, 4, 32, "%d cols")) { contentsWidthChanged = true; if (cols < 1) cols = 1; }
				ImGui.Checkbox("Show Data Preview", ref optShowDataPreview);
				ImGui.Checkbox("Show HexII", ref optShowHexIi);
				if (ImGui.Checkbox("Show ASCII", ref optShowAscii)) contentsWidthChanged = true;
				ImGui.Checkbox("Gray out zeroes", ref optGrayOutZeroes);
				ImGui.Checkbox("Uppercase hex", ref optUpperCaseHex);

				ImGui.EndPopup();
			}

			ImGui.SameLine();
			ImGui.Text(string.Format($"Range {{0:{(optUpperCaseHex ? "X" : "x")}{addrDigitsCount}}}..{{1:{(optUpperCaseHex ? "X" : "x")}{addrDigitsCount}}}", base_display_addr, base_display_addr + mem_size - 1));
			ImGui.SameLine();

			ImGui.SetNextItemWidth((addrDigitsCount + 1) * glyphWidth + style.FramePadding.X * 2f);
			if (ImGui.InputText("##addr", ref addrInputBuf, 32, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue))
			{
				gotoAddr = int.Parse(addrInputBuf, System.Globalization.NumberStyles.HexNumber) - base_display_addr;
				highlightMin = highlightMax = -1;
			}

			if (gotoAddr != -1)
			{
				if (gotoAddr < mem_size)
				{
					ImGui.BeginChild("##scrolling");
					ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + (gotoAddr / cols) * lineHeight);
					ImGui.EndChild();

					dataEditingAddr = dataPreviewAddr = gotoAddr;
					dataEditingTakeFocus = false;
				}
				gotoAddr = -1;
			}
		}

		private void DrawPreviewLine(IMachine machine, int mem_size, int base_display_addr)
		{
			var style = ImGui.GetStyle();

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Preview as:");
			ImGui.SameLine();

			ImGui.SetNextItemWidth((glyphWidth * 10f) + style.FramePadding.X * 2f + style.ItemInnerSpacing.X);
			if (ImGui.BeginCombo("##combo_type", DataTypeGetDesc(previewDataType), ImGuiComboFlags.HeightLargest))
			{
				for (var n = 0; n < (int)ImGuiDataType.COUNT; n++)
					if (ImGui.Selectable(DataTypeGetDesc((ImGuiDataType)n), previewDataType == (ImGuiDataType)n))
						previewDataType = (ImGuiDataType)n;
				ImGui.EndCombo();
			}
			ImGui.SameLine();
			ImGui.SetNextItemWidth((glyphWidth * 6f) + style.FramePadding.X * 2f + style.ItemInnerSpacing.X);
			ImGui.Combo("##combo_endianness", ref previewEndianness, "LE\0BE\0\0");

			var buf = string.Empty;
			var x = glyphWidth * 6f;
			var has_value = dataPreviewAddr != -1;

			if (has_value)
				DrawPreviewData(machine, dataPreviewAddr, mem_size, previewDataType, DataFormat.Dec, ref buf);
			ImGui.Text("Dec"); ImGui.SameLine(x); ImGui.TextUnformatted(has_value ? buf : "N/A");

			if (has_value)
				DrawPreviewData(machine, dataPreviewAddr, mem_size, previewDataType, DataFormat.Hex, ref buf);
			ImGui.Text("Hex"); ImGui.SameLine(x); ImGui.TextUnformatted(has_value ? buf : "N/A");

			if (has_value)
				DrawPreviewData(machine, dataPreviewAddr, mem_size, previewDataType, DataFormat.Bin, ref buf);
			ImGui.Text("Bin"); ImGui.SameLine(x); ImGui.TextUnformatted(has_value ? buf : "N/A");

			ImGui.Text("Sjs"); ImGui.SameLine(x);
			if (has_value)
			{
				var sjsbuf = new byte[64];
				for (var i = 0; i < sjsbuf.Length && i < mem_size; i++) sjsbuf[i] = machine.ReadMemory((uint)(dataPreviewAddr + i));
				buf = Encoding.GetEncoding(932).GetString(sjsbuf);
				ImGui.PushFont(japaneseFont);
				ImGui.TextUnformatted(buf);
				ImGui.PopFont();
			}
			else
				ImGui.TextUnformatted("N/A");
		}

		private void DrawPreviewData(IMachine machine, int addr, int mem_size, ImGuiDataType data_type, DataFormat data_format, ref string out_buf)
		{
			var buf = new byte[8];
			var elem_size = DataTypeGetSize(data_type);
			var size = addr + elem_size > mem_size ? mem_size - addr : elem_size;

			for (var i = 0; i < size; i++)
				buf[i] = machine.ReadMemory((uint)(addr + i));

			if (data_format == DataFormat.Bin)
			{
				var binbuf = new byte[8];
				EndiannessCopy(ref binbuf, buf, size);
				out_buf = FormatBinary(binbuf, size * 8);
				return;
			}

			var otherbuf = new byte[8];
			switch (data_type)
			{
				case ImGuiDataType.S8:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{(sbyte)otherbuf[0]}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}} ", (sbyte)otherbuf[0]);
					break;

				case ImGuiDataType.U8:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{otherbuf[0]}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}2}} ", otherbuf[0]);
					break;

				case ImGuiDataType.S16:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToInt16(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}4}} ", BitConverter.ToInt16(otherbuf));
					break;

				case ImGuiDataType.U16:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToUInt16(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}4}} ", BitConverter.ToUInt16(otherbuf));
					break;

				case ImGuiDataType.S32:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToInt32(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToInt32(otherbuf));
					break;

				case ImGuiDataType.U32:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToUInt32(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToUInt32(otherbuf));
					break;

				case ImGuiDataType.S64:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToInt64(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}16}} ", BitConverter.ToInt64(otherbuf));
					break;

				case ImGuiDataType.U64:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToUInt64(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToUInt64(otherbuf));
					break;

				case ImGuiDataType.Float:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToSingle(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}4}} ", BitConverter.ToUInt32(otherbuf));
					break;

				case ImGuiDataType.Double:
					EndiannessCopy(ref otherbuf, buf, size);
					if (data_format == DataFormat.Dec) out_buf = $"{BitConverter.ToDouble(otherbuf)}";
					else if (data_format == DataFormat.Hex) out_buf = string.Format($"{{0:{(optUpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToUInt64(otherbuf));
					break;
			}
		}

		void EndiannessCopyLittleEndian(ref byte[] dst, byte[] src, int size, bool is_little_endian)
		{
			if (is_little_endian)
				Array.Copy(src, dst, size);
			else
			{
				for (int i = 0, j = size - 1; i < size; i++, j--)
					dst[i] = src[j];
			}
		}

		void EndiannessCopyBigEndian(ref byte[] dst, byte[] src, int size, bool is_little_endian)
		{
			if (is_little_endian)
			{
				for (int i = 0, j = size - 1; i < size; i++, j--)
					dst[i] = src[j];
			}
			else
				Array.Copy(src, dst, size);
		}

		void EndiannessCopy(ref byte[] dst, byte[] src, int size)
		{
			if (BitConverter.IsLittleEndian)
				EndiannessCopyLittleEndian(ref dst, src, size, previewEndianness != 0);
			else
				EndiannessCopyBigEndian(ref dst, src, size, previewEndianness != 0);
		}

		string FormatBinary(byte[] buf, int width)
		{
			System.Diagnostics.Debug.Assert(width <= 64);
			int out_n = 0;
			char[] out_buf = new char[64 + 8 + 1];
			int n = width / 8;
			for (int j = n - 1; j >= 0; --j)
			{
				for (int i = 0; i < 8; ++i)
					out_buf[out_n++] = (buf[j] & (1 << (7 - i))) != 0 ? '1' : '0';
				out_buf[out_n++] = ' ';
			}
			System.Diagnostics.Debug.Assert(out_n < out_buf.Length);
			return new string(out_buf);
		}

		static int DataTypeGetSize(ImGuiDataType data_type)
		{
			int[] sizes = { 1, 1, 2, 2, 4, 4, 8, 8, sizeof(float), sizeof(double) };
			System.Diagnostics.Debug.Assert(data_type >= 0 && data_type < ImGuiDataType.COUNT);
			return sizes[(int)data_type];
		}

		static string DataTypeGetDesc(ImGuiDataType data_type)
		{
			string[] descs = { "Int8", "Uint8", "Int16", "Uint16", "Int32", "Uint32", "Int64", "Uint64", "Float", "Double" };
			System.Diagnostics.Debug.Assert(data_type >= 0 && data_type < ImGuiDataType.COUNT);
			return descs[(int)data_type];
		}

		public class UserData
		{
			public string CurrentBufOverwrite;
			public int CursorPos;
		}
	}
}

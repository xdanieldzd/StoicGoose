using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using ImGuiNET;

using StoicGoose.Emulation;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiMemoryWindow : ImGuiWindowBase
	{
		/* Ported/adapted from https://github.com/ocornut/imgui_club/blob/d4cd9896e15a03e92702a578586c3f91bbde01e8/imgui_memory_editor/imgui_memory_editor.h */

		/* Consts, dicts, enums... */
		const int memorySize = 0x100000;

		readonly static Dictionary<ImGuiDataType, int> dataTypeSizes = new()
		{
			{ ImGuiDataType.S8, 1 },
			{ ImGuiDataType.U8, 1 },
			{ ImGuiDataType.S16, 2 },
			{ ImGuiDataType.U16, 2 },
			{ ImGuiDataType.S32, 4 },
			{ ImGuiDataType.U32, 4 },
			{ ImGuiDataType.S64, 8 },
			{ ImGuiDataType.U64, 8 },
			{ ImGuiDataType.Float, sizeof(float) },
			{ ImGuiDataType.Double, sizeof(double) },
		};

		readonly static Dictionary<ImGuiDataType, string> dataTypeDescs = new()
		{
			{ ImGuiDataType.S8, "Int8" },
			{ ImGuiDataType.U8, "Uint8" },
			{ ImGuiDataType.S16, "Int16" },
			{ ImGuiDataType.U16, "Uint16" },
			{ ImGuiDataType.S32, "Int32" },
			{ ImGuiDataType.U32, "Uint32" },
			{ ImGuiDataType.S64, "Int64" },
			{ ImGuiDataType.U64, "Uint64" },
			{ ImGuiDataType.Float, "Float" },
			{ ImGuiDataType.Double, "Double" },
		};

		enum DataFormat { Dec, Hex, Bin }

		/* Public options */
		public bool IsReadOnly { get; set; } = false;
		public int NumColumns { get { return numColumns; } set { numColumns = value; } }
		public bool ShowOptions { get; set; } = true;
		public bool ShowDataPreview { get { return showDataPreview; } set { showDataPreview = value; } }
		public bool ShowAscii { get { return showAscii; } set { showAscii = value; } }
		public bool GrayOutZeroes { get { return grayOutZeroes; } set { grayOutZeroes = value; } }
		public bool UpperCaseHex { get { return upperCaseHex; } set { upperCaseHex = value; } }
		public int MidColsCount { get; set; } = 8;
		public int AddrDigitsCount { get; set; } = 0;
		public float FooterExtraHeight { get; set; } = 0f;
		public uint HighlightColor { get; set; } = 0;

		/* Backing fields for options etc */
		int numColumns = 16;
		bool showDataPreview = true, showAscii = true, grayOutZeroes = true, upperCaseHex = true;

		/* Internal stuff */
		bool contentsWidthChanged = false;
		int numItemsVisible = 0;
		int dataPreviewAddr = -1, dataEditingAddr = -1;
		bool dataEditingTakeFocus = false;
		string dataInputBuf = new('\0', 32);
		string addrInputBuf = new('\0', 32);
		int gotoAddr = -1;
		int previewEndianness = 0;
		ImGuiDataType previewDataType = ImGuiDataType.S32;

		/* Sizing variables */
		int addrDigitsCount = 0;
		float lineHeight = 0f, glyphWidth = 0f, hexCellWidth = 0f, spacingBetweenMidCols = 0f, windowWidth = 0f;
		float posHexStart = 0f, posHexEnd = 0f, posAsciiStart = 0f, posAsciiEnd = 0f;

		/* Functional stuffs */
		ImFontPtr japaneseFont = default;

		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;

		public ImGuiMemoryWindow() : base("Memory Editor")
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

		private void CalcSizes()
		{
			var style = ImGui.GetStyle();

			addrDigitsCount = AddrDigitsCount;
			if (addrDigitsCount == 0)
				for (var i = memorySize - 1; i > 0; i >>= 4)
					addrDigitsCount++;

			lineHeight = ImGui.GetTextLineHeight();
			glyphWidth = ImGui.CalcTextSize("F").X + 1f;
			hexCellWidth = (int)(glyphWidth * 2.5f);
			spacingBetweenMidCols = (int)(hexCellWidth * 0.25f);
			posHexStart = (addrDigitsCount + 1) * glyphWidth;
			posHexEnd = posHexStart + (hexCellWidth * NumColumns);
			posAsciiStart = posAsciiEnd = posHexEnd;

			if (ShowAscii)
			{
				posAsciiStart = posHexEnd + glyphWidth;
				if (MidColsCount > 0)
					posAsciiStart += ((NumColumns + MidColsCount - 1) / MidColsCount) * spacingBetweenMidCols;
				posAsciiEnd = posAsciiStart + NumColumns * glyphWidth;
			}

			windowWidth = posAsciiEnd + style.ScrollbarSize + style.WindowPadding.X * 2 + glyphWidth;
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not EmulatorHandler handler) return;

			if (HighlightColor == 0)
				HighlightColor = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.Text) & 0x00FFFFFF);

			if (japaneseFont.Equals(default(ImFontPtr)))
				japaneseFont = ImGui.GetIO().Fonts.Fonts[1];

			CalcSizes();

			ImGui.SetNextWindowSize(new NumericsVector2(windowWidth, windowWidth * 0.6f), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(NumericsVector2.Zero, new NumericsVector2(windowWidth, float.MaxValue));

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoScrollbar))
			{
				if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
					ImGui.OpenPopup("context");

				DrawContents(handler);

				if (contentsWidthChanged)
				{
					CalcSizes();
					ImGui.SetWindowSize(new NumericsVector2(windowWidth, ImGui.GetWindowSize().Y));
					contentsWidthChanged = false;
				}

				ImGui.End();
			}
		}

		private void DrawContents(EmulatorHandler handler)
		{
			if (NumColumns < 1) NumColumns = 1;

			var style = ImGui.GetStyle();

			var heightSeparator = style.ItemSpacing.Y;
			var footerHeight = FooterExtraHeight;
			if (ShowOptions)
				footerHeight += heightSeparator + ImGui.GetFrameHeightWithSpacing();
			if (ShowDataPreview)
				footerHeight += heightSeparator + ImGui.GetFrameHeightWithSpacing() + (ImGui.GetTextLineHeightWithSpacing() * 4.5f);

			ImGui.BeginChild("##scrolling", new NumericsVector2(0f, -footerHeight), false, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollWithMouse);
			{
				var drawList = ImGui.GetWindowDrawList();

				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
				ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

				var lineCountTotal = (memorySize + NumColumns - 1) / NumColumns;

				var clipper = new ImGuiListClipperPtr(clipperPointer);
				clipper.Begin(lineCountTotal, lineHeight);
				{
					var dataNext = false;

					if (IsReadOnly || dataEditingAddr >= memorySize)
						dataEditingAddr = -1;
					if (dataPreviewAddr >= memorySize)
						dataPreviewAddr = -1;

					var previewDataTypeSize = ShowDataPreview && dataTypeSizes.ContainsKey(previewDataType) ? dataTypeSizes[previewDataType] : 0;

					var dataEditingAddrNext = -1;
					if (dataEditingAddr != -1)
					{
						void setScroll() => ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + dataEditingAddrNext / NumColumns * lineHeight);

						if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && dataEditingAddr >= NumColumns) { dataEditingAddrNext = dataEditingAddr - NumColumns; }
						else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow) && dataEditingAddr < memorySize - NumColumns) { dataEditingAddrNext = dataEditingAddr + NumColumns; }
						else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow) && dataEditingAddr > 0) { dataEditingAddrNext = dataEditingAddr - 1; }
						else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow) && dataEditingAddr < memorySize - 1) { dataEditingAddrNext = dataEditingAddr + 1; }
						else if (ImGui.IsKeyPressed(ImGuiKey.PageUp) && numItemsVisible != 0)
						{
							if (dataEditingAddr >= NumColumns * numItemsVisible) dataEditingAddrNext = dataEditingAddr - NumColumns * numItemsVisible;
							else dataEditingAddrNext = 0;
							setScroll();
						}
						else if (ImGui.IsKeyPressed(ImGuiKey.PageDown) && numItemsVisible != 0)
						{
							if (dataEditingAddr < memorySize - NumColumns * numItemsVisible) dataEditingAddrNext = dataEditingAddr + NumColumns * numItemsVisible;
							else dataEditingAddrNext = memorySize - NumColumns;
							setScroll();
						}
						else if (ImGui.IsKeyPressed(ImGuiKey.Home))
						{
							dataEditingAddrNext = 0;
							setScroll();
						}
						else if (ImGui.IsKeyPressed(ImGuiKey.End))
						{
							dataEditingAddrNext = memorySize - NumColumns;
							setScroll();
						}
					}

					var windowPos = ImGui.GetWindowPos();
					drawList.AddLine(new NumericsVector2(windowPos.X + posHexStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posHexStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
					if (ShowAscii)
						drawList.AddLine(new NumericsVector2(windowPos.X + posAsciiStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posAsciiStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

					var colorText = ImGui.GetColorU32(ImGuiCol.Text);
					var colorDisabled = GrayOutZeroes ? ImGui.GetColorU32(ImGuiCol.TextDisabled) : colorText;

					while (clipper.Step())
					{
						numItemsVisible = clipper.DisplayEnd - clipper.DisplayStart - 1;

						for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							var addr = i * NumColumns;

							ImGui.Text(string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}{addrDigitsCount}}}", addr));

							for (var j = 0; j < NumColumns && addr < memorySize; j++, addr++)
							{
								var bytePosX = posHexStart + hexCellWidth * j;
								if (MidColsCount > 0)
									bytePosX += j / MidColsCount * spacingBetweenMidCols;

								ImGui.SameLine(bytePosX);

								if (dataPreviewAddr != -1 && addr >= dataPreviewAddr && addr < dataPreviewAddr + previewDataTypeSize)
								{
									var pos = ImGui.GetCursorScreenPos();
									var highlightWidth = glyphWidth * 2;
									if (addr < dataPreviewAddr + previewDataTypeSize - 1 && ((addr + 1 < memorySize) || (j + 1 == NumColumns)))
									{
										highlightWidth = hexCellWidth;
										if (MidColsCount > 0 && j > 0 && (j + 1) < NumColumns && ((j + 1) % MidColsCount) == 0)
											highlightWidth += spacingBetweenMidCols;
									}

									drawList.AddRectFilled(pos, new NumericsVector2(pos.X + highlightWidth, pos.Y + lineHeight), HighlightColor);
								}

								if (dataEditingAddr == addr)
								{
									unsafe
									{
										var b = handler.IsRunning ? handler.Machine.ReadMemory((uint)addr) : 0;

										var dataWrite = false;
										ImGui.PushID(addr);
										if (dataEditingTakeFocus)
										{
											ImGui.SetKeyboardFocusHere(0);
											addrInputBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}{addrDigitsCount}}}", addr);
											dataInputBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}2}}", b);
										}

										var userData = new UserData()
										{
											CursorPos = -1,
											CurrentBufOverwrite = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}2}}", b)
										};

										int callback(ImGuiInputTextCallbackData* data)
										{
											var dataPtr = new ImGuiInputTextCallbackDataPtr(data);

											if (!dataPtr.HasSelection())
												userData.CursorPos = data->CursorPos;

											if (dataPtr.SelectionStart == 0 && dataPtr.SelectionEnd == dataPtr.BufTextLen)
											{
												dataPtr.DeleteChars(0, dataPtr.BufTextLen);
												dataPtr.InsertChars(0, userData.CurrentBufOverwrite);
												dataPtr.SelectionStart = 0;
												dataPtr.SelectionEnd = 2;
												dataPtr.CursorPos = 0;
											}

											return 0;
										}

										var flags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.AlwaysOverwrite;

										ImGui.SetNextItemWidth(glyphWidth * 2f);
										if (ImGui.InputText("##data", ref dataInputBuf, 2, flags, callback))
											dataWrite = dataNext = true;
										else if (!dataEditingTakeFocus && !ImGui.IsItemActive())
											dataEditingAddr = dataEditingAddrNext = -1;

										dataEditingTakeFocus = false;
										if (userData.CursorPos >= 2)
											dataWrite = dataNext = true;
										if (dataEditingAddrNext != -1)
											dataWrite = dataNext = false;

										if (dataWrite)
											handler.Machine.WriteMemory((uint)addr, byte.Parse(dataInputBuf, System.Globalization.NumberStyles.HexNumber));

										ImGui.PopID();
									}
								}
								else
								{
									var b = handler.IsRunning ? handler.Machine.ReadMemory((uint)addr) : 0;

									if (b == 0 && GrayOutZeroes)
										ImGui.TextDisabled("00 ");
									else
										ImGui.Text(string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}2}} ", b));

									if (!IsReadOnly && ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
									{
										dataEditingTakeFocus = true;
										dataEditingAddrNext = addr;
									}
								}
							}

							if (ShowAscii)
							{
								ImGui.SameLine(posAsciiStart);

								var pos = ImGui.GetCursorScreenPos();
								addr = i * NumColumns;

								ImGui.PushID(i);
								if (ImGui.InvisibleButton("ascii", new NumericsVector2(posAsciiEnd - posAsciiStart, lineHeight)))
								{
									dataEditingAddr = dataPreviewAddr = addr + (int)((ImGui.GetIO().MousePos.X - pos.X) / glyphWidth);
									dataEditingTakeFocus = true;
								}
								ImGui.PopID();

								for (var j = 0; j < NumColumns && addr < memorySize; j++, addr++)
								{
									if (dataPreviewAddr != -1 && addr >= dataPreviewAddr && addr < dataPreviewAddr + previewDataTypeSize)
										drawList.AddRectFilled(pos, new NumericsVector2(pos.X + glyphWidth, pos.Y + lineHeight), HighlightColor);

									if (addr == dataEditingAddr)
									{
										drawList.AddRectFilled(pos, new NumericsVector2(pos.X + glyphWidth, pos.Y + lineHeight), ImGui.GetColorU32(ImGuiCol.FrameBg));
										drawList.AddRectFilled(pos, new NumericsVector2(pos.X + glyphWidth, pos.Y + lineHeight), ImGui.GetColorU32(ImGuiCol.TextSelectedBg));
									}

									var ch = handler.IsRunning ? (char)handler.Machine.ReadMemory((uint)addr) : '\0';
									var displayCh = (char.IsControl(ch) || char.IsWhiteSpace(ch)) ? '.' : ch;

									drawList.AddText(pos, (displayCh == ch) ? colorText : colorDisabled, new string(new char[] { displayCh }));
									pos.X += glyphWidth;
								}
							}
						}
					}
					clipper.End();

					ImGui.PopStyleVar(2);
					ImGui.EndChild();

					ImGui.SetCursorPosX(windowWidth);

					if (dataNext && dataEditingAddr + 1 < memorySize)
					{
						dataEditingAddr = dataPreviewAddr = dataEditingAddr + 1;
						dataEditingTakeFocus = true;
					}
					else if (dataEditingAddrNext != -1)
					{
						dataEditingAddr = dataPreviewAddr = dataEditingAddrNext;
						dataEditingTakeFocus = true;
					}

					if (!handler.IsRunning) ImGui.BeginDisabled();

					if (ShowOptions)
					{
						ImGui.Separator();
						DrawOptionsLine();
					}

					if (ShowDataPreview)
					{
						ImGui.Separator();
						DrawPreviewLine(handler, colorText, colorDisabled);
					}

					if (!handler.IsRunning) ImGui.EndDisabled();
				}
			}
		}

		private void DrawOptionsLine()
		{
			var style = ImGui.GetStyle();

			if (ImGui.Button("Options"))
				ImGui.OpenPopup("context");

			if (ImGui.BeginPopup("context"))
			{
				ImGui.SetNextItemWidth(glyphWidth * 17f + style.FramePadding.X * 2f);
				if (ImGui.SliderInt("##cols", ref numColumns, 4, 32, "%d columns")) { contentsWidthChanged = true; if (NumColumns < 1) NumColumns = 1; }
				ImGui.Checkbox("Show Data Preview", ref showDataPreview);
				if (ImGui.Checkbox("Show ASCII", ref showAscii)) contentsWidthChanged = true;
				ImGui.Checkbox("Gray out Zeroes", ref grayOutZeroes);
				ImGui.Checkbox("Uppercase Hex", ref upperCaseHex);

				ImGui.EndPopup();
			}

			ImGui.SameLine();
			ImGui.Text(string.Format($"Range {{0:{(UpperCaseHex ? "X" : "x")}{addrDigitsCount}}}..{{1:{(UpperCaseHex ? "X" : "x")}{addrDigitsCount}}}", 0, memorySize - 1));
			ImGui.SameLine();

			var flags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue;
			if (upperCaseHex) flags |= ImGuiInputTextFlags.CharsUppercase;

			ImGui.SetNextItemWidth((addrDigitsCount + 1) * glyphWidth + style.FramePadding.X * 2f);
			if (ImGui.InputText("##addr", ref addrInputBuf, (uint)addrDigitsCount, flags))
			{
				gotoAddr = int.Parse(addrInputBuf, System.Globalization.NumberStyles.HexNumber);
			}

			if (gotoAddr != -1)
			{
				if (gotoAddr < memorySize)
				{
					ImGui.BeginChild("##scrolling");
					ImGui.SetScrollFromPosY(ImGui.GetCursorStartPos().Y + (gotoAddr / NumColumns) * lineHeight);
					ImGui.EndChild();

					dataEditingAddr = dataPreviewAddr = gotoAddr;
					dataEditingTakeFocus = false;
				}
				gotoAddr = -1;
			}
		}

		private void DrawPreviewLine(EmulatorHandler handler, uint colorText, uint colorDisabled)
		{
			var style = ImGui.GetStyle();

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Preview as:");
			ImGui.SameLine();

			ImGui.SetNextItemWidth((glyphWidth * 10f) + style.FramePadding.X * 2f + style.ItemInnerSpacing.X);
			if (ImGui.BeginCombo("##combo_type", dataTypeDescs.ContainsKey(previewDataType) ? dataTypeDescs[previewDataType] : "N/A", ImGuiComboFlags.HeightLargest))
			{
				foreach (ImGuiDataType type in Enum.GetValues(typeof(ImGuiDataType)))
					if (dataTypeDescs.ContainsKey(type) && ImGui.Selectable(dataTypeDescs[type], previewDataType == type))
						previewDataType = type;
				ImGui.EndCombo();
			}
			ImGui.SameLine();
			ImGui.SetNextItemWidth((glyphWidth * 6f) + style.FramePadding.X * 2f + style.ItemInnerSpacing.X);
			ImGui.Combo("##combo_endianness", ref previewEndianness, new string[] { "LE", "BE" }, 2);

			var buf = string.Empty;
			var dist = glyphWidth * 5f;
			var hasValue = dataPreviewAddr != -1;

			var drawList = ImGui.GetWindowDrawList();
			var pos = ImGui.GetCursorScreenPos();

			foreach (DataFormat format in Enum.GetValues(typeof(DataFormat)))
			{
				drawList.AddText(pos, colorText, $"{format}");

				if (hasValue && dataTypeSizes.ContainsKey(previewDataType))
					DrawPreviewData(handler, dataPreviewAddr, previewDataType, format, ref buf);
				else
					buf = "N/A";

				if (hasValue && format == DataFormat.Bin)
				{
					for (var i = 0; i < buf.Length; i++)
						drawList.AddText(new NumericsVector2(pos.X + dist + (i * (glyphWidth - 1f)), pos.Y),
							grayOutZeroes && buf[i] == '0' ? colorDisabled : colorText, new string(new char[] { buf[i] }));
				}
				else
					drawList.AddText(new NumericsVector2(pos.X + dist, pos.Y), colorText, buf);

				pos.Y += ImGui.GetTextLineHeightWithSpacing();
			}

			drawList.AddText(pos, colorText, "SJIS");
			if (hasValue)
			{
				DrawPreviewSjis(handler, dataPreviewAddr, ref buf);
				ImGui.PushFont(japaneseFont);
				drawList.AddText(new NumericsVector2(pos.X + dist, pos.Y), colorText, buf);
				ImGui.PopFont();
			}
			else
				drawList.AddText(new NumericsVector2(pos.X + dist, pos.Y), colorText, "N/A");
		}

		private void DrawPreviewData(EmulatorHandler handler, int addr, ImGuiDataType dataType, DataFormat dataFormat, ref string outBuf)
		{
			if (!dataTypeSizes.ContainsKey(dataType)) return;

			var elemSize = dataTypeSizes[dataType];
			var size = addr + elemSize > memorySize ? memorySize - addr : elemSize;

			var buf = new byte[elemSize];
			for (var i = 0; i < size; i++)
				buf[i] = handler.IsRunning ? handler.Machine.ReadMemory((uint)(addr + i)) : (byte)0;

			if (dataFormat == DataFormat.Bin)
			{
				var binBuf = new byte[elemSize];
				EndiannessCopy(ref binBuf, buf, elemSize);
				outBuf = FormatBinary(binBuf, size * 8);
				return;
			}

			var otherBuf = new byte[elemSize];
			EndiannessCopy(ref otherBuf, buf, elemSize);

			switch (dataType)
			{
				case ImGuiDataType.S8:
					if (dataFormat == DataFormat.Dec) outBuf = $"{(sbyte)otherBuf[0]}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}2}} ", (sbyte)otherBuf[0]);
					break;

				case ImGuiDataType.U8:
					if (dataFormat == DataFormat.Dec) outBuf = $"{otherBuf[0]}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}2}} ", otherBuf[0]);
					break;

				case ImGuiDataType.S16:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToInt16(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}4}} ", BitConverter.ToInt16(otherBuf));
					break;

				case ImGuiDataType.U16:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToUInt16(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}4}} ", BitConverter.ToUInt16(otherBuf));
					break;

				case ImGuiDataType.S32:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToInt32(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToInt32(otherBuf));
					break;

				case ImGuiDataType.U32:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToUInt32(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToUInt32(otherBuf));
					break;

				case ImGuiDataType.S64:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToInt64(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}16}} ", BitConverter.ToInt64(otherBuf));
					break;

				case ImGuiDataType.U64:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToUInt64(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}16}} ", BitConverter.ToUInt64(otherBuf));
					break;

				case ImGuiDataType.Float:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToSingle(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}8}} ", BitConverter.ToUInt32(otherBuf));
					break;

				case ImGuiDataType.Double:
					if (dataFormat == DataFormat.Dec) outBuf = $"{BitConverter.ToDouble(otherBuf)}";
					else if (dataFormat == DataFormat.Hex) outBuf = string.Format($"{{0:{(UpperCaseHex ? "X" : "x")}16}} ", BitConverter.ToUInt64(otherBuf));
					break;
			}
		}

		private void DrawPreviewSjis(EmulatorHandler handler, int addr, ref string outBuf)
		{
			var buf = new byte[256];
			var elemSize = dataTypeSizes[ImGuiDataType.U16];
			var size = addr + (buf.Length / elemSize) > memorySize ? memorySize - addr : (buf.Length / elemSize);

			for (var i = 0; i < size; i++)
				buf[i] = handler.IsRunning ? handler.Machine.ReadMemory((uint)(addr + i)) : (byte)0;
			outBuf = Encoding.GetEncoding(932).GetString(buf);
		}

		private void EndiannessCopyLittleEndian(ref byte[] dst, byte[] src, int elemSize, bool isLittleEndian)
		{
			if (isLittleEndian)
				Array.Copy(src, dst, src.Length);
			else
			{
				for (int i = 0; i < dst.Length; i += elemSize)
					for (var j = 0; j < elemSize; j++)
						dst[i + j] = src[(i + (elemSize - 1)) - j];
			}
		}

		private void EndiannessCopyBigEndian(ref byte[] dst, byte[] src, int elemSize, bool isLittleEndian)
		{
			if (isLittleEndian)
			{
				for (int i = 0, j = dst.Length - 1; i < dst.Length; i += elemSize, j -= elemSize)
					for (var k = 0; k < elemSize; k++)
						dst[i + k] = src[j - k];
			}
			else
				Array.Copy(src, dst, src.Length);
		}

		private void EndiannessCopy(ref byte[] dst, byte[] src, int elemSize)
		{
			if (dst.Length != src.Length) throw new ArgumentException("Array size mismatch");

			if (BitConverter.IsLittleEndian)
				EndiannessCopyLittleEndian(ref dst, src, elemSize, previewEndianness != 0);
			else
				EndiannessCopyBigEndian(ref dst, src, elemSize, previewEndianness != 0);
		}

		private string FormatBinary(byte[] buf, int width)
		{
			var outBuf = new char[width + (width / buf.Length)];
			for (int i = buf.Length - 1, j = 0; i >= 0; i--, j += 9)
			{
				for (var k = 0; k < 8; k++)
					outBuf[j + k] = (buf[i] & (1 << (7 - k))) != 0 ? '1' : '0';
				outBuf[j + 8] = ' ';

			}
			return new string(outBuf);
		}

		public class UserData
		{
			public string CurrentBufOverwrite;
			public int CursorPos;
		}
	}
}

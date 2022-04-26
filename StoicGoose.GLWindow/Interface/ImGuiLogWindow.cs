using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using ImGuiNET;

using StoicGoose.Common.Extensions;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class ImGuiLogWindow : ImGuiWindowBase
	{
		/* https://github.com/ocornut/imgui/blob/3587ee492bbee8144dbfbf5f660d4a7fd6316638/imgui_demo.cpp#L6819
		 * https://github.com/BalazsJako/ImGuiColorTextEdit/blob/0a88824f7de8d0bd11d8419066caa7d3469395c4/TextEditor.cpp#L854
		 */

		readonly static uint defaultTextColor = GetColor(255, 255, 255);
		readonly static uint filterHighlightTextColor = GetColor(13, 188, 121);

		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;
		readonly ImGuiListClipperPtr clipper = default;

		readonly LogWriter logWriter = default;

		readonly List<string> messageList = new();
		string currentLine = string.Empty;
		string filterString = string.Empty;

		bool autoScroll = true;

		public TextWriter TextWriter => logWriter;

		public ImGuiLogWindow() : base("Log", new NumericsVector2(450f, 500f), ImGuiCond.FirstUseEver)
		{
			clipperObject = new ImGuiListClipper();
			clipperHandle = GCHandle.Alloc(clipperObject, GCHandleType.Pinned);
			clipperPointer = clipperHandle.AddrOfPinnedObject();
			clipper = new ImGuiListClipperPtr(clipperPointer);

			logWriter = new(this);
		}

		~ImGuiLogWindow()
		{
			clipperHandle.Free();
		}

		private void Write(char value)
		{
			currentLine += value;
			if (value == '\n')
			{
				messageList.Add(currentLine);
				currentLine = string.Empty;
			}
		}

		protected override void DrawWindow(object userData)
		{
			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				var fontSize = ImGui.CalcTextSize("#").X;

				if (ImGui.BeginPopup("Options"))
				{
					ImGui.Checkbox("Auto-scroll", ref autoScroll);
					ImGui.EndPopup();
				}

				if (ImGui.Button("Options"))
					ImGui.OpenPopup("Options");
				ImGui.SameLine();
				var clear = ImGui.Button("Clear");
				ImGui.SameLine();
				var copy = ImGui.Button("Copy");
				ImGui.SameLine();
				ImGui.SetNextItemWidth(-fontSize * 6f);
				ImGui.InputText("Filter", ref filterString, 256);

				ImGui.Separator();

				ImGui.BeginChild("scrolling", NumericsVector2.Zero, false, ImGuiWindowFlags.HorizontalScrollbar);
				{
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

					var mCharAdvance = new NumericsVector2(fontSize, ImGui.GetTextLineHeightWithSpacing());

					var cursorScreenPos = ImGui.GetCursorScreenPos();
					var drawList = ImGui.GetWindowDrawList();

					var lineBuffer = new StringBuilder();
					var prevColor = defaultTextColor;

					if (!string.IsNullOrEmpty(filterString))
					{
						var validLines = messageList.Where(x => x.Contains(filterString)).ToArray();

						clipper.Begin(validLines.Length);
						while (clipper.Step())
						{
							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var line = validLines[i];

								// TODO: proper final byte detection?
								int escStartIdx;
								while ((escStartIdx = line.IndexOf('\x1B')) != -1)
								{
									var escEndIdx = line.IndexOf('m', escStartIdx);
									if (escEndIdx != -1) line = line.Remove(escStartIdx, escEndIdx - escStartIdx + 1);
								}

								var matches = line.IndexOfAll(filterString).Select(x => (start: x, end: x + filterString.Length)).ToArray();
								var currentMatch = 0;

								var textScreenPos = new NumericsVector2(cursorScreenPos.X, cursorScreenPos.Y + i * mCharAdvance.Y);
								var bufferOffset = NumericsVector2.Zero;

								for (var j = 0; j < line.Length; j++)
								{
									var ch = line[j];

									var insideMatch = currentMatch >= 0 && currentMatch < matches.Length &&
										j >= matches[currentMatch].start && j < matches[currentMatch].end;

									var color = insideMatch ? filterHighlightTextColor : defaultTextColor;

									if ((color != prevColor || ch == '\t') && lineBuffer.Length != 0)
									{
										var substring = lineBuffer.ToString();

										ImGui.SetCursorScreenPos(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y));
										ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(prevColor), substring);

										bufferOffset.X += ImGui.CalcTextSize(substring).X;

										lineBuffer.Clear();
									}
									prevColor = color;

									if (ch == '\t')
										textScreenPos.X += 1f + (float)Math.Floor((1f + bufferOffset.X) / (4f * fontSize)) * (4f * fontSize);
									else
										lineBuffer.Append(ch);

									if (lineBuffer.ToString() == filterString)
										currentMatch++;
								}

								if (lineBuffer.Length != 0)
								{
									ImGui.SetCursorScreenPos(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y));
									ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(prevColor), lineBuffer.ToString());

									lineBuffer.Clear();
								}
							}
						}
						clipper.End();
					}
					else
					{
						clipper.Begin(messageList.Count);
						while (clipper.Step())
						{
							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var line = messageList[i];

								var textScreenPos = new NumericsVector2(cursorScreenPos.X, cursorScreenPos.Y + i * mCharAdvance.Y);
								var bufferOffset = NumericsVector2.Zero;

								var color = defaultTextColor;

								for (var j = 0; j < messageList[i].Length; j++)
								{
									if (j + 1 < messageList[i].Length && line[j] == '\x1B' && line[j + 1] == '[')
									{
										// TODO: proper final byte detection?
										var escEndIdx = line.IndexOf('m', j);
										if (escEndIdx != -1)
										{
											(color, _) = ParseEscSequence(line[j..(escEndIdx + 1)]);
											j = escEndIdx;
										}
									}
									else
									{
										var ch = line[j];

										if ((color != prevColor || ch == '\t') && lineBuffer.Length != 0)
										{
											var substring = lineBuffer.ToString();

											ImGui.SetCursorScreenPos(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y));
											ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(prevColor), substring);

											bufferOffset.X += ImGui.CalcTextSize(substring).X;

											lineBuffer.Clear();
										}
										prevColor = color;

										if (ch == '\t')
											textScreenPos.X += 1f + (float)Math.Floor((1f + bufferOffset.X) / (4f * fontSize)) * (4f * fontSize);
										else
											lineBuffer.Append(ch);
									}
								}

								if (lineBuffer.Length != 0)
								{
									ImGui.SetCursorScreenPos(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y));
									ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(prevColor), lineBuffer.ToString());

									lineBuffer.Clear();
								}
							}
						}
						clipper.End();
					}

					ImGui.PopStyleVar();

					if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
						ImGui.SetScrollHereY(1f);

					ImGui.EndChild();
				}

				if (clear) messageList.Clear();
				if (copy) ImGui.LogToClipboard();

				ImGui.End();
			}
		}

		private (uint fgColor, uint _) ParseEscSequence(string esc)
		{
			var fgColor = uint.MaxValue;

			var values = esc[2..^1].Split(';').Select(x => byte.Parse(x)).ToArray();
			for (var i = 0; i < values.Length; i++)
			{
				switch (values[i])
				{
					case 0: fgColor = defaultTextColor; break; // reset all

					case 30: fgColor = GetColor(0, 0, 0); break; // black
					case 31: fgColor = GetColor(205, 49, 49); break; // red
					case 32: fgColor = GetColor(13, 188, 121); break; // green
					case 33: fgColor = GetColor(229, 229, 16); break; // yellow
					case 34: fgColor = GetColor(36, 114, 200); break; // blue
					case 35: fgColor = GetColor(188, 63, 188); break; // magenta
					case 36: fgColor = GetColor(17, 168, 205); break; // cyan
					case 37: fgColor = GetColor(255, 255, 255); break; // white

					case 38:
						{
							/* https://en.wikipedia.org/wiki/ANSI_escape_code#24-bit */
							fgColor = GetColor(values[2], values[3], values[4]);
							i += 4;
						}
						break;
				}
			}

			return (fgColor, 0);
		}

		private static uint GetColor(byte r, byte g, byte b) => 0xFF000000 | ((uint)b << 16) | ((uint)g << 8) | ((uint)r << 0);

		class LogWriter : TextWriter
		{
			readonly ImGuiLogWindow logWindow;

			public LogWriter(ImGuiLogWindow log) : base() => logWindow = log;

			public override Encoding Encoding => Encoding.Unicode;
			public override void Write(char value) => logWindow.Write(value);
		}
	}
}

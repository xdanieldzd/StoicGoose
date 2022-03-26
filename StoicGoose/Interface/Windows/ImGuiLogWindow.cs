using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ImGuiNET;

using StoicGoose.Extensions;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiLogWindow : ImGuiWindowBase
	{
		/* https://github.com/ocornut/imgui/blob/3587ee492bbee8144dbfbf5f660d4a7fd6316638/imgui_demo.cpp#L6819
		 * https://github.com/BalazsJako/ImGuiColorTextEdit/blob/0a88824f7de8d0bd11d8419066caa7d3469395c4/TextEditor.cpp#L854
		 */

		readonly LogWriter logWriter = default;

		readonly List<string> messageList = new();
		string currentLine = string.Empty;
		string filterString = string.Empty;

		bool autoScroll = true;

		public TextWriter TextWriter => logWriter;

		public ImGuiLogWindow() : base("Log", new NumericsVector2(450f, 500f), ImGuiCond.FirstUseEver) => logWriter = new(this);

		private void Write(char value)
		{
			currentLine += value;
			if (value == '\n')
			{
				messageList.Add(currentLine);
				currentLine = string.Empty;
			}
		}

		protected override void DrawWindow(params object[] args)
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

					if (!string.IsNullOrEmpty(filterString))
					{
						var validLines = messageList.Where(x => x.Contains(filterString)).ToArray();

						var cursorScreenPos = ImGui.GetCursorScreenPos();
						var drawList = ImGui.GetWindowDrawList();

						var lineBuffer = new StringBuilder();
						var prevColor = uint.MaxValue;

						for (var i = 0; i < validLines.Length; i++)
						{
							var matches = validLines[i].IndexOfAll(filterString).Select(x => (start: x, end: x + filterString.Length)).ToArray();
							var currentMatch = 0;

							var textScreenPos = new NumericsVector2(cursorScreenPos.X, cursorScreenPos.Y + i * mCharAdvance.Y);
							var bufferOffset = NumericsVector2.Zero;

							for (var j = 0; j < validLines[i].Length; j++)
							{
								var ch = validLines[i][j];

								var insideMatch = currentMatch >= 0 && currentMatch < matches.Length &&
									j >= matches[currentMatch].start && j < matches[currentMatch].end;

								var color = insideMatch ? 0xFF80FF80 : uint.MaxValue;

								if ((color != prevColor || ch == '\t') && lineBuffer.Length != 0)
								{
									var substring = lineBuffer.ToString();
									drawList.AddText(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y), prevColor, substring);
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
								drawList.AddText(new NumericsVector2(textScreenPos.X + bufferOffset.X, textScreenPos.Y + bufferOffset.Y), prevColor, lineBuffer.ToString());
								lineBuffer.Clear();
							}
						}
					}
					else
					{
						foreach (var line in messageList)
							ImGui.TextUnformatted(line);
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

		class LogWriter : TextWriter
		{
			readonly ImGuiLogWindow logWindow;

			public LogWriter(ImGuiLogWindow log) : base() => logWindow = log;

			public override Encoding Encoding => Encoding.Unicode;
			public override void Write(char value) => logWindow.Write(value);
		}
	}
}

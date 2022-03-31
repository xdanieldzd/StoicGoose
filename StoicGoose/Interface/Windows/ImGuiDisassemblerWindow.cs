using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using ImGuiNET;

using StoicGoose.Disassembly;
using StoicGoose.Emulation;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiDisassemblerWindow : ImGuiWindowBase
	{
		/* Consts, dicts, enums... */
		const int segmentSize = 0x10000;
		const int maxOpcodeBytes = 8;

		/* Public options */
		public bool UpperCaseHex { get { return upperCaseHex; } set { upperCaseHex = value; } }
		public bool TraceExecution { get { return traceExecution; } set { traceExecution = value; } }
		public uint HighlightColor1 { get; set; } = 0;
		public uint HighlightColor2 { get; set; } = 0;

		/* Backing fields for options etc */
		bool traceExecution = true, upperCaseHex = true;

		/* Internal stuff */
		ushort codeSegment = 0xFE00;
		readonly List<ushort> instructionAddresses = new();
		bool jumpToIpNext = false;
		string addrInputBuf = new('\0', 32);
		int gotoAddr = -1;

		/* Sizing variables */
		float lineHeight = 0f, glyphWidth = 0f, hexCellWidth = 0f;
		float posAddrStart = 0f, posAddrEnd = 0f, posHexStart = 0f, posHexEnd = 0f, posMnemonicStart = 0f;

		/* Functional stuffs */
		readonly Disassembler disassembler = new();

		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;

		public ImGuiDisassemblerWindow() : base("Disassembler", new NumericsVector2(650, 605f), ImGuiCond.Always)
		{
			clipperObject = new();
			clipperHandle = GCHandle.Alloc(clipperObject, GCHandleType.Pinned);
			clipperPointer = clipperHandle.AddrOfPinnedObject();
		}

		~ImGuiDisassemblerWindow()
		{
			clipperHandle.Free();
		}

		private void CalcSizes()
		{
			lineHeight = ImGui.GetTextLineHeight();
			glyphWidth = ImGui.CalcTextSize("F").X + 1f;
			hexCellWidth = (int)(glyphWidth * 2.5f);
			posAddrStart = glyphWidth * 3f;
			posAddrEnd = posAddrStart + (glyphWidth * 9f);
			posHexStart = posAddrEnd + glyphWidth;
			posHexEnd = posHexStart + (hexCellWidth * maxOpcodeBytes);
			posMnemonicStart = posHexEnd + glyphWidth;
		}

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not EmulatorHandler handler) return;

			if (HighlightColor1 == 0)
				HighlightColor1 = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
			if (HighlightColor2 == 0)
				HighlightColor2 = 0x1F000000 | (ImGui.GetColorU32(ImGuiCol.Text) & 0x00FFFFFF);

			CalcSizes();

			if (disassembler.ReadDelegate == null) disassembler.ReadDelegate = handler.Machine.ReadMemory;
			if (disassembler.WriteDelegate == null) disassembler.WriteDelegate = handler.Machine.WriteMemory;

			if (instructionAddresses.Count == 0 || codeSegment != handler.Machine.Cpu.Registers.CS)
			{
				instructionAddresses.Clear();
				codeSegment = handler.Machine.Cpu.Registers.CS;

				for (var i = 0; i < segmentSize;)
				{
					instructionAddresses.Add((ushort)i);
					i += disassembler.EvaluateInstructionLength(codeSegment, (ushort)i);
				}
			}

			var style = ImGui.GetStyle();
			var footerHeight = style.ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + (ImGui.GetTextLineHeightWithSpacing() * 9f);

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoResize))
			{
				if (ImGui.BeginChild("##disassembly", new NumericsVector2(0f, -footerHeight), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var drawListDisasm = ImGui.GetWindowDrawList();

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin(instructionAddresses.Count, lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posAddrStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posAddrStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posHexStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posHexStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posMnemonicStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posMnemonicStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

						static void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * ImGui.GetTextLineHeight()) - ImGui.GetScrollY(), 0.5f);

						if (traceExecution || jumpToIpNext)
						{
							var idx = instructionAddresses.IndexOf(handler.Machine.Cpu.Registers.IP);
							if (idx != -1)
							{
								centerScrollTo(idx);
								addrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", instructionAddresses[idx]);
							}
						}

						if (gotoAddr != -1)
						{
							var idx = instructionAddresses.BinarySearch((ushort)gotoAddr);
							if (idx < 0) idx = ~idx;
							addrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", instructionAddresses[idx]);
							centerScrollTo(idx);
						}

						jumpToIpNext = false;
						gotoAddr = -1;

						while (clipper.Step())
						{
							if (!traceExecution)
							{
								if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
									ImGui.SetScrollY(ImGui.GetScrollY() - clipper.ItemsHeight);

								else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
									ImGui.SetScrollY(ImGui.GetScrollY() + clipper.ItemsHeight);

								else if (ImGui.IsKeyPressed(ImGuiKey.PageUp, true))
									ImGui.SetScrollY(ImGui.GetScrollY() - clipper.ItemsHeight * (clipper.DisplayEnd - clipper.DisplayStart));
								else if (ImGui.IsKeyPressed(ImGuiKey.PageDown, true))
									ImGui.SetScrollY(ImGui.GetScrollY() + clipper.ItemsHeight * (clipper.DisplayEnd - clipper.DisplayStart));

								else if (ImGui.IsKeyPressed(ImGuiKey.Home) && !ImGui.IsAnyItemActive())
									ImGui.SetScrollY(0f);
								else if (ImGui.IsKeyPressed(ImGuiKey.End) && !ImGui.IsAnyItemActive())
									ImGui.SetScrollY(instructionAddresses.Count * clipper.ItemsHeight);
							}

							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var pos = ImGui.GetCursorScreenPos();

								if (handler.Machine.Cpu.Registers.IP == instructionAddresses[i])
									drawListDisasm.AddRectFilled(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor1);

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawListDisasm.AddRect(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor2);

								var (_, _, bytes, disasm, comment) = disassembler.DisassembleInstruction(codeSegment, instructionAddresses[i]);

								ImGui.Dummy(NumericsVector2.One);
								ImGui.SameLine(posAddrStart);

								ImGui.TextUnformatted($"{codeSegment:X4}:{instructionAddresses[i]:X4}");
								ImGui.SameLine(posHexStart);

								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{string.Join(" ", bytes.Select(x => ($"{x:X2}")))}");
								ImGui.SameLine(posMnemonicStart);

								ImGui.TextUnformatted(disasm);
								ImGui.SameLine();
								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{(!string.IsNullOrEmpty(comment) ? $" ; {comment}" : string.Empty)}");
							}
						}
						clipper.End();
					}
					ImGui.PopStyleVar(2);
					ImGui.EndChild();
				}

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				if (ImGui.BeginChild("##controls"))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new NumericsVector2(12f, 12f));

					var contentAvailWidth = ImGui.GetContentRegionAvail().X;
					var buttonWidth = (ImGui.GetContentRegionAvail().X - (style.ItemSpacing.X * 3f)) / 4f;

					if (!handler.IsPaused)
					{
						if (ImGui.Button("Pause", new NumericsVector2(buttonWidth, 0f))) handler.Pause();
						ImGui.SameLine();
						ImGui.BeginDisabled();
						ImGui.Button("Unpause", new NumericsVector2(buttonWidth, 0f));
						ImGui.EndDisabled();
					}
					else
					{
						ImGui.BeginDisabled();
						ImGui.Button("Pause", new NumericsVector2(buttonWidth, 0f));
						ImGui.EndDisabled();
						ImGui.SameLine();
						if (ImGui.Button("Unpause", new NumericsVector2(buttonWidth, 0f))) handler.Unpause();
					}
					ImGui.SameLine();
					if (handler.IsPaused)
					{
						ImGui.PushButtonRepeat(true);
						if (ImGui.Button("Step Instruction", new NumericsVector2(buttonWidth, 0f))) handler.Machine.RunStep();
						ImGui.SameLine();
						if (ImGui.Button("Step Frame", new NumericsVector2(buttonWidth, 0f))) handler.Machine.RunFrame();
						ImGui.PopButtonRepeat();
					}
					else
					{
						ImGui.BeginDisabled();
						ImGui.Button("Step Instruction", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Button("Step Frame", new NumericsVector2(buttonWidth, 0f));
						ImGui.EndDisabled();
					}

					var gotoInputFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue | (upperCaseHex ? ImGuiInputTextFlags.CharsUppercase : ImGuiInputTextFlags.None);

					if (ImGui.Button("Reset", new NumericsVector2(buttonWidth, 0f))) handler.Reset();
					ImGui.SameLine();
					ImGui.Checkbox("Trace execution", ref traceExecution);
					ImGui.SameLine();
					ImGui.SetCursorPosX(contentAvailWidth - (buttonWidth * 2f) - style.ItemSpacing.X);
					if (traceExecution)
					{
						ImGui.BeginDisabled();
						ImGui.Button("Jump to IP", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Text("Jump to:");
						ImGui.SameLine();
						ImGui.InputText("##addr", ref addrInputBuf, 4, gotoInputFlags);
						ImGui.EndDisabled();
					}
					else
					{
						if (ImGui.Button("Jump to IP", new NumericsVector2(buttonWidth, 0f))) jumpToIpNext = true;
						ImGui.SameLine();
						ImGui.Text("Jump to:");
						ImGui.SameLine();
						if (ImGui.InputText("##addr", ref addrInputBuf, 4, gotoInputFlags))
							gotoAddr = int.Parse(addrInputBuf, System.Globalization.NumberStyles.HexNumber);
					}
					ImGui.PopStyleVar();
					ImGui.EndChild();
				}

				ImGui.End();
			}
		}
	}
}

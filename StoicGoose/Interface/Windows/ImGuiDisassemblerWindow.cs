using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using ImGuiNET;

using StoicGoose.Debugging;
using StoicGoose.Emulation;
using StoicGoose.Emulation.CPU;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiDisassemblerWindow : ImGuiWindowBase
	{
		/* Consts, dicts, enums... */
		const int segmentSize = 0x10000;
		const int maxInternalRamSize = 0x10000;
		const int maxOpcodeBytes = 8;

		/* Public options */
		public bool UpperCaseHex { get { return upperCaseHex; } set { upperCaseHex = value; } }
		public bool TraceExecution { get { return traceExecution; } set { traceExecution = value; } }
		public uint HighlightColor1 { get; set; } = 0;
		public uint HighlightColor2 { get; set; } = 0;

		/* Backing fields for options etc */
		bool traceExecution = true, upperCaseHex = true;

		/* Internal stuff */
		ushort codeSegment = 0x0000;
		readonly List<ushort> instructionAddresses = new();
		readonly List<ushort> stackAddresses = new(), stackAddressesAscending = new();
		bool jumpToIpNext = false, jumpToSpNext = false;
		string disasmAddrInputBuf = new('\0', 32), stackAddrInputBuf = new('\0', 32);
		int disasmGotoAddr = -1, stackGotoAddr = -1;
		uint colorText = 0, colorDisabled = 0;

		/* Sizing variables */
		float lineHeight = 0f, glyphWidth = 0f, hexCellWidth = 0f;
		float posDisasmAddrStart = 0f, posDisasmAddrEnd = 0f, posDisasmHexStart = 0f, posDisasmHexEnd = 0f, posDisasmMnemonicStart = 0f;
		float posStackAddrEnd = 0f, posStackHexStart = 0f;

		/* Functional stuffs */
		readonly Disassembler disassembler = new();

		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;

		public event EventHandler<EventArgs> PauseEmulation;
		public void OnPauseEmulation(EventArgs e) { PauseEmulation?.Invoke(this, e); }

		public event EventHandler<EventArgs> UnpauseEmulation;
		public void OnUnpauseEmulation(EventArgs e) { UnpauseEmulation?.Invoke(this, e); }

		public ImGuiDisassemblerWindow() : base("Disassembler", new NumericsVector2(700f, 635f), ImGuiCond.Always)
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

			posDisasmAddrStart = glyphWidth * 3f;
			posDisasmAddrEnd = posDisasmAddrStart + (glyphWidth * 9f);
			posDisasmHexStart = posDisasmAddrEnd + glyphWidth;
			posDisasmHexEnd = posDisasmHexStart + (hexCellWidth * maxOpcodeBytes);
			posDisasmMnemonicStart = posDisasmHexEnd + glyphWidth;

			posStackAddrEnd = glyphWidth * 6.5f;
			posStackHexStart = posStackAddrEnd + glyphWidth;
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not EmulatorHandler handler) return;

			if (HighlightColor1 == 0) HighlightColor1 = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
			if (HighlightColor2 == 0) HighlightColor2 = 0x1F000000 | (ImGui.GetColorU32(ImGuiCol.Text) & 0x00FFFFFF);

			if (colorText == 0) colorText = ImGui.GetColorU32(ImGuiCol.Text);
			if (colorDisabled == 0) colorDisabled = ImGui.GetColorU32(ImGuiCol.TextDisabled);

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

			if (stackAddresses.Count == 0)
				for (var i = maxInternalRamSize - 2; i >= 0; i -= 2)
					stackAddresses.Add((ushort)i);
			if (stackAddressesAscending.Count == 0)
				for (var i = 0; i < maxInternalRamSize; i += 2)
					stackAddressesAscending.Add((ushort)i);

			var style = ImGui.GetStyle();

			var processorHeight = style.ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + (ImGui.GetTextLineHeightWithSpacing() * 5f);
			var stackWidth = style.ItemSpacing.X + glyphWidth * 11.5f + style.ScrollbarSize;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoResize))
			{
				if (ImGui.BeginChild("##disassembly-scroll", new NumericsVector2(-stackWidth, 390f), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var drawListDisasm = ImGui.GetWindowDrawList();

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin(instructionAddresses.Count, lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posDisasmAddrStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posDisasmAddrStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posDisasmHexStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posDisasmHexStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posDisasmMnemonicStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posDisasmMnemonicStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

						static void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * ImGui.GetTextLineHeight()) - ImGui.GetScrollY(), 0.5f);

						if (traceExecution || jumpToIpNext)
						{
							var idx = instructionAddresses.IndexOf(handler.Machine.Cpu.Registers.IP);
							if (idx != -1)
							{
								centerScrollTo(idx);
								disasmAddrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", instructionAddresses[idx]);
							}
						}

						if (disasmGotoAddr != -1)
						{
							var idx = instructionAddresses.BinarySearch((ushort)disasmGotoAddr);
							if (idx < 0) idx = ~idx;
							disasmAddrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", instructionAddresses[idx]);
							centerScrollTo(idx);
						}

						jumpToIpNext = false;
						disasmGotoAddr = -1;

						while (clipper.Step())
						{
							if (!traceExecution && ImGui.IsWindowFocused())
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
								ImGui.SameLine(posDisasmAddrStart);

								ImGui.TextUnformatted(string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}:{{1:{(upperCaseHex ? "X" : "x")}4}}", codeSegment, instructionAddresses[i]));
								ImGui.SameLine(posDisasmHexStart);

								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{string.Join(" ", bytes.Select(x => $"{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}2}}", x)}"))}");
								ImGui.SameLine(posDisasmMnemonicStart);

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

				ImGui.SameLine();

				if (ImGui.BeginChild("##stack-scroll", new NumericsVector2(0f, 390f), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var drawListStack = ImGui.GetWindowDrawList();

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin(stackAddresses.Count, lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						drawListStack.AddLine(new NumericsVector2(windowPos.X + posStackHexStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posStackHexStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

						static void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * ImGui.GetTextLineHeight()) - ImGui.GetScrollY(), 0.5f);

						if (traceExecution || jumpToSpNext)
						{
							var idx = stackAddresses.IndexOf(handler.Machine.Cpu.Registers.SP);
							if (idx != -1)
							{
								centerScrollTo(idx);
								stackAddrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", stackAddresses[idx]);
							}
						}

						if (stackGotoAddr != -1)
						{
							var idx = stackAddressesAscending.BinarySearch((ushort)stackGotoAddr);
							if (idx < 0) idx = ~idx;
							idx = stackAddresses.Count - 1 - idx;
							stackAddrInputBuf = string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", stackAddresses[idx]);
							centerScrollTo(idx);
						}

						jumpToSpNext = false;
						stackGotoAddr = -1;

						while (clipper.Step())
						{
							if (!traceExecution && ImGui.IsWindowFocused())
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
									ImGui.SetScrollY(stackAddresses.Count * clipper.ItemsHeight);
							}

							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var pos = ImGui.GetCursorScreenPos();

								if (handler.Machine.Cpu.Registers.SP == stackAddresses[i])
									drawListStack.AddRectFilled(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor1);

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawListStack.AddRect(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor2);

								var value = (ushort)(handler.Machine.ReadMemory((uint)(stackAddresses[i] + 1)) << 8 | handler.Machine.ReadMemory(stackAddresses[i]));

								ImGui.TextUnformatted(string.Format($"0x{{0:{(upperCaseHex ? "X" : "x")}4}}", stackAddresses[i]));
								ImGui.SameLine(posStackHexStart);
								ImGui.TextUnformatted(string.Format($"0x{{0:{(upperCaseHex ? "X" : "x")}4}}", value));
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

				if (ImGui.BeginChild("##controls", new NumericsVector2(0f, -processorHeight)))
				{
					if (!handler.IsRunning) ImGui.BeginDisabled();

					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new NumericsVector2(10f, 8f));

					var contentAvailWidth = ImGui.GetContentRegionAvail().X;
					var buttonWidth = (ImGui.GetContentRegionAvail().X - (style.ItemSpacing.X * 3f)) / 4f;

					if (!handler.IsPaused)
					{
						if (ImGui.Button("Pause", new NumericsVector2(buttonWidth, 0f))) OnPauseEmulation(EventArgs.Empty);
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
						if (ImGui.Button("Unpause", new NumericsVector2(buttonWidth, 0f))) OnUnpauseEmulation(EventArgs.Empty);
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
					ImGui.SetCursorPosX(contentAvailWidth - (buttonWidth * 2f) - style.ItemSpacing.X);
					if (traceExecution)
					{
						ImGui.BeginDisabled();
						ImGui.Button("Jump to IP", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Text("Code jump: ");
						ImGui.SameLine();
						ImGui.InputText("##disasm-addr", ref disasmAddrInputBuf, 4, gotoInputFlags);
						ImGui.EndDisabled();
					}
					else
					{
						if (ImGui.Button("Jump to IP", new NumericsVector2(buttonWidth, 0f))) jumpToIpNext = true;
						ImGui.SameLine();
						ImGui.Text("Code jump: ");
						ImGui.SameLine();
						if (ImGui.InputText("##disasm-addr", ref disasmAddrInputBuf, 4, gotoInputFlags))
							disasmGotoAddr = int.Parse(disasmAddrInputBuf, System.Globalization.NumberStyles.HexNumber);
					}

					ImGui.Checkbox("Trace execution", ref traceExecution);
					ImGui.SameLine();
					ImGui.SetCursorPosX(contentAvailWidth - (buttonWidth * 2f) - style.ItemSpacing.X);
					if (traceExecution)
					{
						ImGui.BeginDisabled();
						ImGui.Button("Jump to SP", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Text("Stack jump:");
						ImGui.SameLine();
						ImGui.InputText("##stack-addr", ref stackAddrInputBuf, 4, gotoInputFlags);
						ImGui.EndDisabled();
					}
					else
					{
						if (ImGui.Button("Jump to SP", new NumericsVector2(buttonWidth, 0f))) jumpToSpNext = true;
						ImGui.SameLine();
						ImGui.Text("Stack jump:");
						ImGui.SameLine();
						if (ImGui.InputText("##stack-addr", ref stackAddrInputBuf, 4, gotoInputFlags))
							stackGotoAddr = int.Parse(stackAddrInputBuf, System.Globalization.NumberStyles.HexNumber);
					}

					ImGui.PopStyleVar();

					if (!handler.IsRunning) ImGui.EndDisabled();

					ImGui.EndChild();
				}

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				if (ImGui.BeginChild("##processor", NumericsVector2.Zero))
				{
					// TODO: better layout?

					var drawListProcessor = ImGui.GetWindowDrawList();

					var height = ImGui.GetTextLineHeightWithSpacing();

					var windowPos = ImGui.GetWindowPos();
					drawListProcessor.AddLine(new NumericsVector2(windowPos.X + glyphWidth * 48f, windowPos.Y), new NumericsVector2(windowPos.X + glyphWidth * 48f, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
					drawListProcessor.AddLine(new NumericsVector2(windowPos.X, windowPos.Y + height + 2f), new NumericsVector2(windowPos.X + glyphWidth * 47f, windowPos.Y + height + 2f), ImGui.GetColorU32(ImGuiCol.Border));
					drawListProcessor.AddLine(new NumericsVector2(windowPos.X + glyphWidth * 49f, windowPos.Y + height + 2f), new NumericsVector2(windowPos.X + 9999, windowPos.Y + height + 2f), ImGui.GetColorU32(ImGuiCol.Border));

					var pos = ImGui.GetCursorScreenPos();
					var posStart = pos;

					drawListProcessor.AddText(pos, colorText, $"IP: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.IP)}"); pos.X += glyphWidth * 19f;

					drawListProcessor.AddText(pos, colorText, "Flags:"); pos.X += glyphWidth * 6f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Carry, "CF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Parity, "PF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Auxiliary, "AF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Zero, "ZF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Sign, "SF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Trap, "TF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.InterruptEnable, "IF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Direction, "DF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, handler.Machine.Cpu, V30MZ.Flags.Overflow, "OF"); pos.X += glyphWidth * 2.5f;
					pos.X = posStart.X;
					pos.Y += height * 1.5f;

					DrawRegister(drawListProcessor, pos, handler.Machine.Cpu.Registers.AX, "A"); pos.Y += height;
					DrawRegister(drawListProcessor, pos, handler.Machine.Cpu.Registers.BX, "B"); pos.Y += height;
					DrawRegister(drawListProcessor, pos, handler.Machine.Cpu.Registers.CX, "C"); pos.Y += height;
					DrawRegister(drawListProcessor, pos, handler.Machine.Cpu.Registers.DX, "D"); pos.Y += height;
					pos.Y = posStart.Y;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"CPU Halted? {handler.Machine.Cpu.IsHalted}"); pos.X += glyphWidth * 25f;
					drawListProcessor.AddText(pos, colorText, $"Scanline: {handler.Machine.DisplayController.LineCurrent,3}");
					pos.Y += height + lineHeight * 0.75f;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"SP: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.SP)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.SP}]"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorText, $"CS: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.CS)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.CS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"BP: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.BP)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.BP}]"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorText, $"DS: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.DS)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.DS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"SI: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.SI)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.SI}]"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorText, $"SS: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.SS)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.SS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"DI: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.DI)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.DI}]"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorText, $"ES: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", handler.Machine.Cpu.Registers.ES)}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{handler.Machine.Cpu.Registers.ES}]"); pos.Y += height;

					ImGui.EndChild();
				}

				ImGui.End();
			}
		}

		private void DrawFlag(ImDrawListPtr drawList, NumericsVector2 position, V30MZ cpu, V30MZ.Flags flag, string label)
		{
			drawList.AddText(position, cpu.IsFlagSet(flag) ? colorText : colorDisabled, label);
		}

		private void DrawRegister(ImDrawListPtr drawList, NumericsVector2 position, V30MZ.Register16 register, string label)
		{
			drawList.AddText(position, colorText, $"{label}X: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}4}}", register.Word)}"); position.X += glyphWidth * 10f;
			drawList.AddText(position, colorDisabled, $"[{register.Word}]"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorText, $"{label}L: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}2}}", register.Low)}"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.Low}]"); position.X += glyphWidth * 6f;
			drawList.AddText(position, colorText, $"{label}H: 0x{string.Format($"{{0:{(upperCaseHex ? "X" : "x")}2}}", register.High)}"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.High}]"); position.X += glyphWidth * 6f;
		}
	}
}

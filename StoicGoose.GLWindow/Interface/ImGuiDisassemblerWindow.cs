using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using ImGuiNET;

using StoicGoose.Core.CPU;
using StoicGoose.Core.Machines;
using StoicGoose.GLWindow.Debugging;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface
{
	public class ImGuiDisassemblerWindow : ImGuiWindowBase
	{
		/* Consts, dicts, enums... */
		const int maxInternalRamSize = 0x10000;
		const int maxOpcodeBytes = 8;

		/* Public options */
		public bool TraceExecution { get { return traceExecution; } set { traceExecution = value; } }
		public uint HighlightColor1 { get; set; } = 0;
		public uint HighlightColor2 { get; set; } = 0;

		/* Backing fields for options etc */
		bool traceExecution = true;

		/* Internal stuff */
		readonly List<Instruction> dummyInstructions = new();
		ushort codeSegment = 0x0000;
		List<Instruction> instructions = new();
		readonly List<ushort> stackAddresses = new(), stackAddressesAscending = new();
		bool jumpToIpNext = false, jumpToSpNext = false;
		string disasmAddrInputBuf = new('\0', 32), stackAddrInputBuf = new('\0', 32);
		int disasmGotoAddr = -1, stackGotoAddr = -1;
		uint colorText = 0, colorDisabled = 0;

		/* Sizing variables */
		float lineHeight = 0f, glyphWidth = 0f, hexCellWidth = 0f;
		float posDisasmAddrStart = 0f, posDisasmAddrEnd = 0f, posDisasmHexStart = 0f, posDisasmHexEnd = 0f, posDisasmMnemonicStart = 0f;
		float posStackAddrEnd = 0f, posStackDivStart = 0f, posStackHexStart = 0f;

		/* Functional stuffs */
		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;

		string doModifyRegisterName = string.Empty;
		ushort newRegisterValue = 0;

		public event EventHandler<EventArgs> PauseEmulation;
		private void OnPauseEmulation(EventArgs e) => PauseEmulation?.Invoke(this, e);

		public event EventHandler<EventArgs> UnpauseEmulation;
		private void OnUnpauseEmulation(EventArgs e) => UnpauseEmulation?.Invoke(this, e);

		public ImGuiDisassemblerWindow() : base("Disassembler", new NumericsVector2(720f, 635f), ImGuiCond.Always)
		{
			dummyInstructions.AddRange(Enumerable.Range(0, 0x10000).Select(x => new Instruction() { Address = (ushort)x, Mnemonic = "---" }));

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

			posDisasmAddrStart = 0f;
			posDisasmAddrEnd = posDisasmAddrStart + (glyphWidth * 9f);
			posDisasmHexStart = posDisasmAddrEnd + glyphWidth;
			posDisasmHexEnd = posDisasmHexStart + (hexCellWidth * maxOpcodeBytes);
			posDisasmMnemonicStart = posDisasmHexEnd + glyphWidth;

			posStackAddrEnd = glyphWidth * 5.5f;
			posStackDivStart = posStackAddrEnd + 0.75f;
			posStackHexStart = posStackDivStart + glyphWidth;
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not (IMachine machine, ThreadedDisassembler disassembler, bool isRunning, bool isPaused)) return;

			if (HighlightColor1 == 0) HighlightColor1 = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
			if (HighlightColor2 == 0) HighlightColor2 = 0x1F000000 | (ImGui.GetColorU32(ImGuiCol.Text) & 0x00FFFFFF);

			if (colorText == 0) colorText = ImGui.GetColorU32(ImGuiCol.Text);
			if (colorDisabled == 0) colorDisabled = ImGui.GetColorU32(ImGuiCol.TextDisabled);

			CalcSizes();

			if (instructions.Count == 0 || codeSegment != machine.Cpu.CS || instructions == dummyInstructions)
			{
				codeSegment = machine.Cpu.CS;
				instructions = disassembler.GetSegmentInstructions(codeSegment) ?? dummyInstructions;
			}

			if (stackAddresses.Count == 0)
				for (var i = maxInternalRamSize - 2; i >= 0; i -= 2)
					stackAddresses.Add((ushort)i);
			if (stackAddressesAscending.Count == 0)
				for (var i = 0; i < maxInternalRamSize; i += 2)
					stackAddressesAscending.Add((ushort)i);

			var style = ImGui.GetStyle();

			var processorHeight = style.ItemSpacing.Y + ImGui.GetFrameHeightWithSpacing() + (ImGui.GetTextLineHeightWithSpacing() * 5f);
			var stackWidth = style.ItemSpacing.X + glyphWidth * 13.5f + style.ScrollbarSize;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
			{
				if (ImGui.BeginChild("##disassembly-scroll", new NumericsVector2(-(stackWidth + 1f), 390f), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					if (instructions == dummyInstructions)
						ImGui.BeginDisabled();

					var drawListDisasm = ImGui.GetWindowDrawList();

					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin(instructions.Count, lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posDisasmHexStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posDisasmHexStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
						drawListDisasm.AddLine(new NumericsVector2(windowPos.X + posDisasmMnemonicStart - glyphWidth, windowPos.Y), new NumericsVector2(windowPos.X + posDisasmMnemonicStart - glyphWidth, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));

						static void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * ImGui.GetTextLineHeight()) - ImGui.GetScrollY(), 0.5f);

						if (traceExecution || jumpToIpNext)
						{
							var idx = instructions.FindIndex(x => x.Address == machine.Cpu.IP);
							if (idx != -1)
							{
								centerScrollTo(idx);
								disasmAddrInputBuf = $"{instructions[idx].Address:x4}";
							}
						}

						if (disasmGotoAddr != -1)
						{
							var idx = instructions.Select(x => x.Address).ToList().BinarySearch((ushort)disasmGotoAddr);
							if (idx < 0) idx = ~idx;
							disasmAddrInputBuf = $"{instructions[idx].Address:x4}";
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
									ImGui.SetScrollY(instructions.Count * clipper.ItemsHeight);
							}

							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var pos = ImGui.GetCursorScreenPos();

								if (machine.Cpu.IP == instructions[i].Address)
									drawListDisasm.AddRectFilled(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor1);

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawListDisasm.AddRect(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor2);

								ImGui.TextUnformatted($"{codeSegment:x4}:{instructions[i].Address:x4}");
								ImGui.SameLine(posDisasmHexStart);

								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{string.Join(" ", instructions[i].Bytes.Select(x => $"{x:x2}"))}");
								ImGui.SameLine(posDisasmMnemonicStart);

								ImGui.TextUnformatted($"{instructions[i].Mnemonic} {instructions[i].Operand}");
								ImGui.SameLine();
								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{(!string.IsNullOrEmpty(instructions[i].Comment) ? $" ; {instructions[i].Comment}" : string.Empty)}");
							}
						}
						clipper.End();
					}
					ImGui.PopStyleVar(2);
					ImGui.EndChild();

					if (instructions == dummyInstructions)
						ImGui.EndDisabled();
				}

				ImGui.SameLine();

				if (ImGui.BeginChild("##disasm-stack-divider", new NumericsVector2(1f, 390f), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var pos = ImGui.GetCursorScreenPos();
					ImGui.GetWindowDrawList().AddLine(pos, pos + new NumericsVector2(0f, 390f), ImGui.GetColorU32(ImGuiCol.Border));
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
						static void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * ImGui.GetTextLineHeight()) - ImGui.GetScrollY(), 0.5f);

						if (traceExecution || jumpToSpNext)
						{
							var idx = stackAddresses.IndexOf(machine.Cpu.SP);
							if (idx != -1)
							{
								centerScrollTo(idx);
								stackAddrInputBuf = $"{stackAddresses[idx]:x4}";
							}
						}

						if (stackGotoAddr != -1)
						{
							var idx = stackAddressesAscending.BinarySearch((ushort)stackGotoAddr);
							if (idx < 0) idx = ~idx;
							idx = stackAddresses.Count - 1 - idx;
							stackAddrInputBuf = $"{stackAddresses[idx]:x4}";
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

								if (machine.Cpu.SP == stackAddresses[i])
									drawListStack.AddRectFilled(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor1);

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawListStack.AddRect(pos, new NumericsVector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + lineHeight), HighlightColor2);

								var value = (ushort)(machine.ReadMemory((uint)(stackAddresses[i] + 1)) << 8 | machine.ReadMemory(stackAddresses[i]));

								ImGui.TextUnformatted($"0x{stackAddresses[i]:x4}");
								ImGui.SameLine(posStackDivStart);
								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), ":");
								ImGui.SameLine(posStackHexStart);
								ImGui.TextUnformatted($"0x{value:x4}");
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
					if (!isRunning) ImGui.BeginDisabled();

					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new NumericsVector2(10f, 8f));

					var contentAvailWidth = ImGui.GetContentRegionAvail().X;
					var buttonWidth = (ImGui.GetContentRegionAvail().X - (style.ItemSpacing.X * 3f)) / 4f;

					var gotoInputFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue;

					if (!isPaused)
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

					if (ImGui.Button("Reset", new NumericsVector2(buttonWidth, 0f))) machine.Reset();
					ImGui.SameLine();
					if (ImGui.Button("Reset -> Pause", new NumericsVector2(buttonWidth, 0f))) { OnPauseEmulation(EventArgs.Empty); machine.Reset(); }
					ImGui.SameLine();
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

					ImGui.Checkbox("Trace execution", ref traceExecution);
					ImGui.SameLine();
					ImGui.SetCursorPosX(contentAvailWidth - (buttonWidth * 3f) - (style.ItemSpacing.X * 2f));
					if (isPaused)
					{
						ImGui.PushButtonRepeat(true);
						if (ImGui.Button("Step Instruction", new NumericsVector2(buttonWidth, 0f))) machine.RunStep();
						ImGui.SameLine();
						if (ImGui.Button("Step Scanline", new NumericsVector2(buttonWidth, 0f))) machine.RunLine();
						ImGui.SameLine();
						if (ImGui.Button("Step Frame", new NumericsVector2(buttonWidth, 0f))) machine.RunFrame();
						ImGui.PopButtonRepeat();
					}
					else
					{
						ImGui.BeginDisabled();
						ImGui.Button("Step Instruction", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Button("Step Scanline", new NumericsVector2(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Button("Step Frame", new NumericsVector2(buttonWidth, 0f));
						ImGui.EndDisabled();
					}

					ImGui.PopStyleVar();

					if (!isRunning) ImGui.EndDisabled();

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

					drawListProcessor.AddText(pos, colorText, $"IP: 0x{machine.Cpu.IP:x4}"); pos.X += glyphWidth * 19f;

					drawListProcessor.AddText(pos, colorText, "Flags:"); pos.X += glyphWidth * 6f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Carry, "CF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Parity, "PF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Auxiliary, "AF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Zero, "ZF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Sign, "SF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Trap, "TF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.InterruptEnable, "IF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Direction, "DF"); pos.X += glyphWidth * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Overflow, "OF"); pos.X += glyphWidth * 2.5f;
					pos.X = posStart.X;
					pos.Y += height * 1.5f;

					if (DrawRegister(drawListProcessor, pos, machine.Cpu.AX, "A")) { machine.Cpu.AX = newRegisterValue; }
					pos.Y += height;
					if (DrawRegister(drawListProcessor, pos, machine.Cpu.BX, "B")) { machine.Cpu.BX = newRegisterValue; }
					pos.Y += height;
					if (DrawRegister(drawListProcessor, pos, machine.Cpu.CX, "C")) { machine.Cpu.CX = newRegisterValue; }
					pos.Y += height;
					if (DrawRegister(drawListProcessor, pos, machine.Cpu.DX, "D")) { machine.Cpu.DX = newRegisterValue; }
					pos.Y = posStart.Y;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"CPU Halted? {machine.Cpu.IsHalted}"); pos.X += glyphWidth * 27f;
					drawListProcessor.AddText(pos, colorText, $"Scanline: {machine.DisplayController.LineCurrent,3}");
					pos.Y += height + lineHeight * 0.75f;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"SP: 0x{machine.Cpu.SP:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SP}]"); pos.X += glyphWidth * 11f;
					drawListProcessor.AddText(pos, colorText, $"CS: 0x{machine.Cpu.CS:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.CS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"BP: 0x{machine.Cpu.BP:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.BP}]"); pos.X += glyphWidth * 11f;
					drawListProcessor.AddText(pos, colorText, $"DS: 0x{machine.Cpu.DS:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.DS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"SI: 0x{machine.Cpu.SI:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SI}]"); pos.X += glyphWidth * 11f;
					drawListProcessor.AddText(pos, colorText, $"SS: 0x{machine.Cpu.SS:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphWidth * 49f;
					drawListProcessor.AddText(pos, colorText, $"DI: 0x{machine.Cpu.DI:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.DI}]"); pos.X += glyphWidth * 11f;
					drawListProcessor.AddText(pos, colorText, $"ES: 0x{machine.Cpu.ES:x4}"); pos.X += glyphWidth * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.ES}]"); pos.Y += height;

					ImGui.EndChild();
				}

				ImGui.End();
			}
		}

		private void DrawFlag(ImDrawListPtr drawList, NumericsVector2 position, V30MZ cpu, V30MZ.Flags flag, string label)
		{
			drawList.AddText(position, cpu.IsFlagSet(flag) ? colorText : colorDisabled, label);
		}

		private bool DrawRegister(ImDrawListPtr drawList, NumericsVector2 position, V30MZ.Register16 register, string label)
		{
			var result = false;

			var padding = ImGui.GetStyle().ItemSpacing.Y / 2f;
			var mousePosition = ImGui.GetMousePos();
			var rectPos = new NumericsVector2(position.X + glyphWidth * 3f, position.Y - padding);
			var rectSize = new NumericsVector2(glyphWidth * 14f, lineHeight + padding);

			drawList.AddText(position, colorText, $"{label}X: 0x{register.Word:x4}"); position.X += glyphWidth * 10f;
			drawList.AddText(position, colorDisabled, $"[{register.Word}]"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorText, $"{label}L: 0x{register.Low:x2}"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.Low}]"); position.X += glyphWidth * 6f;
			drawList.AddText(position, colorText, $"{label}H: 0x{register.High:x2}"); position.X += glyphWidth * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.High}]"); position.X += glyphWidth * 6f;

			if (string.IsNullOrEmpty(doModifyRegisterName) && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && ImGuiHelpers.IsPointInsideRectangle(mousePosition, rectPos, rectSize))
			{
				drawList.AddRectFilled(rectPos, rectPos + rectSize, HighlightColor1);
				if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
				{
					newRegisterValue = register.Word;
					doModifyRegisterName = label;
				}
			}

			if (!string.IsNullOrEmpty(doModifyRegisterName) && doModifyRegisterName == label)
			{
				ImGui.OpenPopup($"Modify {label}X##modify-reg{label}");

				var viewportCenter = ImGui.GetMainViewport().GetCenter();
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new NumericsVector2(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal($"Modify {label}X##modify-reg{label}", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					ImGuiHelpers.InputHex("New Value##modify-value", ref newRegisterValue, 4, false);

					ImGui.Dummy(new NumericsVector2(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new NumericsVector2(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Apply", new NumericsVector2(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						ImGui.CloseCurrentPopup();
						result = true;
						doModifyRegisterName = string.Empty;
					}
					ImGui.SameLine();
					if (ImGui.Button("Cancel", new NumericsVector2(ImGui.GetContentRegionAvail().X, 0f)))
					{
						ImGui.CloseCurrentPopup();
						doModifyRegisterName = string.Empty;
					}

					ImGui.PopStyleVar();

					ImGui.EndPopup();
				}

				if (!popupDummy)
					doModifyRegisterName = string.Empty;
			}

			return result;
		}
	}
}

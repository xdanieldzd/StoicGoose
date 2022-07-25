using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

using ImGuiNET;

using StoicGoose.Core.CPU;
using StoicGoose.Core.Interfaces;
using StoicGoose.GLWindow.Debugging;
using StoicGoose.ImGuiCommon.Windows;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.GLWindow.Interface.Windows
{
	public class DisassemblerWindow : WindowBase, IDisposable
	{
		const int segmentSize = 0x10000;
		const int maxInternalRamSize = 0x10000;

		/* Disassembler */
		Disassembler disassembler = default;

		/* Positioning/sizing/coloring variables */
		NumericsVector2 controlsItemSpacing = NumericsVector2.One, glyphSize = NumericsVector2.Zero;
		float lineHeight = 0f, hexCellWidth = 0f, scrollerHeight = 0f, stackWidth = 0f, controlsHeight = 0f;
		float posDisasmBytesStart = 0f, posDisasmMnemonicStart = 0f;
		uint highlightColor1 = 0, highlightColor2 = 0;
		uint colorText = 0, colorDisabled = 0;

		/* Scrolling/jumping */
		readonly ImGuiListClipper clipperObject = default;
		readonly GCHandle clipperHandle = default;
		readonly IntPtr clipperPointer = IntPtr.Zero;
		readonly Dictionary<ushort, List<ushort>> instructionIndexCache = new();
		string disasmAddrInputBuf = new('\0', 32), stackAddrInputBuf = new('\0', 32);
		int disasmGotoAddr = -1, stackGotoAddr = -1;

		/* Editing */
		string doModifyRegisterName = string.Empty;
		ushort newRegisterValue = 0;

		/* Misc functional stuff */
		bool traceExecution = true, jumpToIpNext = false, jumpToSpNext = false;

		/* External events */
		public event EventHandler<EventArgs> PauseEmulation;
		private void OnPauseEmulation(EventArgs e) => PauseEmulation?.Invoke(this, e);

		public event EventHandler<EventArgs> UnpauseEmulation;
		private void OnUnpauseEmulation(EventArgs e) => UnpauseEmulation?.Invoke(this, e);

		public DisassemblerWindow() : base("Disassembler", new(700f, 600f), ImGuiCond.Always)
		{
			clipperObject = new();
			clipperHandle = GCHandle.Alloc(clipperObject, GCHandleType.Pinned);
			clipperPointer = clipperHandle.AddrOfPinnedObject();
		}

		~DisassemblerWindow()
		{
			Dispose();
		}

		public void Dispose()
		{
			clipperHandle.Free();

			GC.SuppressFinalize(this);
		}

		protected override void InitializeWindow(object userData)
		{
			if (userData is not (IMachine machine, bool _, bool _)) return;

			disassembler = new(machine);

			var style = ImGui.GetStyle();

			controlsItemSpacing = new(10f, 8f);

			lineHeight = ImGui.GetTextLineHeight();
			glyphSize = ImGui.CalcTextSize("F") + NumericsVector2.One;
			hexCellWidth = glyphSize.X * 2.5f;
			scrollerHeight = lineHeight * 28f;
			stackWidth = glyphSize.X * 9f + style.ScrollbarSize;
			controlsHeight = (glyphSize.Y + (style.FramePadding.Y + controlsItemSpacing.Y)) * 3f;

			posDisasmBytesStart = glyphSize.X * 10f;
			posDisasmMnemonicStart = posDisasmBytesStart + hexCellWidth * 9f;

			highlightColor1 = 0x3F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
			highlightColor2 = 0x1F000000 | (ImGui.GetColorU32(ImGuiCol.Text) & 0x00FFFFFF);

			colorText = ImGui.GetColorU32(ImGuiCol.Text);
			colorDisabled = ImGui.GetColorU32(ImGuiCol.TextDisabled);
		}

		protected override void DrawWindow(object userData)
		{
			if (userData is not (IMachine machine, bool isRunning, bool isPaused)) return;

			var codeSegment = machine.Cpu.CS;
			var instructionPointer = machine.Cpu.IP;
			var stackPointer = machine.Cpu.SP;

			var style = ImGui.GetStyle();

			void centerScrollTo(int idx) => ImGui.SetScrollFromPosY((idx * lineHeight) - ImGui.GetScrollY(), 0.5f);

			if (ImGui.Begin(WindowTitle, ref isWindowOpen, ImGuiWindowFlags.NoResize))
			{
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, NumericsVector2.Zero);
				ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, NumericsVector2.Zero);

				if (ImGui.BeginChild("##disassembly-scroll", new(-stackWidth, scrollerHeight), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin(segmentSize, lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						var drawList = ImGui.GetWindowDrawList();
						drawList.AddLine(new(windowPos.X + posDisasmBytesStart - glyphSize.X, windowPos.Y), new(windowPos.X + posDisasmBytesStart - glyphSize.X, windowPos.Y + 9999f), ImGui.GetColorU32(ImGuiCol.Border));
						drawList.AddLine(new(windowPos.X + posDisasmMnemonicStart - glyphSize.X, windowPos.Y), new(windowPos.X + posDisasmMnemonicStart - glyphSize.X, windowPos.Y + 9999f), ImGui.GetColorU32(ImGuiCol.Border));

						var instructions = disassembler.DisassembleSegment(codeSegment);

						if (!instructionIndexCache.ContainsKey(codeSegment))
						{
							var indices = new List<ushort>();
							for (var i = 0; i < clipper.ItemsCount; i++)
							{
								var idx = instructions.FindIndex(i, x => x.Address >= i);
								if (idx == -1) break;
								indices.Add(instructions[idx].Address);
							}
							instructionIndexCache.Add(codeSegment, indices);
						}

						var instructionAddresses = instructionIndexCache[codeSegment];

						while (clipper.Step())
						{
							if (traceExecution || jumpToIpNext)
							{
								var idx = instructionAddresses.BinarySearch(instructionPointer);
								if (idx < 0) idx = ~idx;

								idx = Math.Min(instructionAddresses.Count - 1, idx);
								disasmAddrInputBuf = $"{instructionAddresses[idx]:X4}";
								centerScrollTo(idx);
							}

							if (disasmGotoAddr != -1)
							{
								var idx = instructionAddresses.BinarySearch((ushort)disasmGotoAddr);
								if (idx < 0) idx = ~idx;

								idx = Math.Min(instructionAddresses.Count - 1, idx);
								disasmAddrInputBuf = $"{instructionAddresses[idx]:X4}";
								centerScrollTo(idx);
							}

							jumpToIpNext = false;
							disasmGotoAddr = -1;

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
									ImGui.SetScrollY((instructions.Count - 1) * clipper.ItemsHeight);
							}

							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var cursorScreenPos = ImGui.GetCursorScreenPos();
								var contentRegionAvailWidth = ImGui.GetContentRegionAvail().X;

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawList.AddRect(cursorScreenPos, new(cursorScreenPos.X + contentRegionAvailWidth, cursorScreenPos.Y + lineHeight), highlightColor2);

								var idx = i < instructionAddresses.Count ? instructions.FindIndex(x => x.Address == instructionAddresses[i]) : -1;
								if (idx != -1 && idx < instructions.Count)
								{
									var instruction = instructions[idx];

									if (codeSegment == instruction.Segment && instructionPointer == instruction.Address)
										drawList.AddRectFilled(cursorScreenPos, new(cursorScreenPos.X + contentRegionAvailWidth, cursorScreenPos.Y + lineHeight), highlightColor1);

									if (!instruction.IsValid) ImGui.BeginDisabled();
									ImGui.TextUnformatted($"{instruction.Segment:X4}:{instruction.Address:X4}");
									ImGui.SameLine(posDisasmBytesStart);
									ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{string.Join(' ', instruction.Bytes.Select(x => $"{x:X2}")).TrimEnd(' ')}");
									ImGui.SameLine(posDisasmMnemonicStart);
									ImGui.TextUnformatted(instruction.Mnemonic);
									ImGui.SameLine();
									ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), $"{(!string.IsNullOrEmpty(instruction.Comment) ? $" ; {instruction.Comment}" : string.Empty)}");
									if (!instruction.IsValid) ImGui.EndDisabled();
								}
								else
								{
									ImGui.BeginDisabled();
									ImGui.TextUnformatted($"----:----");
									ImGui.EndDisabled();
								}
							}
						}
					}
					ImGui.EndChild();
				}

				ImGui.SameLine();
				ImGui.Dummy(new(glyphSize.X, 0f));
				ImGui.SameLine();

				if (ImGui.BeginChild("##stack-scroll", new(0f, scrollerHeight), false, traceExecution ? ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar : ImGuiWindowFlags.None))
				{
					var clipper = new ImGuiListClipperPtr(clipperPointer);
					clipper.Begin((int)(maxInternalRamSize / 2f), lineHeight);
					{
						var windowPos = ImGui.GetWindowPos();
						var drawList = ImGui.GetWindowDrawList();

						while (clipper.Step())
						{
							if (traceExecution || jumpToSpNext)
							{
								stackAddrInputBuf = $"{stackPointer / 2 * 2:X4}";
								centerScrollTo((maxInternalRamSize - stackPointer - 2) / 2);
							}

							if (stackGotoAddr != -1)
							{
								stackAddrInputBuf = $"{stackGotoAddr / 2 * 2:X4}";
								centerScrollTo((maxInternalRamSize - (ushort)stackGotoAddr - 2) / 2);
							}

							jumpToSpNext = false;
							stackGotoAddr = -1;

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
									ImGui.SetScrollY(((maxInternalRamSize / 2f) - 1) * clipper.ItemsHeight);
							}

							for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
							{
								var cursorScreenPos = ImGui.GetCursorScreenPos();
								var contentRegionAvailWidth = ImGui.GetContentRegionAvail().X;

								if (i == clipper.DisplayStart + (clipper.DisplayEnd - clipper.DisplayStart) / 2f)
									drawList.AddRect(cursorScreenPos, new(cursorScreenPos.X + contentRegionAvailWidth, cursorScreenPos.Y + lineHeight), highlightColor2);

								var address = (uint)(maxInternalRamSize - 2 - i * 2);
								var value = (ushort)(machine.ReadMemory(address + 1) << 8 | machine.ReadMemory(address));

								if (stackPointer == (ushort)address)
									drawList.AddRectFilled(cursorScreenPos, new(cursorScreenPos.X + contentRegionAvailWidth, cursorScreenPos.Y + lineHeight), highlightColor1);

								ImGui.TextUnformatted($"{address:X4}");
								ImGui.SameLine();
								ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.TextDisabled)), ":");
								ImGui.SameLine();
								ImGui.TextUnformatted($"{value:X4}");
							}
						}
					}

					ImGui.EndChild();
				}

				ImGui.PopStyleVar(2);

				ImGui.Dummy(new(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new(0f, 2f));

				// TODO: clean up controls and cpu status stuffs

				if (ImGui.BeginChild("##controls", new(0f, controlsHeight)))
				{
					if (!isRunning) ImGui.BeginDisabled();

					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, controlsItemSpacing);

					var contentAvailWidth = ImGui.GetContentRegionAvail().X;
					var buttonWidth = (contentAvailWidth - (style.ItemSpacing.X * 3f)) / 4f;

					var gotoInputFlags = ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.EnterReturnsTrue;

					if (!isPaused)
					{
						if (ImGui.Button("Pause", new(buttonWidth, 0f))) OnPauseEmulation(EventArgs.Empty);
						ImGui.SameLine();
						ImGui.BeginDisabled();
						ImGui.Button("Unpause", new(buttonWidth, 0f));
						ImGui.EndDisabled();
					}
					else
					{
						ImGui.BeginDisabled();
						ImGui.Button("Pause", new(buttonWidth, 0f));
						ImGui.EndDisabled();
						ImGui.SameLine();
						if (ImGui.Button("Unpause", new(buttonWidth, 0f))) OnUnpauseEmulation(EventArgs.Empty);
					}
					ImGui.SameLine();
					if (traceExecution)
					{
						ImGui.BeginDisabled();
						ImGui.Button("Jump to IP", new(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Text("Code jump: ");
						ImGui.SameLine();
						ImGui.InputText("##disasm-addr", ref disasmAddrInputBuf, 4, gotoInputFlags);
						ImGui.EndDisabled();
					}
					else
					{
						if (ImGui.Button("Jump to IP", new(buttonWidth, 0f))) jumpToIpNext = true;
						ImGui.SameLine();
						ImGui.Text("Code jump: ");
						ImGui.SameLine();
						if (ImGui.InputText("##disasm-addr", ref disasmAddrInputBuf, 4, gotoInputFlags))
							disasmGotoAddr = int.Parse(disasmAddrInputBuf, NumberStyles.HexNumber);
					}

					if (ImGui.Button("Reset", new(buttonWidth, 0f))) machine.Reset();
					ImGui.SameLine();
					if (ImGui.Button("Reset -> Pause", new(buttonWidth, 0f))) { OnPauseEmulation(EventArgs.Empty); machine.Reset(); }
					ImGui.SameLine();
					if (traceExecution)
					{
						ImGui.BeginDisabled();
						ImGui.Button("Jump to SP", new(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Text("Stack jump:");
						ImGui.SameLine();
						ImGui.InputText("##stack-addr", ref stackAddrInputBuf, 4, gotoInputFlags);
						ImGui.EndDisabled();
					}
					else
					{
						if (ImGui.Button("Jump to SP", new(buttonWidth, 0f))) jumpToSpNext = true;
						ImGui.SameLine();
						ImGui.Text("Stack jump:");
						ImGui.SameLine();
						if (ImGui.InputText("##stack-addr", ref stackAddrInputBuf, 4, gotoInputFlags))
							stackGotoAddr = int.Parse(stackAddrInputBuf, NumberStyles.HexNumber);
					}

					ImGui.Checkbox("Trace execution", ref traceExecution);
					ImGui.SameLine();
					ImGui.SetCursorPosX(contentAvailWidth - (buttonWidth * 3f) - (style.ItemSpacing.X * 2f));
					if (isPaused)
					{
						ImGui.PushButtonRepeat(true);
						if (ImGui.Button("Step Instruction", new(buttonWidth, 0f))) machine.RunStep();
						ImGui.SameLine();
						if (ImGui.Button("Step Scanline", new(buttonWidth, 0f))) machine.RunLine();
						ImGui.SameLine();
						if (ImGui.Button("Step Frame", new(buttonWidth, 0f))) machine.RunFrame();
						ImGui.PopButtonRepeat();
					}
					else
					{
						ImGui.BeginDisabled();
						ImGui.Button("Step Instruction", new(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Button("Step Scanline", new(buttonWidth, 0f));
						ImGui.SameLine();
						ImGui.Button("Step Frame", new(buttonWidth, 0f));
						ImGui.EndDisabled();
					}

					ImGui.PopStyleVar();

					if (!isRunning) ImGui.EndDisabled();

					ImGui.EndChild();
				}

				ImGui.Dummy(new(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new(0f, 2f));

				if (ImGui.BeginChild("##processor", NumericsVector2.Zero))
				{
					// TODO: better layout?

					var drawListProcessor = ImGui.GetWindowDrawList();

					var height = ImGui.GetTextLineHeightWithSpacing();

					var windowPos = ImGui.GetWindowPos();
					drawListProcessor.AddLine(new(windowPos.X + glyphSize.X * 48f, windowPos.Y), new(windowPos.X + glyphSize.X * 48f, windowPos.Y + 9999), ImGui.GetColorU32(ImGuiCol.Border));
					drawListProcessor.AddLine(new(windowPos.X, windowPos.Y + height + 2f), new(windowPos.X + glyphSize.X * 47f, windowPos.Y + height + 2f), ImGui.GetColorU32(ImGuiCol.Border));
					drawListProcessor.AddLine(new(windowPos.X + glyphSize.X * 49f, windowPos.Y + height + 2f), new(windowPos.X + 9999, windowPos.Y + height + 2f), ImGui.GetColorU32(ImGuiCol.Border));

					var pos = ImGui.GetCursorScreenPos();
					var posStart = pos;

					drawListProcessor.AddText(pos, colorText, $"IP: 0x{machine.Cpu.IP:X4}"); pos.X += glyphSize.X * 19f;

					drawListProcessor.AddText(pos, colorText, "Flags:"); pos.X += glyphSize.X * 6f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Carry, "CF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Parity, "PF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Auxiliary, "AF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Zero, "ZF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Sign, "SF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Trap, "TF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.InterruptEnable, "IF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Direction, "DF"); pos.X += glyphSize.X * 2.5f;
					DrawFlag(drawListProcessor, pos, machine.Cpu, V30MZ.Flags.Overflow, "OF"); pos.X += glyphSize.X * 2.5f;
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

					pos.X = windowPos.X + glyphSize.X * 49f;
					drawListProcessor.AddText(pos, colorText, $"CPU Halted? {machine.Cpu.IsHalted}"); pos.X += glyphSize.X * 25f;
					drawListProcessor.AddText(pos, colorText, $"Scanline: {machine.DisplayController.LineCurrent,3}");
					pos.Y += height + lineHeight * 0.75f;

					pos.X = windowPos.X + glyphSize.X * 49f;
					drawListProcessor.AddText(pos, colorText, $"SP: 0x{machine.Cpu.SP:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SP}]"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorText, $"CS: 0x{machine.Cpu.CS:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.CS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphSize.X * 49f;
					drawListProcessor.AddText(pos, colorText, $"BP: 0x{machine.Cpu.BP:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.BP}]"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorText, $"DS: 0x{machine.Cpu.DS:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.DS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphSize.X * 49f;
					drawListProcessor.AddText(pos, colorText, $"SI: 0x{machine.Cpu.SI:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SI}]"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorText, $"SS: 0x{machine.Cpu.SS:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.SS}]"); pos.Y += height;

					pos.X = windowPos.X + glyphSize.X * 49f;
					drawListProcessor.AddText(pos, colorText, $"DI: 0x{machine.Cpu.DI:X4}"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorDisabled, $"[{machine.Cpu.DI}]"); pos.X += glyphSize.X * 10f;
					drawListProcessor.AddText(pos, colorText, $"ES: 0x{machine.Cpu.ES:X4}"); pos.X += glyphSize.X * 10f;
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
			var rectPos = new NumericsVector2(position.X + glyphSize.X * 3f, position.Y - padding);
			var rectSize = new NumericsVector2(glyphSize.X * 14f, lineHeight + padding);

			drawList.AddText(position, colorText, $"{label}X: 0x{register.Word:X4}"); position.X += glyphSize.X * 10f;
			drawList.AddText(position, colorDisabled, $"[{register.Word}]"); position.X += glyphSize.X * 8f;
			drawList.AddText(position, colorText, $"{label}L: 0x{register.Low:X2}"); position.X += glyphSize.X * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.Low}]"); position.X += glyphSize.X * 6f;
			drawList.AddText(position, colorText, $"{label}H: 0x{register.High:X2}"); position.X += glyphSize.X * 8f;
			drawList.AddText(position, colorDisabled, $"[{register.High}]"); position.X += glyphSize.X * 6f;

			if (string.IsNullOrEmpty(doModifyRegisterName) && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && Helpers.IsPointInsideRectangle(mousePosition, rectPos, rectSize))
			{
				drawList.AddRectFilled(rectPos, rectPos + rectSize, highlightColor1);
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
				ImGui.SetNextWindowPos(viewportCenter, ImGuiCond.Always, new(0.5f, 0.5f));

				var popupDummy = true;
				if (ImGui.BeginPopupModal($"Modify {label}X##modify-reg{label}", ref popupDummy, ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new NumericsVector2(5f));

					Helpers.InputHex("New Value##modify-value", ref newRegisterValue, 4, false);

					ImGui.Dummy(new(0f, 2f));
					ImGui.Separator();
					ImGui.Dummy(new(0f, 2f));

					ImGui.SetItemDefaultFocus();
					if (ImGui.Button("Apply", new(ImGui.GetContentRegionAvail().X / 2f, 0f)))
					{
						ImGui.CloseCurrentPopup();
						result = true;
						doModifyRegisterName = string.Empty;
					}
					ImGui.SameLine();
					if (ImGui.Button("Cancel", new(ImGui.GetContentRegionAvail().X, 0f)))
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

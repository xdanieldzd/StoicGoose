using ImGuiNET;

using StoicGoose.Emulation.CPU;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface
{
	public class ImGuiCpuWindow : ImGuiWindowBase
	{
		// TODO: rewrite to use table(s) instead of columns?

		float dbgDrawFlagsPosition = -1f;

		public ImGuiCpuWindow() : base("CPU Status", new NumericsVector2(425f, 185f), ImGuiCond.FirstUseEver) { }

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not V30MZ cpu) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				ImGui.BeginGroup();
				{
					var contentRegionMaxX = ImGui.GetWindowContentRegionMax().X;

					ImGui.Columns(2, "##reg-column1", false);
					ImGui.SetColumnWidth(0, contentRegionMaxX / 4f);
					ImGui.Text($"IP: 0x{cpu.Registers.IP:X4}"); ImGui.NextColumn();

					var cursorPos = ImGui.GetCursorPosX();
					if (dbgDrawFlagsPosition >= 0f)
						ImGui.SetCursorPosX(contentRegionMaxX - dbgDrawFlagsPosition);
					ImGui.Text("Flags:"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Carry, "CF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Parity, "PF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Auxiliary, "AF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Zero, "ZF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Sign, "SF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Trap, "TF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.InterruptEnable, "IF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Direction, "DF"); ImGui.SameLine();
					DrawFlag(cpu, V30MZ.Flags.Overflow, "OF"); ImGui.SameLine();

					if (dbgDrawFlagsPosition < 0f)
						dbgDrawFlagsPosition = ImGui.GetCursorPosX() - cursorPos - ImGui.GetStyle().WindowPadding.X;
				}
				ImGui.EndGroup();

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				ImGui.BeginGroup();
				{
					ImGui.Columns(3, "##reg-column2", false);
					DrawRegister(cpu.Registers.AX, "A");
					DrawRegister(cpu.Registers.BX, "B");
					DrawRegister(cpu.Registers.CX, "C");
					DrawRegister(cpu.Registers.DX, "D");
				}
				ImGui.EndGroup();

				ImGui.Dummy(new NumericsVector2(0f, 2f));
				ImGui.Separator();
				ImGui.Dummy(new NumericsVector2(0f, 2f));

				ImGui.BeginGroup();
				{
					ImGui.Columns(4, "##reg-column3", false);
					ImGui.Text($"SP: 0x{cpu.Registers.SP:X4}"); ImGui.NextColumn();
					ImGui.Text($"BP: 0x{cpu.Registers.BP:X4}"); ImGui.NextColumn();
					ImGui.Text($"SI: 0x{cpu.Registers.SI:X4}"); ImGui.NextColumn();
					ImGui.Text($"DI: 0x{cpu.Registers.DI:X4}"); ImGui.NextColumn();
					ImGui.Text($"CS: 0x{cpu.Registers.CS:X4}"); ImGui.NextColumn();
					ImGui.Text($"DS: 0x{cpu.Registers.DS:X4}"); ImGui.NextColumn();
					ImGui.Text($"SS: 0x{cpu.Registers.SS:X4}"); ImGui.NextColumn();
					ImGui.Text($"ES: 0x{cpu.Registers.ES:X4}"); ImGui.NextColumn();
				}
				ImGui.EndGroup();
			}
		}

		private void DrawFlag(V30MZ cpu, V30MZ.Flags flag, string label)
		{
			if (cpu.IsFlagSet(flag)) ImGui.Text(label);
			else ImGui.TextDisabled(label);
		}

		private void DrawRegister(V30MZ.Register16 register, string label)
		{
			ImGui.Text($"{label}X: 0x{register.Word:X4}"); ImGui.SameLine(); ImGui.TextDisabled($"[{register.Word}]"); ImGui.NextColumn();
			ImGui.Text($"{label}L: 0x{register.Low:X2}"); ImGui.SameLine(); ImGui.TextDisabled($"[{(sbyte)register.Low}]"); ImGui.NextColumn();
			ImGui.Text($"{label}H: 0x{register.High:X2}"); ImGui.SameLine(); ImGui.TextDisabled($"[{(sbyte)register.High}]"); ImGui.NextColumn();
		}
	}
}

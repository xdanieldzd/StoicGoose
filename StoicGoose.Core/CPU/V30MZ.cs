using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ : IComponent
	{
		// TODO: attempt prefetch emulation (Meitantei Conan - Nishi no Meitantei Saidai no Kiki; cart changes banks on startup, can no longer execute jump, execs garbage)

		/* General registers */
		Register16 ax, bx, cx, dx;
		ushort sp, bp, si, di;
		/* Segment registers */
		ushort cs, ds, ss, es;
		/* Status and instruction registers */
		ushort ip;
		Flags flags;

		bool halted;
		int opCycles, intCycles;

		/* Public properties for registers */
		public Register16 AX { get => ax; set => ax = value; }
		public Register16 BX { get => bx; set => bx = value; }
		public Register16 CX { get => cx; set => cx = value; }
		public Register16 DX { get => dx; set => dx = value; }
		public ushort SP { get => sp; set => sp = value; }
		public ushort BP { get => bp; set => bp = value; }
		public ushort SI { get => si; set => si = value; }
		public ushort DI { get => di; set => di = value; }
		public ushort CS { get => cs; set => cs = value; }
		public ushort DS { get => ds; set => ds = value; }
		public ushort SS { get => ss; set => ss = value; }
		public ushort ES { get => es; set => es = value; }
		public ushort IP { get => ip; set => ip = value; }

		public bool IsHalted { get => halted; set => halted = value; }

		readonly Func<ushort, ushort, string> disassembleOpcode = default;

		public V30MZ(MemoryReadDelegate memoryRead, MemoryWriteDelegate memoryWrite, RegisterReadDelegate registerRead, RegisterWriteDelegate registerWrite, Func<ushort, ushort, string> disassemble = null)
		{
			memoryReadDelegate = memoryRead;
			memoryWriteDelegate = memoryWrite;
			registerReadDelegate = registerRead;
			registerWriteDelegate = registerWrite;

			disassembleOpcode = disassemble;

			Reset();
		}

		public void Reset()
		{
			/* CPU reset */
			flags = Flags.ReservedB1 | Flags.ReservedB12 | Flags.ReservedB13 | Flags.ReservedB14 | Flags.ReservedB15;
			ip = 0x0000;
			cs = 0xFFFF;
			ds = 0x0000;
			ss = 0x0000;
			es = 0x0000;

			/* Initialized by WS bootstrap */
			ax.Word = 0x0000;
			dx.Word = 0x0000;
			bp = 0x0000;
			ss = 0x0000;
			sp = 0x2000;
			ds = 0x0000;
			es = 0x0000;

			/* Misc variables */
			halted = false;
			opCycles = intCycles = 0;

			ResetPrefixes();
			modRm.Reset();
		}

		public void Shutdown()
		{
			CloseTraceLogger();
		}

		public void Interrupt(int vector)
		{
			/* Resume execution */
			halted = false;

			/* Read interrupt handler's segment & offset */
			var offset = ReadMemory16(0, (ushort)((vector * 4) + 0));
			var segment = ReadMemory16(0, (ushort)((vector * 4) + 2));

			/* Push state, clear flags, etc. */
			Push((ushort)flags);
			Push(cs);
			Push(ip);

			ClearFlags(Flags.InterruptEnable);
			ClearFlags(Flags.Trap);

			ResetPrefixes();
			modRm.Reset();

			intCycles = 32;

			/* Continue with interrupt handler */
			cs = segment;
			ip = offset;
		}

		public int Step()
		{
			var cycles = 0;

			if (halted)
			{
				/* CPU is halted */
				cycles++;
			}
			else
			{
				/* If CPU trace logger is enabled and disassembly function was given, write to logger */
				if (isTraceLogOpen && disassembleOpcode != null)
				{
					var address = $"{cs:X4}:{ip:X4}";
					var disassemblyAndComment = disassembleOpcode(cs, ip);
					var registerStates = $"AX:{ax.Word:X4} BX:{bx.Word:X4} CX:{cx.Word:X4} DX:{dx.Word:X4} SP:{sp:X4} BP:{bp:X4} SI:{si:X4} DI:{di:X4}";
					var segmentStates = $"CS:{cs:X4} SS:{ss:X4} DS:{ds:X4} ES:{es:X4}";
					var flagStates =
						$"{(IsFlagSet(Flags.Carry) ? "C" : "-")}{(IsFlagSet(Flags.Parity) ? "P" : "-")}{(IsFlagSet(Flags.Auxiliary) ? "A" : "-")}{(IsFlagSet(Flags.Zero) ? "Z" : "-")}" +
						$"{(IsFlagSet(Flags.Sign) ? "S" : "-")}{(IsFlagSet(Flags.Trap) ? "T" : "-")}{(IsFlagSet(Flags.InterruptEnable) ? "I" : "-")}{(IsFlagSet(Flags.Direction) ? "D" : "-")}" +
						$"{(IsFlagSet(Flags.Overflow) ? "O" : "-")}";

					WriteToTraceLog($"{address} | {disassemblyAndComment,-84} | {registerStates} | {segmentStates} | {flagStates}");
				}

				/* Read any prefixes & opcode */
				byte opcode;
				while (!HandlePrefixes(opcode = ReadMemory8(cs, ip++))) { }

				/* Execute instruction */
				opCycles = instructions[opcode](this);
				if (opCycles == 0)
				{
					/* Assume invalid opcode, raise interrupt */
					Interrupt(6);
					cycles = 8;
				}
				cycles += opCycles;
				opCycles = 0;
			}

			cycles += intCycles;
			intCycles = 0;

			/* Reset state for next instruction */
			ResetPrefixes();
			modRm.Reset();

			return cycles;
		}
	}
}

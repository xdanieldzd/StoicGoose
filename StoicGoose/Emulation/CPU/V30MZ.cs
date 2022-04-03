using System;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ : IComponent
	{
		/* General registers */
		Register16 ax, bx, cx, dx;
		ushort sp, bp, si, di;
		/* Segment registers */
		ushort cs, ds, ss, es;
		/* Status and instruction registers */
		ushort ip;
		Flags flags;

		bool halted;

		int pendingIntVector;

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

		public bool IsHalted => halted;

		public V30MZ(MemoryReadDelegate memoryRead, MemoryWriteDelegate memoryWrite, RegisterReadDelegate registerRead, RegisterWriteDelegate registerWrite)
		{
			memoryReadDelegate = memoryRead;
			memoryWriteDelegate = memoryWrite;
			registerReadDelegate = registerRead;
			registerWriteDelegate = registerWrite;

			InitializeDisassembler();

			Reset();
		}

		public void Reset()
		{
			/* CPU reset */
			flags = 0;
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

			pendingIntVector = -1;

			ResetPrefixes();
			modRm.Reset();
		}

		public void Shutdown()
		{
			CloseTraceLogger();
		}

		public void RaiseInterrupt(int vector)
		{
			pendingIntVector = vector;
			halted = false;
		}

		private int CheckAndServiceInterrupt()
		{
			/* Interrupts enabled AND interrupt pending? */
			if (IsFlagSet(Flags.InterruptEnable) && pendingIntVector != -1)
			{
				/* Service interrupt */
				var offset = ReadMemory16(0, (ushort)((pendingIntVector * 4) + 0));
				var segment = ReadMemory16(0, (ushort)((pendingIntVector * 4) + 2));

				Push((ushort)flags);
				Push(cs);
				Push(ip);

				cs = segment;
				ip = offset;

				ResetPrefixes();
				modRm.Reset();

				ClearFlags(Flags.InterruptEnable);

				return 32;
			}

			pendingIntVector = -1;

			return 0;
		}

		public int Step()
		{
			var csBegin = cs;
			var ipBegin = ip;

			/* Do interrupt handling & service interrupt if needed */
			var intCycles = CheckAndServiceInterrupt();

			/* Is CPU halted? */
			if (halted) return 1;

			/* Read any prefixes & opcode */
			byte opcode;
			while (!HandlePrefixes(opcode = ReadMemory8(cs, ip++))) { }

			/* If enabled, write to CPU trace logger */
			if (isTraceLogOpen)
			{
				var registerStates = $"AX:{ax.Word:X4} BX:{bx.Word:X4} CX:{cx.Word:X4} DX:{dx.Word:X4} SP:{sp:X4} BP:{bp:X4} SI:{si:X4} DI:{di:X4}";
				var segmentStates = $"CS:{cs:X4} SS:{ss:X4} DS:{ds:X4} ES:{es:X4}";

				WriteToTraceLog($"{DisassembleInstruction(csBegin, ipBegin),-96} | {registerStates} | {segmentStates}");
			}

			/* Execute instruction */
			var cycles = instructions[opcode](this);
			if (cycles == 0) throw new Exception($"Cycle count for opcode 0x{opcode:X2} is zero");

			/* Reset state for next instruction */
			ResetPrefixes();
			modRm.Reset();

			return cycles + intCycles;
		}
	}
}

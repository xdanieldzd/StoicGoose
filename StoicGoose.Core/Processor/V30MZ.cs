using System.Collections.Generic;

namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		const byte PrefixSegmentOverrideDS1 = 0x26;
		const byte PrefixSegmentOverridePS = 0x2E;
		const byte PrefixSegmentOverrideSS = 0x36;
		const byte PrefixSegmentOverrideDS0 = 0x3E;
		const byte PrefixBusLock = 0xF0;
		const byte PrefixRepeatWhileNonZero = 0xF2;
		const byte PrefixRepeatWhileZero = 0xF3;

		/* General Purpose Registers */
		protected Register16 aw = default, bw = default, cw = default, dw = default;
		public Register16 AW { get => aw; set => aw = value; }
		public Register16 BW { get => bw; set => bw = value; }
		public Register16 CW { get => cw; set => cw = value; }
		public Register16 DW { get => dw; set => dw = value; }

		/* Segment Registers */
		protected ushort ds0 = default, ds1 = default, ps = default, ss = default;
		public ushort DS0 { get => ds0; set => ds0 = value; }
		public ushort DS1 { get => ds1; set => ds1 = value; }
		public ushort PS { get => ps; set => ps = value; }
		public ushort SS { get => ss; set => ss = value; }

		/* Stack Pointer */
		protected ushort sp = default;
		public ushort SP { get => sp; set => sp = value; }

		/* Base Pointer */
		protected ushort bp = default;
		public ushort BP { get => bp; set => bp = value; }

		/* Index Registers */
		protected ushort ix = default, iy = default;
		public ushort IX { get => ix; set => ix = value; }
		public ushort IY { get => iy; set => iy = value; }

		/* Program Counter */
		protected ushort pc = default;
		public ushort PC { get => pc; set => pc = value; }

		/* Prefetch Pointer */
		protected ushort pfp = default;
		public ushort PFP { get => pfp; set => pfp = value; }

		/* Program Status Word */
		protected ProgramStatusWord psw = default;
		public ProgramStatusWord PSW { get => psw; set => psw = value; }

		/* Prefixes */
		protected readonly Queue<byte> prefixes = new();
		protected bool isPrefix = false;

		/* Mode/Register/Memory byte */
		protected ModRM modRM = default;

		/* Miscellaneous variables */
		protected bool isHalted = default;
		public bool IsHalted { get => isHalted; set => isHalted = value; }

		protected int cycles = default;

		public V30MZ()
		{
			ModRM.cpu = this;

			GenerateInstructionHandlers();

			Reset();
		}

		public void Reset()
		{
			/* Reset CPU */
			ds0 = 0x0000;
			ds1 = 0x0000;
			ps = 0xFFFF;
			ss = 0x0000;
			pc = 0x0000;
			psw = 0xF002;

			prefixes.Clear();
			isPrefix = false;

			modRM = 0;

			isHalted = false;

			cycles = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void Interrupt(int vector)
		{
			Wait(32);

			isHalted = false;

			isPrefix = false;
			if (prefixes.Count > 0)
			{
				pc -= (ushort)prefixes.Count;
				prefixes.Clear();
			}

			var offset = ReadMemory16(0x0000, (ushort)((vector * 4) + 0));
			var segment = ReadMemory16(0x0000, (ushort)((vector * 4) + 2));

			PUSH(psw.Value);
			PUSH(ps);
			PUSH(pc);

			psw.InterruptEnable = false;
			psw.Break = false;

			ps = segment;
			pc = offset;
		}

		public int Step()
		{
			cycles = 0;

			if (isHalted)
			{
				Wait(1);
			}
			else
			{
				isPrefix = false;

				byte opcode;
				while (OpcodeIsPrefix(opcode = Fetch8()))
				{
					if (prefixes.Count > 15) prefixes.Dequeue();
					prefixes.Enqueue(opcode);

					isPrefix = true;

					if (opcode == PrefixRepeatWhileNonZero || opcode == PrefixRepeatWhileZero)
						Wait(4);
				}

				instructions[opcode]();

				if (!isPrefix) prefixes.Clear();
			}

			return cycles;
		}

		protected static bool OpcodeIsPrefix(byte op)
		{
			return
				op == PrefixSegmentOverrideDS1 ||
				op == PrefixSegmentOverridePS ||
				op == PrefixSegmentOverrideSS ||
				op == PrefixSegmentOverrideDS0 ||
				op == PrefixBusLock ||
				op == PrefixRepeatWhileNonZero ||
				op == PrefixRepeatWhileZero;
		}

		protected ushort SegmentViaPrefix(ushort value)
		{
			foreach (var prefix in prefixes)
			{
				if (prefix == PrefixSegmentOverrideDS1) return ds1;
				if (prefix == PrefixSegmentOverridePS) return ps;
				if (prefix == PrefixSegmentOverrideSS) return ss;
				if (prefix == PrefixSegmentOverrideDS0) return ds0;
			}
			return value;
		}

		protected byte RepeatViaPrefix()
		{
			foreach (var prefix in prefixes)
			{
				if (prefix == PrefixRepeatWhileNonZero) return prefix;
				if (prefix == PrefixRepeatWhileZero) return prefix;
			}
			return 0;
		}

		protected static bool Parity(int value)
		{
			int count = 0;
			while (value != 0) { count += value & 0x01; value >>= 1; }
			return !((count % 2) != 0);
		}

		protected void Wait(int count)
		{
			cycles += count;
		}

		protected void BranchIf(bool condition)
		{
			var offset = (sbyte)Fetch8();
			if (!condition)
			{
				Wait(1);
			}
			else
			{
				Wait(3);
				pc = (ushort)(pc + offset);
			}
		}

		protected abstract byte ReadMemory(uint address);
		protected abstract void WriteMemory(uint address, byte value);
		protected abstract byte ReadPort(ushort address);
		protected abstract void WritePort(ushort address, byte value);

		private byte Fetch8()
		{
			return ReadMemory((uint)((ps << 4) + pc++));
		}

		private ushort Fetch16()
		{
			var lo = Fetch8();
			var hi = Fetch8();
			return (ushort)(lo << 0 | hi << 8);
		}

		private byte ReadMemory8(ushort segment, ushort address)
		{
			//cycles += 1;
			return ReadMemory((uint)((segment << 4) + address));
		}

		private ushort ReadMemory16(ushort segment, ushort address)
		{
			//cycles += 1 + (address & 0b1);
			return (ushort)(
				(ReadMemory((uint)((segment << 4) + address + 1)) << 8) |
				(ReadMemory((uint)((segment << 4) + address + 0)) << 0));
		}

		private void WriteMemory8(ushort segment, ushort address, byte value)
		{
			//cycles += 1;
			WriteMemory((uint)((segment << 4) + address), value);
		}

		private void WriteMemory16(ushort segment, ushort address, ushort value)
		{
			//cycles += 1 + (address & 0b1);
			WriteMemory((uint)((segment << 4) + address + 0), (byte)((value >> 0) & 0xFF));
			WriteMemory((uint)((segment << 4) + address + 1), (byte)((value >> 8) & 0xFF));
		}

		private byte ReadPort8(ushort port)
		{
			//cycles += 1;
			return ReadPort(port);
		}

		private ushort ReadPort16(ushort port)
		{
			//cycles += 1 + (port & 0b1);
			return (ushort)(
				(ReadPort((ushort)(port + 1)) << 8) |
				(ReadPort((ushort)(port + 0)) << 0));
		}

		private void WritePort8(ushort port, byte value)
		{
			//cycles += 1;
			WritePort(port, value);
		}

		private void WritePort16(ushort port, ushort value)
		{
			//cycles += 1 + (port & 0b1);
			WritePort((ushort)(port + 0), (byte)((value >> 0) & 0xFF));
			WritePort((ushort)(port + 1), (byte)((value >> 8) & 0xFF));
		}
	}
}

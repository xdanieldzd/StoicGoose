using System.Collections.Generic;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ : IComponent
	{
		const byte PrefixSegmentOverrideDS1 = 0x26;
		const byte PrefixSegmentOverridePS = 0x2E;
		const byte PrefixSegmentOverrideSS = 0x36;
		const byte PrefixSegmentOverrideDS0 = 0x3E;
		const byte PrefixBusLock = 0xF0;
		const byte PrefixRepeatWhileNonZero = 0xF2;
		const byte PrefixRepeatWhileZero = 0xF3;

		/* Parent machine instance */
		readonly IMachine machine = default;

		/* General Purpose Registers */
		Register16 aw = default, bw = default, cw = default, dw = default;
		public Register16 AW { get => aw; set => aw = value; }
		public Register16 BW { get => bw; set => bw = value; }
		public Register16 CW { get => cw; set => cw = value; }
		public Register16 DW { get => dw; set => dw = value; }

		/* Segment Registers */
		ushort ds0 = default, ds1 = default, ps = default, ss = default;
		public ushort DS0 { get => ds0; set => ds0 = value; }
		public ushort DS1 { get => ds1; set => ds1 = value; }
		public ushort PS { get => ps; set => ps = value; }
		public ushort SS { get => ss; set => ss = value; }

		/* Stack Pointer */
		ushort sp = default;
		public ushort SP { get => sp; set => sp = value; }

		/* Base Pointer */
		ushort bp = default;
		public ushort BP { get => bp; set => bp = value; }

		/* Index Registers */
		ushort ix = default, iy = default;
		public ushort IX { get => ix; set => ix = value; }
		public ushort IY { get => iy; set => iy = value; }

		/* Program Counter */
		ushort pc = default;
		public ushort PC { get => pc; set => pc = value; }

		/* Prefetch Pointer */
		ushort pfp = default;
		public ushort PFP { get => pfp; set => pfp = value; }

		/* Program Status Word */
		ProgramStatusWord psw = default;
		public ProgramStatusWord PSW { get => psw; set => psw = value; }

		/* Prefetch queue */
		readonly Queue<byte> pf = new();

		/* Prefixes */
		readonly Queue<byte> prefixes = new();
		bool isPrefix = false;

		/* Mode/Register/Memory byte */
		ModRM modRM = default;

		/* Miscellaneous variables */
		bool isHalted = default;
		public bool IsHalted { get => isHalted; set => isHalted = value; }

		byte opcode = default;
		int cycles = default;

		public V30MZ(IMachine machine)
		{
			this.machine = machine;

			ModRM.cpu = this;

			GenerateInstructionHandlers();

			Reset();
		}

		public void Reset()
		{
			/* Reset CPU */
			aw = Random.GetUInt16();
			bw = Random.GetUInt16();
			cw = Random.GetUInt16();
			dw = Random.GetUInt16();
			ds0 = 0x0000;
			ds1 = 0x0000;
			ps = 0xFFFF;
			ss = 0x0000;
			sp = Random.GetUInt16();
			bp = Random.GetUInt16();
			ix = Random.GetUInt16();
			iy = Random.GetUInt16();
			pc = 0x0000;
			pfp = 0x0000;
			psw = 0x0000;

			pf.Clear();

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

			//Push((ushort)flags);
			//Push(cs);
			//Push(ip);

			psw.InterruptEnable = false;
			psw.Break = false;

			ps = segment;
			pc = offset;

			Flush();
		}

		public int Step()
		{
			cycles = 0;

			isPrefix = false;

			if (isHalted) Wait(1);
			else instructions[opcode = Fetch8()]();

			if (!isPrefix) prefixes.Clear();

			return cycles;
		}

		private void EnqueuePrefix()
		{
			if (prefixes.Count >= 16) prefixes.Dequeue();
			prefixes.Enqueue(opcode);
			isPrefix = true;
		}

		private ushort SegmentViaPrefix(ushort value)
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

		private byte RepeatViaPrefix()
		{
			foreach (var prefix in prefixes)
			{
				if (prefix == PrefixRepeatWhileNonZero) return prefix;
				if (prefix == PrefixRepeatWhileZero) return prefix;
			}
			return 0;
		}

		private static bool Parity(int value)
		{
			int count = 0;
			while (value != 0) { count += value & 0x01; value >>= 1; }
			return !((count % 2) != 0);
		}

		private void Wait(int count)
		{
			while (count-- != 0) Prefetch();
		}

		private void Prefetch()
		{
			var isUnaligned = (pfp & 0b1) == 0b1;
			if (pf.Count < 16) pf.Enqueue(ReadMemory8(ps, pfp++));
			if (pf.Count < 16 && isUnaligned) pf.Enqueue(ReadMemory8(ps, pfp++));
			cycles++;
		}

		private void Flush()
		{
			pf.Clear();
			pfp = pc;
			if ((pfp & 0b1) == 0b1) cycles++;
		}

		private byte Fetch8()
		{
			pc++;
			if (pf.Count == 0) Prefetch();
			return pf.Dequeue();
		}

		private ushort Fetch16()
		{
			return (ushort)(Fetch8() | Fetch8() << 8);
		}

		private byte ReadMemory8(ushort segment, ushort address)
		{
			cycles += 1;
			return machine.ReadMemory((uint)((segment << 4) + address));
		}

		private ushort ReadMemory16(ushort segment, ushort address)
		{
			cycles += 1 + (address & 0b1);
			return (ushort)((machine.ReadMemory((uint)((segment << 4) + address + 1)) << 8) | machine.ReadMemory((uint)((segment << 4) + address)));
		}

		private void WriteMemory8(ushort segment, ushort address, byte value)
		{
			cycles += 1;
			machine.WriteMemory((uint)((segment << 4) + address), value);
		}

		private void WriteMemory16(ushort segment, ushort address, ushort value)
		{
			cycles += 1 + (address & 0b1);
			machine.WriteMemory((uint)((segment << 4) + address), (byte)(value & 0xFF));
			machine.WriteMemory((uint)((segment << 4) + address + 1), (byte)(value >> 8));
		}

		private byte ReadPort8(ushort port)
		{
			cycles += 1;
			return machine.ReadPort(port);
		}

		private ushort ReadPort16(ushort port)
		{
			cycles += 1 + (port & 0b1);
			return (ushort)(machine.ReadPort((ushort)(port + 1)) << 8 | machine.ReadPort(port));
		}

		private void WritePort8(ushort port, byte value)
		{
			cycles += 1;
			machine.WritePort(port, value);
		}

		private void WritePort16(ushort port, ushort value)
		{
			cycles += 1 + (port & 0b1);
			machine.WritePort(port, (byte)(value & 0xFF));
			machine.WritePort((ushort)(port + 1), (byte)(value >> 8));
		}
	}
}

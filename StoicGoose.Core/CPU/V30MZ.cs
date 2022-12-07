using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ : IComponent
	{
		/* Parent machine instance */
		readonly IMachine machine = default;

		/* General Purpose Registers */
		Register16 aw, bw, cw, dw;
		public Register16 AW { get => aw; set => aw = value; }
		public Register16 BW { get => bw; set => bw = value; }
		public Register16 CW { get => cw; set => cw = value; }
		public Register16 DW { get => dw; set => dw = value; }

		/* Segment Registers */
		ushort ds0, ds1, ps, ss;
		public ushort DS0 { get => ds0; set => ds0 = value; }
		public ushort DS1 { get => ds1; set => ds1 = value; }
		public ushort PS { get => ps; set => ps = value; }
		public ushort SS { get => ss; set => ss = value; }

		/* Stack Pointer */
		ushort sp;
		public ushort SP { get => sp; set => sp = value; }

		/* Base Pointer */
		ushort bp;
		public ushort BP { get => bp; set => bp = value; }

		/* Index Registers */
		ushort ix, iy;
		public ushort IX { get => ix; set => ix = value; }
		public ushort IY { get => iy; set => iy = value; }

		/* Program Counter */
		ushort pc;
		public ushort PC { get => pc; set => pc = value; }

		/* Prefetch Pointer */
		ushort pfp;
		public ushort PFP { get => pfp; set => pfp = value; }

		/* Program Status Word */
		ProgramStatusWord psw;
		public ProgramStatusWord PSW { get => psw; set => psw = value; }

		/* Miscellaneous variables */
		bool isHalted;
		public bool IsHalted { get => isHalted; set => isHalted = value; }

		public V30MZ(IMachine machine)
		{
			this.machine = machine;

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

			isHalted = false;
		}

		public void Shutdown()
		{
			//
		}

		public void Interrupt(int vector)
		{
			//
		}

		public int Step()
		{
			return 0;
		}
	}
}

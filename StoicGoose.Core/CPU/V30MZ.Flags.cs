using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		[Flags]
		public enum Flags : ushort
		{
			Carry = 1 << 0,            /* CF */
			ReservedB1 = 1 << 1,       /* (reserved) */
			Parity = 1 << 2,           /* PF */
			ReservedB3 = 1 << 3,       /* (reserved) */
			Auxiliary = 1 << 4,        /* AF */
			ReservedB5 = 1 << 5,       /* (reserved) */
			Zero = 1 << 6,             /* ZF */
			Sign = 1 << 7,             /* SF */
			Trap = 1 << 8,             /* TF */
			InterruptEnable = 1 << 9,  /* IF */
			Direction = 1 << 10,       /* DF */
			Overflow = 1 << 11,        /* OF */
			ReservedB12 = 1 << 12,     /* (reserved) */
			ReservedB13 = 1 << 13,     /* (reserved) */
			ReservedB14 = 1 << 14,     /* (reserved) */
			ReservedB15 = 1 << 15      /* (reserved) */
		}

		private void SetFlags(Flags flags)
		{
			this.flags |= flags;
		}

		private void ClearFlags(Flags flags)
		{
			this.flags &= ~flags;
		}

		public bool IsFlagSet(Flags flags)
		{
			return (this.flags & flags) == flags;
		}

		private void SetClearFlagConditional(Flags flags, bool condition)
		{
			if (condition) this.flags |= flags;
			else this.flags &= ~flags;
		}
	}
}

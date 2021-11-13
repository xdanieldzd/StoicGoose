﻿namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		private int JumpConditional(bool condition)
		{
			if (condition) { ip = ReadOpcodeJb(); return 4; }
			else { ip++; return 1; }
		}

		private bool CalculateParity(int result)
		{
			int bitsSet = 0;
			while (result != 0) { bitsSet += result & 0x01; result >>= 1; }
			return bitsSet == 0 || (bitsSet % 2) == 0;
		}
	}
}

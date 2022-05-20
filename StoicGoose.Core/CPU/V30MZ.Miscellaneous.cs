namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		private void Push(ushort value)
		{
			sp -= 2;
			WriteMemory16(ss, sp, value);
		}

		private ushort Pop()
		{
			var value = ReadMemory16(ss, sp);
			sp += 2;
			return value;
		}

		private int Loop()
		{
			if (--cx.Word != 0) { ip = ReadOpcodeJb(); return 4; }
			else { ip++; return 1; }
		}

		private int LoopWhile(bool condition)
		{
			if (--cx.Word != 0 && condition) { ip = ReadOpcodeJb(); return 5; }
			else { ip++; return 2; }
		}

		private int JumpConditional(bool condition)
		{
			if (condition) { ip = ReadOpcodeJb(); return 4; }
			else { ip++; return 1; }
		}

		private static bool CalculateParity(int result)
		{
			int bitsSet = 0;
			while (result != 0) { bitsSet += result & 0x01; result >>= 1; }
			return bitsSet == 0 || (bitsSet % 2) == 0;
		}

		private static void Exchange(ref ushort a, ref ushort b)
		{
			(b, a) = (a, b);
		}
	}
}

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		internal void CHKIND()
		{
			var lo = GetMemory16(0);
			var hi = GetMemory16(2);
			var reg = GetRegister16();
			if (reg < lo || reg > hi) Interrupt(5);
		}

		internal void DISPOSE()
		{
			Wait(1);
			sp = bp;
			bp = POP();
		}

		internal void PREPARE()
		{
			Wait(7);
			var offset = Fetch16();
			var length = (byte)(Fetch8() & 0x1F);

			PUSH(bp);
			bp = sp;
			sp -= offset;

			if (length != 0)
			{
				Wait(length > 1 ? 7 : 6);
				for (var i = 1; i < length; i++)
				{
					Wait(4);
					PUSH(ReadMemory16(SegmentViaPrefix(ss), (ushort)(bp - i * 2)));
				}
				PUSH(bp);
			}
		}
	}
}

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		//CMPBK
		//CMPM

		internal void INM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadPort8(dw);
				WriteMemory8(ds1, iy, value);
				iy += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		internal void INM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadPort16(dw);
				WriteMemory16(ds1, iy, value);
				iy += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		//LDM

		internal void MOVBK8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadMemory8(SegmentViaPrefix(ds0), ix);
				WriteMemory8(ds1, iy, value);
				ix += (ushort)(psw.Direction ? -1 : 1);
				iy += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		internal void MOVBK16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadMemory16(SegmentViaPrefix(ds0), ix);
				WriteMemory16(ds1, iy, value);
				ix += (ushort)(psw.Direction ? -2 : 2);
				iy += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		internal void OUTM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadMemory8(SegmentViaPrefix(ds0), ix);
				WritePort8(dw, value);
				ix += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		internal void OUTM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				var value = ReadMemory16(SegmentViaPrefix(ds0), ix);
				WritePort16(dw, value);
				ix += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		//STM
	}
}

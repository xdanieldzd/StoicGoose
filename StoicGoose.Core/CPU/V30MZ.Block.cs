namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		internal void CMPBK8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				var x = ReadMemory8(SegmentViaPrefix(ds0), ix);
				var y = ReadMemory8(ds1, iy);
				ix += (ushort)(psw.Direction ? -1 : 1);
				iy += (ushort)(psw.Direction ? -1 : 1);
				SUB(x, y);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void CMPBK16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				var x = ReadMemory16(SegmentViaPrefix(ds0), ix);
				var y = ReadMemory16(ds1, iy);
				ix += (ushort)(psw.Direction ? -2 : 2);
				iy += (ushort)(psw.Direction ? -2 : 2);
				SUB(x, y);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void CMPM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				var x = aw.Low;
				var y = ReadMemory8(ds1, iy);
				iy += (ushort)(psw.Direction ? -1 : 1);
				SUB(x, y);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void CMPM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				var x = aw.Word;
				var y = ReadMemory16(ds1, iy);
				iy += (ushort)(psw.Direction ? -2 : 2);
				SUB(x, y);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void INM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory8(ds1, iy, ReadPort8(dw));
				iy += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void INM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory16(ds1, iy, ReadPort16(dw));
				iy += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void LDM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				aw.Low = ReadMemory8(SegmentViaPrefix(ds0), ix);
				ix += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void LDM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				aw.Word = ReadMemory16(SegmentViaPrefix(ds0), ix);
				ix += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void MOVBK8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory8(ds1, iy, ReadMemory8(SegmentViaPrefix(ds0), ix));
				ix += (ushort)(psw.Direction ? -1 : 1);
				iy += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void MOVBK16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory16(ds1, iy, ReadMemory16(SegmentViaPrefix(ds0), ix));
				ix += (ushort)(psw.Direction ? -2 : 2);
				iy += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void OUTM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WritePort8(dw, ReadMemory8(SegmentViaPrefix(ds0), ix));
				ix += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void OUTM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WritePort16(dw, ReadMemory16(SegmentViaPrefix(ds0), ix));
				ix += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void STM8()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory8(ds1, iy, aw.Low);
				iy += (ushort)(psw.Direction ? -1 : 1);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}

		internal void STM16()
		{
			if (cw.Word != 0 || RepeatViaPrefix() == 0)
			{
				WriteMemory16(ds1, iy, aw.Word);
				iy += (ushort)(psw.Direction ? -2 : 2);

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				isPrefix = true;
				pc--;
			}
		}
	}
}

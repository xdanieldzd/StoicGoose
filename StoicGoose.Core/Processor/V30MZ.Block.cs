using System;

namespace StoicGoose.Core.Processor
{
	public abstract partial class V30MZ
	{
		internal void CMPBK8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				var x = ReadMemory8(SegmentViaPrefix(ds0), ix);
				var y = ReadMemory8(ds1, iy);
				ix += (UInt16)(psw.Direction ? -1 : 1);
				iy += (UInt16)(psw.Direction ? -1 : 1);
				SUB(x, y);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void CMPM8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				var x = aw.Low;
				var y = ReadMemory8(ds1, iy);
				iy += (UInt16)(psw.Direction ? -1 : 1);
				SUB(x, y);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void INM8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory8(ds1, iy, ReadPort8(dw));
				iy += (UInt16)(psw.Direction ? -1 : 1);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void LDM8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				aw.Low = ReadMemory8(SegmentViaPrefix(ds0), ix);
				ix += (UInt16)(psw.Direction ? -1 : 1);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void MOVBK8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory8(ds1, iy, ReadMemory8(SegmentViaPrefix(ds0), ix));
				ix += (UInt16)(psw.Direction ? -1 : 1);
				iy += (UInt16)(psw.Direction ? -1 : 1);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void OUTM8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WritePort8(dw, ReadMemory8(SegmentViaPrefix(ds0), ix));
				ix += (UInt16)(psw.Direction ? -1 : 1);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void STM8()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory8(ds1, iy, aw.Low);
				iy += (UInt16)(psw.Direction ? -1 : 1);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void CMPBK16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				var x = ReadMemory16(SegmentViaPrefix(ds0), ix);
				var y = ReadMemory16(ds1, iy);
				ix += (UInt16)(psw.Direction ? -2 : 2);
				iy += (UInt16)(psw.Direction ? -2 : 2);
				SUB(x, y);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void CMPM16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				var x = aw.Word;
				var y = ReadMemory16(ds1, iy);
				iy += (UInt16)(psw.Direction ? -2 : 2);
				SUB(x, y);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileNonZero && psw.Zero) return;
				if (RepeatViaPrefix() == PrefixRepeatWhileZero && !psw.Zero) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void INM16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory16(ds1, iy, ReadPort16(dw));
				iy += (UInt16)(psw.Direction ? -2 : 2);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void LDM16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				aw.Word = ReadMemory16(SegmentViaPrefix(ds0), ix);
				ix += (UInt16)(psw.Direction ? -2 : 2);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void MOVBK16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory16(ds1, iy, ReadMemory16(SegmentViaPrefix(ds0), ix));
				ix += (UInt16)(psw.Direction ? -2 : 2);
				iy += (UInt16)(psw.Direction ? -2 : 2);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void OUTM16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WritePort16(dw, ReadMemory16(SegmentViaPrefix(ds0), ix));
				ix += (UInt16)(psw.Direction ? -2 : 2);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
		internal void STM16()
		{
			if (RepeatViaPrefix() == 0 || cw.Word != 0)
			{
				WriteMemory16(ds1, iy, aw.Word);
				iy += (UInt16)(psw.Direction ? -2 : 2);

				if (RepeatViaPrefix() == 0 || --cw.Word == 0) return;

				isPrefix = true;
				pc--;
			}
		}
	}
}

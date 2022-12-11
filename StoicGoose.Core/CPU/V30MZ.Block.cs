using System;

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		// TODO: rethink 8/16bit size selection stuffs? auto-gen with template instead?


		//CMPBK
		//CMPM

		internal void INM<T>()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				if (typeof(T) == typeof(byte))
				{
					var value = ReadPort8(dw);
					WriteMemory8(ds1, iy, value);
					iy += (ushort)(psw.Direction ? -1 : 1);
				}
				else if (typeof(T) == typeof(ushort))
				{
					var value = ReadPort16(dw);
					WriteMemory16(ds1, iy, value);
					iy += (ushort)(psw.Direction ? -2 : 2);
				}
				else
					throw new ArgumentException();

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		//LDM
		//MOVBK

		internal void OUTM<T>()
		{
			if (cw.Word != 0 || RepeatViaPrefix() != 0)
			{
				if (typeof(T) == typeof(byte))
				{
					var value = ReadMemory8(SegmentViaPrefix(ds0), ix);
					WritePort8(dw, value);
					ix += (ushort)(psw.Direction ? -1 : 1);
				}
				else if (typeof(T) == typeof(ushort))
				{
					var value = ReadMemory16(SegmentViaPrefix(ds0), ix);
					WritePort16(dw, value);
					ix += (ushort)(psw.Direction ? -1 : 1);
				}
				else
					throw new ArgumentException();

				if (--cw.Word == 0 || RepeatViaPrefix() == 0) return;

				pc--;
				Loop();
			}
		}

		//STM
	}
}

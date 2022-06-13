namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		private static int GetIncrement(bool is16Bit, bool isDirectionFlagSet)
		{
			return isDirectionFlagSet ? (is16Bit ? -2 : -1) : (is16Bit ? 2 : 1);
		}

		private void InString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			if (!is16Bit)
				WriteMemory8(es, di, machine.ReadPort(dx.Word));
			else
				WriteMemory16(es, di, ReadPort16(dx.Word));

			di = (ushort)(di + increment);
		}

		private void OutString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			var temp = GetSegmentViaOverride(SegmentNumber.DS);

			if (!is16Bit)
				machine.WritePort(dx.Word, ReadMemory8(temp, si));
			else
				WritePort16(dx.Word, ReadMemory16(temp, si));

			si = (ushort)(si + increment);
		}

		private void MoveString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			var temp = GetSegmentViaOverride(SegmentNumber.DS);

			if (!is16Bit)
				WriteMemory8(es, di, ReadMemory8(temp, si));
			else
				WriteMemory16(es, di, ReadMemory16(temp, si));

			di = (ushort)(di + increment);
			si = (ushort)(si + increment);
		}

		private void CompareString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			var temp = GetSegmentViaOverride(SegmentNumber.DS);

			if (!is16Bit)
				Sub8(false, ReadMemory8(temp, si), ReadMemory8(es, di));
			else
				Sub16(false, ReadMemory16(temp, si), ReadMemory16(es, di));

			di = (ushort)(di + increment);
			si = (ushort)(si + increment);
		}

		private void StoreString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			if (!is16Bit)
				WriteMemory8(es, di, ax.Low);
			else
				WriteMemory16(es, di, ax.Word);

			di = (ushort)(di + increment);
		}

		private void LoadString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			var temp = GetSegmentViaOverride(SegmentNumber.DS);

			if (!is16Bit)
				ax.Low = ReadMemory8(temp, si);
			else
				ax.Word = ReadMemory16(temp, si);

			si = (ushort)(si + increment);
		}

		private void ScanString(bool is16Bit)
		{
			var increment = GetIncrement(is16Bit, IsFlagSet(Flags.Direction));

			if (!is16Bit)
				Sub8(false, ax.Low, ReadMemory8(es, di));
			else
				Sub16(false, ax.Word, ReadMemory16(es, di));

			di = (ushort)(di + increment);
		}
	}
}

namespace StoicGoose.Core.CPU
{
	public sealed partial class V30MZ
	{
		SegmentNumber prefixSegOverride;
		bool prefixHasRepeat;
		bool prefixRepeatOnNotEqual;

		private void ResetPrefixes()
		{
			prefixSegOverride = SegmentNumber.Unset;
			prefixHasRepeat = false;
			prefixRepeatOnNotEqual = false;
		}

		private bool HandlePrefixes(byte op)
		{
			var isOpcode = true;

			switch (op)
			{
				/* Prefixes */
				case 0x26:
					/* :ES */
					prefixSegOverride = SegmentNumber.ES;
					isOpcode = false;
					break;

				case 0x2E:
					/* :CS */
					prefixSegOverride = SegmentNumber.CS;
					isOpcode = false;
					break;

				case 0x36:
					/* :SS */
					prefixSegOverride = SegmentNumber.SS;
					isOpcode = false;
					break;

				case 0x3E:
					/* :DS */
					prefixSegOverride = SegmentNumber.DS;
					isOpcode = false;
					break;

				case 0xF0:
					/* LOCK */
					//TODO: implement??
					isOpcode = false;
					break;

				case 0xF2:
					/* REPNE */
					prefixHasRepeat = true;
					prefixRepeatOnNotEqual = true;
					isOpcode = false;
					break;

				case 0xF3:
					/* REP/REPE */
					prefixHasRepeat = true;
					prefixRepeatOnNotEqual = false;
					isOpcode = false;
					break;
			}

			return isOpcode;
		}
	}
}

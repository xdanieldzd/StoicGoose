namespace StoicGoose.Core.Sound
{
	/* HyperVoice channel */
	public sealed class SoundChannelHyperVoice
	{
		public byte OutputLeft { get; set; }
		public byte OutputRight { get; set; }

		/* REG_HYPER_CTRL */
		public bool IsEnabled { get; set; }
		public int ScalingMode { get; set; }
		public int Volume { get; set; }
		public byte CtrlUnknown { get; set; }

		/* REG_HYPER_CHAN_CTRL */
		public bool RightEnable { get; set; }
		public bool LeftEnable { get; set; }
		public byte ChanCtrlUnknown { get; set; }

		/* REG_SND_HYPERVOICE */
		public byte Data { get; set; }

		public SoundChannelHyperVoice() { }

		public void Reset()
		{
			OutputLeft = OutputRight = 0;

			IsEnabled = false;
			ScalingMode = Volume = 0;
			CtrlUnknown = 0;

			RightEnable = LeftEnable = false;
			ChanCtrlUnknown = 0;

			Data = 0;
		}

		public void Step()
		{
			var output = (byte)0;

			switch (ScalingMode)
			{
				case 0: output = (byte)(Data << 3 - Volume); break;
				case 1: output = (byte)((Data << 3 - Volume) | (-0x100 << 3 - Volume)); break;
				case 2: output = (byte)(Data << 3 - Volume); break;    // ???
				case 3: output = (byte)(Data << 3); break;
			}

			OutputLeft = LeftEnable ? output : (byte)0;
			OutputRight = RightEnable ? output : (byte)0;
		}
	}
}

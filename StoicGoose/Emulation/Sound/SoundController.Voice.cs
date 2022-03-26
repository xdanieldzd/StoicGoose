namespace StoicGoose.Emulation.Sound
{
	public partial class SoundController
	{
		public sealed class Voice : Wave
		{
			/* REG_SND_VOICE_CTRL */
			public bool PcmRightFull { get; set; }
			public bool PcmRightHalf { get; set; }
			public bool PcmLeftFull { get; set; }
			public bool PcmLeftHalf { get; set; }

			bool isVoice => Mode;

			public Voice(WaveTableReadDelegate waveTableRead) : base(false, waveTableRead) { }

			public sealed override void Reset()
			{
				base.Reset();

				PcmRightFull = PcmRightHalf = PcmLeftFull = PcmLeftHalf = false;
			}

			public sealed override void Step()
			{
				if (isVoice)
				{
					var pcm = (ushort)(VolumeLeft << 4 | VolumeRight);
					OutputLeft = (byte)(PcmLeftFull ? pcm : (PcmLeftHalf ? pcm >> 1 : 0));
					OutputRight = (byte)(PcmRightFull ? pcm : (PcmRightHalf ? pcm >> 1 : 0));
				}
				else
				{
					counter--;
					if (counter == Pitch)
					{
						var data = waveTableReadDelegate((ushort)(pointer >> 1));
						if ((pointer & 0b1) == 0b1) data >>= 4;
						data &= 0x0F;

						OutputLeft = (byte)(data * VolumeLeft);
						OutputRight = (byte)(data * VolumeRight);

						pointer++;
						pointer &= 0b11111;
						counter = counterReload;
					}
				}
			}
		}
	}
}

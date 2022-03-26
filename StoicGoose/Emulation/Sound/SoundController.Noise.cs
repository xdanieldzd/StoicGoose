namespace StoicGoose.Emulation.Sound
{
	public partial class SoundController
	{
		public sealed class Noise : Wave
		{
			readonly static byte[] noiseLfsrTaps = { 14, 10, 13, 4, 8, 6, 9, 11 };

			/* REG_SND_NOISE */
			public byte NoiseMode { get; set; }
			public bool NoiseReset { get; set; }
			public bool NoiseEnable { get; set; }
			/* REG_SND_RANDOM */
			public ushort NoiseLfsr { get; set; }

			bool isNoise => Mode;

			public Noise(WaveTableReadDelegate waveTableRead) : base(false, waveTableRead) { }

			public sealed override void Reset()
			{
				base.Reset();

				NoiseMode = 0;
				NoiseReset = NoiseEnable = false;
				NoiseLfsr = 0;
			}

			public sealed override void Step()
			{
				counter--;
				if (counter == Pitch)
				{
					if (NoiseEnable)
					{
						var tap = noiseLfsrTaps[NoiseMode];
						var noise = (1 ^ (NoiseLfsr >> 7) ^ (NoiseLfsr >> tap)) & 0b1;
						NoiseLfsr = (ushort)(((NoiseLfsr << 1) | noise) & 0x7FFF);
					}

					var data = isNoise ? ((NoiseLfsr & 0b1) * 0x0F) : waveTableReadDelegate((ushort)(pointer >> 1));
					if (!isNoise)
					{
						if ((pointer & 0b1) == 0b1) data >>= 4;
						data &= 0x0F;
					}

					OutputLeft = (byte)(data * VolumeLeft);
					OutputRight = (byte)(data * VolumeRight);

					if (NoiseReset)
					{
						NoiseLfsr = 0;
						OutputLeft = OutputRight = 0;
						NoiseReset = false;
					}

					pointer++;
					pointer &= 0b11111;
					counter = counterReload;
				}
			}
		}
	}
}

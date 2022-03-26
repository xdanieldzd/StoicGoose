namespace StoicGoose.Emulation.Sound
{
	public partial class SoundController
	{
		public class Wave : ISoundChannel
		{
			protected const int counterReload = 2048;
			const int sweepReload = 32 * 256;

			protected ushort counter;
			protected byte pointer;

			public byte OutputLeft { get; set; }
			public byte OutputRight { get; set; }

			readonly protected bool channelSupportsSweep;
			protected int sweepCounter;
			int sweepCycles;

			protected WaveTableReadDelegate waveTableReadDelegate;

			/* REG_SND_CHx_PITCH */
			public ushort Pitch { get; set; }
			/* REG_SND_CHx_VOL */
			public byte VolumeLeft { get; set; }
			public byte VolumeRight { get; set; }
			/* REG_SND_SWEEP_VALUE */
			public sbyte SweepValue { get; set; }
			/* REG_SND_SWEEP_TIME */
			public byte SweepTime { get; set; }
			/* REG_SND_CTRL */
			public bool Enable { get; set; }
			public bool Mode { get; set; }

			bool isSweep => Mode;

			public Wave(bool hasSweep, WaveTableReadDelegate waveTableRead)
			{
				channelSupportsSweep = hasSweep;
				waveTableReadDelegate = waveTableRead;
			}

			public virtual void Reset()
			{
				counter = counterReload;
				pointer = 0;
				OutputLeft = OutputRight = 0;

				sweepCounter = 0;
				sweepCycles = sweepReload;

				Pitch = 0;
				VolumeLeft = VolumeRight = 0;
				SweepValue = 0;
				SweepTime = 0;
				Enable = Mode = false;
			}

			public virtual void Step()
			{
				if (isSweep)
				{
					sweepCycles--;
					if (sweepCycles == 0)
					{
						sweepCounter--;
						if (sweepCounter <= 0)
						{
							sweepCounter = SweepTime;
							Pitch = (ushort)(Pitch + SweepValue);
						}
					}
					sweepCycles = sweepReload;
				}

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

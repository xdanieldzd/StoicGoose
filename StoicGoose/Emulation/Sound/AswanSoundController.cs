using System;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Sound
{
	public class AswanSoundController : SoundControllerCommon
	{
		public override int MaxMasterVolume => 2;

		/* REG_SND_9697 */
		protected ushort unknown9697;
		/* REG_SND_9899 */
		protected ushort unknown9899;

		public AswanSoundController(MemoryReadDelegate memoryRead, int rate, int outChannels) : base(memoryRead, rate, outChannels) { }

		public override void ResetRegisters()
		{
			base.ResetRegisters();

			unknown9697 = 0;
			unknown9899 = 0;
		}

		public override void StepChannels()
		{
			for (var j = 0; j < numChannels; j++)
				channels[j].Step();
		}

		public override void GenerateSample()
		{
			var mixedLeft = 0;
			if (channels[0].Enable) mixedLeft += channels[0].OutputLeft;
			if (channels[1].Enable) mixedLeft += channels[1].OutputLeft;
			if (channels[2].Enable) mixedLeft += channels[2].OutputLeft;
			if (channels[3].Enable) mixedLeft += channels[3].OutputLeft;
			mixedLeft = (mixedLeft & 0x07FF) << 5;

			var mixedRight = 0;
			if (channels[0].Enable) mixedRight += channels[0].OutputRight;
			if (channels[1].Enable) mixedRight += channels[1].OutputRight;
			if (channels[2].Enable) mixedRight += channels[2].OutputRight;
			if (channels[3].Enable) mixedRight += channels[3].OutputRight;
			mixedRight = (mixedRight & 0x07FF) << 5;

			if (HeadphonesConnected && !headphoneEnable && !speakerEnable)
				/* Headphones connected but neither headphones nor speaker enabled? Don't output sound */
				mixedLeft = mixedRight = 0;
			else if (!HeadphonesConnected)
				/* Otherwise, no headphones connected? Mix down to mono */
				mixedLeft = mixedRight = (mixedLeft + mixedRight) / 2;

			mixedSampleBuffer.Add((short)(mixedLeft * (MasterVolume / 2.0)));
			mixedSampleBuffer.Add((short)(mixedRight * (MasterVolume / 2.0)));
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x80:
				case 0x81:
				case 0x82:
				case 0x83:
				case 0x84:
				case 0x85:
				case 0x86:
				case 0x87:
					/* REG_SND_CHx_PITCH */
					retVal |= (byte)(channels[(register >> 1) & 0b11].Pitch >> ((register & 0b1) * 8));
					break;

				case 0x88:
				case 0x89:
				case 0x8A:
				case 0x8B:
					/* REG_SND_CHx_VOL */
					retVal |= (byte)(channels[register & 0b1].VolumeLeft << 4 | channels[register & 0b1].VolumeRight);
					break;

				case 0x8C:
					/* REG_SND_SWEEP_VALUE */
					retVal |= (byte)(channels[2] as Wave).SweepValue;
					break;

				case 0x8D:
					/* REG_SND_SWEEP_TIME */
					retVal |= (byte)((channels[2] as Wave).SweepTime & 0b11111);
					break;

				case 0x8E:
					/* REG_SND_NOISE */
					retVal |= (byte)((channels[3] as Noise).NoiseMode & 0b111);
					// Noise reset (bit 3) always reads 0
					ChangeBit(ref retVal, 4, (channels[3] as Noise).NoiseEnable);
					break;

				case 0x8F:
					/* REG_SND_WAVE_BASE */
					retVal |= waveTableBase;
					break;

				case 0x90:
					/* REG_SND_CTRL */
					for (var i = 0; i < numChannels; i++)
					{
						ChangeBit(ref retVal, i, channels[i].Enable);
						if (i != 0) ChangeBit(ref retVal, i + 4, channels[i].Mode);
					}
					break;

				case 0x91:
					/* REG_SND_OUTPUT */
					ChangeBit(ref retVal, 0, speakerEnable);
					ChangeBit(ref retVal, 3, headphoneEnable);
					ChangeBit(ref retVal, 7, HeadphonesConnected);
					retVal |= (byte)((speakerVolumeShift & 0b11) << 1);
					break;

				case 0x92:
				case 0x93:
					/* REG_SND_RANDOM */
					//TODO verify
					retVal |= (byte)(((channels[3] as Noise).NoiseLfsr >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0x94:
					/* REG_SND_VOICE_CTRL */
					ChangeBit(ref retVal, 0, (channels[1] as Voice).PcmRightFull);
					ChangeBit(ref retVal, 1, (channels[1] as Voice).PcmRightHalf);
					ChangeBit(ref retVal, 2, (channels[1] as Voice).PcmLeftFull);
					ChangeBit(ref retVal, 3, (channels[1] as Voice).PcmLeftHalf);
					break;

				case 0x96:
					/* REG_SND_9697 (low) */
					retVal |= (byte)((unknown9697 >> 0) & 0b11111111);
					break;
				case 0x97:
					/* REG_SND_9697 (high) */
					retVal |= (byte)((unknown9697 >> 8) & 0b00000011);
					break;

				case 0x98:
					/* REG_SND_9899 (low) */
					retVal |= (byte)((unknown9899 >> 0) & 0b11111111);
					break;
				case 0x99:
					/* REG_SND_9899 (high) */
					retVal |= (byte)((unknown9899 >> 8) & 0b00000011);
					break;

				case 0x9A:
					/* REG_SND_9A */
					retVal |= 0b111;
					break;

				case 0x9B:
					/* REG_SND_9B */
					retVal |= 0b11111110;
					break;

				case 0x9C:
					/* REG_SND_9C */
					retVal |= 0b11111111;
					break;

				case 0x9D:
					/* REG_SND_9D */
					retVal |= 0b11111111;
					break;

				case 0x9E:
					/* REG_SND_9E */
					retVal |= (byte)(unknown9E & 0b11);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register read {register:X2}");
			}

			return retVal;
		}

		public override void WriteRegister(ushort register, byte value)
		{
			switch (register)
			{
				case 0x80:
				case 0x81:
				case 0x82:
				case 0x83:
				case 0x84:
				case 0x85:
				case 0x86:
				case 0x87:
					/* REG_SND_CHx_PITCH */
					var channel = (register >> 1) & 0b11;
					var mask = (ushort)((register & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channels[channel].Pitch &= mask;
					channels[channel].Pitch |= (ushort)(value << ((register & 0b1) * 8));
					break;

				case 0x88:
				case 0x89:
				case 0x8A:
				case 0x8B:
					/* REG_SND_CHx_VOL */
					channels[register & 0b11].VolumeLeft = (byte)((value >> 4) & 0b1111);
					channels[register & 0b11].VolumeRight = (byte)((value >> 0) & 0b1111);
					break;

				case 0x8C:
					/* REG_SND_SWEEP_VALUE */
					(channels[2] as Wave).SweepValue = (sbyte)value;
					break;

				case 0x8D:
					/* REG_SND_SWEEP_TIME */
					(channels[2] as Wave).SweepTime = (byte)(value & 0b11111);
					break;

				case 0x8E:
					/* REG_SND_NOISE */
					(channels[3] as Noise).NoiseMode = (byte)(value & 0b111);
					(channels[3] as Noise).NoiseReset = IsBitSet(value, 3);
					(channels[3] as Noise).NoiseEnable = IsBitSet(value, 4);
					break;

				case 0x8F:
					/* REG_SND_WAVE_BASE */
					waveTableBase = value;
					break;

				case 0x90:
					/* REG_SND_CTRL */
					for (var i = 0; i < numChannels; i++)
					{
						channels[i].Enable = IsBitSet(value, i);
						channels[i].Mode = i != 0 && IsBitSet(value, i + 4);
					}
					break;

				case 0x91:
					/* REG_SND_OUTPUT */
					speakerEnable = IsBitSet(value, 0);
					speakerVolumeShift = (byte)((value >> 1) & 0b11);
					headphoneEnable = IsBitSet(value, 3);
					break;

				case 0x92:
				case 0x93:
					/* REG_SND_RANDOM */
					break;

				case 0x94:
					/* REG_SND_VOICE_CTRL */
					(channels[1] as Voice).PcmRightFull = IsBitSet(value, 0);
					(channels[1] as Voice).PcmRightHalf = IsBitSet(value, 1);
					(channels[1] as Voice).PcmLeftFull = IsBitSet(value, 2);
					(channels[1] as Voice).PcmLeftHalf = IsBitSet(value, 3);
					break;

				case 0x96:
					/* REG_SND_9697 (low) */
					unknown9697 = (ushort)((unknown9697 & 0x0300) | (value << 0));
					break;

				case 0x97:
					/* REG_SND_9697 (high) */
					unknown9697 = (ushort)((unknown9697 & 0x00FF) | (value << 8));
					break;

				case 0x98:
					/* REG_SND_9899 (low) */
					unknown9899 = (ushort)((unknown9899 & 0x0300) | (value << 0));
					break;

				case 0x99:
					/* REG_SND_9899 (high) */
					unknown9899 = (ushort)((unknown9899 & 0x00FF) | (value << 8));
					break;

				case 0x9A:
				case 0x9B:
				case 0x9C:
				case 0x9D:
					/* REG_SND_9x */
					break;

				case 0x9E:
					/* REG_SND_9E */
					unknown9E = (byte)(value & 0b11);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register write {register:X2}");
			}
		}
	}
}

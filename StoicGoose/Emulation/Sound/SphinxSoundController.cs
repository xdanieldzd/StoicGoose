using System;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Sound
{
	public class SphinxSoundController : SoundControllerCommon
	{
		public override int MaxMasterVolume => 3;

		/* REG_HYPER_CTRL */
		bool hyperEnable;
		int hyperScalingMode, hyperVolume;
		byte hyperCtrlUnknown;
		/* REG_HYPER_CHAN_CTRL */
		bool hyperRightEnable, hyperLeftEnable;
		byte hyperChanCtrlUnknown;

		/* REG_SND_HYPERVOICE */
		byte hyperVoiceData;

		byte hyperVoiceOutput;

		public SphinxSoundController(MemoryReadDelegate memoryRead, int rate, int outChannels) : base(memoryRead, rate, outChannels) { }

		public override void ResetRegisters()
		{
			base.ResetRegisters();

			hyperEnable = false;
			hyperScalingMode = hyperVolume = 0;
			hyperCtrlUnknown = 0;

			hyperRightEnable = hyperLeftEnable = false;
			hyperChanCtrlUnknown = 0;

			hyperVoiceData = 0;

			hyperVoiceOutput = 0;
		}

		public override void StepChannels()
		{
			for (var j = 0; j < numChannels; j++)
				channels[j].Step();

			StepHyperVoice();
		}

		private void StepHyperVoice()
		{
			switch (hyperScalingMode)
			{
				case 0: hyperVoiceOutput = (byte)(hyperVoiceData << 3 - hyperVolume); break;
				case 1: hyperVoiceOutput = (byte)((hyperVoiceData << 3 - hyperVolume) | (-0x100 << 3 - hyperVolume)); break;
				case 2: hyperVoiceOutput = (byte)(hyperVoiceData << 3 - hyperVolume); break;    // ???
				case 3: hyperVoiceOutput = (byte)(hyperVoiceData << 3); break;
			}
		}

		public override void GenerateSample()
		{
			var mixedLeft = 0;
			if (channels[0].Enable) mixedLeft += channels[0].OutputLeft;
			if (channels[1].Enable) mixedLeft += channels[1].OutputLeft;
			if (channels[2].Enable) mixedLeft += channels[2].OutputLeft;
			if (channels[3].Enable) mixedLeft += channels[3].OutputLeft;
			if (hyperLeftEnable && HeadphonesConnected) mixedLeft += hyperVoiceOutput;
			mixedLeft = (mixedLeft & 0x07FF) << 5;

			var mixedRight = 0;
			if (channels[0].Enable) mixedRight += channels[0].OutputRight;
			if (channels[1].Enable) mixedRight += channels[1].OutputRight;
			if (channels[2].Enable) mixedRight += channels[2].OutputRight;
			if (channels[3].Enable) mixedRight += channels[3].OutputRight;
			if (hyperRightEnable && HeadphonesConnected) mixedRight += hyperVoiceOutput;
			mixedRight = (mixedRight & 0x07FF) << 5;

			if (HeadphonesConnected && !headphoneEnable && !speakerEnable)
				/* Headphones connected but neither headphones nor speaker enabled? Don't output sound */
				mixedLeft = mixedRight = 0;
			else if (!HeadphonesConnected)
				/* Otherwise, no headphones connected? Mix down to mono */
				mixedLeft = mixedRight = (mixedLeft + mixedRight) / 2;

			mixedSampleBuffer.Add((short)(mixedLeft * (MasterVolume / 3.0)));
			mixedSampleBuffer.Add((short)(mixedRight * (MasterVolume / 3.0)));
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x6A:
					/* REG_HYPER_CTRL */
					ChangeBit(ref retVal, 7, hyperEnable);
					retVal |= (byte)((hyperCtrlUnknown << 4) & 0b111);
					retVal |= (byte)((hyperScalingMode << 2) & 0b11);
					retVal |= (byte)((hyperVolume << 0) & 0b11);
					break;

				case 0x6B:
					/* REG_HYPER_CHAN_CTRL */
					ChangeBit(ref retVal, 6, hyperRightEnable);
					ChangeBit(ref retVal, 5, hyperLeftEnable);
					retVal |= (byte)((hyperChanCtrlUnknown << 0) & 0b1111);
					break;

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

				case 0x95:
					/* REG_SND_HYPERVOICE */
					retVal |= hyperVoiceData;
					break;

				case 0x96:
				case 0x97:
					break;

				case 0x98:
				case 0x99:
				case 0x9A:
				case 0x9B:
				case 0x9C:
				case 0x9D:
					/* REG_SND_9x */
					retVal = 0;
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
				case 0x6A:
					/* REG_HYPER_CTRL */
					hyperEnable = IsBitSet(value, 7);
					hyperCtrlUnknown = (byte)((value >> 4) & 0b111);
					hyperScalingMode = (byte)((value >> 2) & 0b11);
					hyperVolume = (byte)((value >> 0) & 0b11);
					break;

				case 0x6B:
					/* REG_HYPER_CHAN_CTRL */
					hyperRightEnable = IsBitSet(value, 6);
					hyperLeftEnable = IsBitSet(value, 5);
					hyperChanCtrlUnknown = (byte)((value >> 0) & 0b1111);
					break;

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

				case 0x95:
					/* REG_SND_HYPERVOICE */
					hyperVoiceData = value;
					break;

				case 0x96:
				case 0x97:
				case 0x98:
				case 0x99:
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

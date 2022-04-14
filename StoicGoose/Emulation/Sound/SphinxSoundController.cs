using System;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Sound
{
	public class SphinxSoundController : SoundControllerCommon
	{
		public override byte MaxMasterVolume => 3;

		readonly SoundChannelHyperVoice channelHyperVoice = default;

		public SphinxSoundController(MemoryReadDelegate memoryRead, int rate, int outChannels) : base(memoryRead, rate, outChannels)
		{
			channelHyperVoice = new();
		}

		public override void Reset()
		{
			base.Reset();

			channelHyperVoice.Reset();
		}

		public override void StepChannels()
		{
			base.StepChannels();

			channelHyperVoice.Step();
		}

		public override void GenerateSample()
		{
			var mixedLeft = 0;
			if (channel1.IsEnabled) mixedLeft += channel1.OutputLeft;
			if (channel2.IsEnabled) mixedLeft += channel2.OutputLeft;
			if (channel3.IsEnabled) mixedLeft += channel3.OutputLeft;
			if (channel4.IsEnabled) mixedLeft += channel4.OutputLeft;
			if (channelHyperVoice.IsEnabled && HeadphonesConnected) mixedLeft += channelHyperVoice.OutputLeft;
			mixedLeft = (mixedLeft & 0x07FF) << 5;

			var mixedRight = 0;
			if (channel1.IsEnabled) mixedRight += channel1.OutputRight;
			if (channel2.IsEnabled) mixedRight += channel2.OutputRight;
			if (channel3.IsEnabled) mixedRight += channel3.OutputRight;
			if (channel4.IsEnabled) mixedRight += channel4.OutputRight;
			if (channelHyperVoice.IsEnabled && HeadphonesConnected) mixedRight += channelHyperVoice.OutputRight;
			mixedRight = (mixedRight & 0x07FF) << 5;

			if (HeadphonesConnected && !headphoneEnable && !speakerEnable)
				/* Headphones connected but neither headphones nor speaker enabled? Don't output sound */
				mixedLeft = mixedRight = 0;
			else if (!HeadphonesConnected)
				/* Otherwise, no headphones connected? Mix down to mono, perform volume shift */
				mixedLeft = mixedRight = ((mixedLeft + mixedRight) / 2) >> speakerVolumeShift;

			mixedSampleBuffer.Add((short)(mixedLeft * (masterVolume / 3.0)));
			mixedSampleBuffer.Add((short)(mixedRight * (masterVolume / 3.0)));
		}

		public override byte ReadRegister(ushort register)
		{
			var retVal = (byte)0;

			switch (register)
			{
				case 0x6A:
					/* REG_HYPER_CTRL */
					ChangeBit(ref retVal, 7, channelHyperVoice.IsEnabled);
					retVal |= (byte)((channelHyperVoice.CtrlUnknown << 4) & 0b111);
					retVal |= (byte)((channelHyperVoice.ScalingMode << 2) & 0b11);
					retVal |= (byte)((channelHyperVoice.Volume << 0) & 0b11);
					break;

				case 0x6B:
					/* REG_HYPER_CHAN_CTRL */
					ChangeBit(ref retVal, 6, channelHyperVoice.RightEnable);
					ChangeBit(ref retVal, 5, channelHyperVoice.LeftEnable);
					retVal |= (byte)((channelHyperVoice.ChanCtrlUnknown << 0) & 0b1111);
					break;

				case 0x80:
				case 0x81:
					/* REG_SND_CH1_PITCH */
					retVal |= (byte)(channel1.Pitch >> ((register & 0b1) * 8));
					break;
				case 0x82:
				case 0x83:
					/* REG_SND_CH2_PITCH */
					retVal |= (byte)(channel2.Pitch >> ((register & 0b1) * 8));
					break;
				case 0x84:
				case 0x85:
					/* REG_SND_CH3_PITCH */
					retVal |= (byte)(channel3.Pitch >> ((register & 0b1) * 8));
					break;
				case 0x86:
				case 0x87:
					/* REG_SND_CH4_PITCH */
					retVal |= (byte)(channel4.Pitch >> ((register & 0b1) * 8));
					break;

				case 0x88:
					/* REG_SND_CH1_VOL */
					retVal |= (byte)(channel1.VolumeLeft << 4 | channel1.VolumeRight);
					break;
				case 0x89:
					/* REG_SND_CH2_VOL */
					retVal |= (byte)(channel2.VolumeLeft << 4 | channel2.VolumeRight);
					break;
				case 0x8A:
					/* REG_SND_CH3_VOL */
					retVal |= (byte)(channel3.VolumeLeft << 4 | channel3.VolumeRight);
					break;
				case 0x8B:
					/* REG_SND_CH4_VOL */
					retVal |= (byte)(channel4.VolumeLeft << 4 | channel4.VolumeRight);
					break;

				case 0x8C:
					/* REG_SND_SWEEP_VALUE */
					retVal |= (byte)channel3.SweepValue;
					break;

				case 0x8D:
					/* REG_SND_SWEEP_TIME */
					retVal |= (byte)(channel3.SweepTime & 0b11111);
					break;

				case 0x8E:
					/* REG_SND_NOISE */
					retVal |= (byte)(channel4.NoiseMode & 0b111);
					// Noise reset (bit 3) always reads 0
					ChangeBit(ref retVal, 4, channel4.NoiseEnable);
					break;

				case 0x8F:
					/* REG_SND_WAVE_BASE */
					retVal |= waveTableBase;
					break;

				case 0x90:
					/* REG_SND_CTRL */
					ChangeBit(ref retVal, 0, channel1.IsEnabled);
					ChangeBit(ref retVal, 1, channel2.IsEnabled);
					ChangeBit(ref retVal, 2, channel3.IsEnabled);
					ChangeBit(ref retVal, 3, channel4.IsEnabled);
					ChangeBit(ref retVal, 5, channel2.IsVoiceEnabled);
					ChangeBit(ref retVal, 6, channel3.IsSweepEnabled);
					ChangeBit(ref retVal, 7, channel4.IsNoiseEnabled);
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
					retVal |= (byte)((channel4.NoiseLfsr >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0x94:
					/* REG_SND_VOICE_CTRL */
					ChangeBit(ref retVal, 0, channel2.PcmRightFull);
					ChangeBit(ref retVal, 1, channel2.PcmRightHalf);
					ChangeBit(ref retVal, 2, channel2.PcmLeftFull);
					ChangeBit(ref retVal, 3, channel2.PcmLeftHalf);
					break;

				case 0x95:
					/* REG_SND_HYPERVOICE */
					retVal |= channelHyperVoice.Data;
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
					/* REG_SND_VOLUME */
					retVal |= (byte)(masterVolume & 0b11);
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
					channelHyperVoice.IsEnabled = IsBitSet(value, 7);
					channelHyperVoice.CtrlUnknown = (byte)((value >> 4) & 0b111);
					channelHyperVoice.ScalingMode = (byte)((value >> 2) & 0b11);
					channelHyperVoice.Volume = (byte)((value >> 0) & 0b11);
					break;

				case 0x6B:
					/* REG_HYPER_CHAN_CTRL */
					channelHyperVoice.RightEnable = IsBitSet(value, 6);
					channelHyperVoice.LeftEnable = IsBitSet(value, 5);
					channelHyperVoice.ChanCtrlUnknown = (byte)((value >> 0) & 0b1111);
					break;

				case 0x80:
				case 0x81:
					/* REG_SND_CH1_PITCH */
					channel1.Pitch &= (ushort)((register & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel1.Pitch |= (ushort)(value << ((register & 0b1) * 8));
					break;
				case 0x82:
				case 0x83:
					/* REG_SND_CH2_PITCH */
					channel2.Pitch &= (ushort)((register & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel2.Pitch |= (ushort)(value << ((register & 0b1) * 8));
					break;
				case 0x84:
				case 0x85:
					/* REG_SND_CH3_PITCH */
					channel3.Pitch &= (ushort)((register & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel3.Pitch |= (ushort)(value << ((register & 0b1) * 8));
					break;
				case 0x86:
				case 0x87:
					/* REG_SND_CH4_PITCH */
					channel4.Pitch &= (ushort)((register & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel4.Pitch |= (ushort)(value << ((register & 0b1) * 8));
					break;

				case 0x88:
					/* REG_SND_CH1_VOL */
					channel1.VolumeLeft = (byte)((value >> 4) & 0b1111);
					channel1.VolumeRight = (byte)((value >> 0) & 0b1111);
					break;
				case 0x89:
					/* REG_SND_CH2_VOL */
					channel2.VolumeLeft = (byte)((value >> 4) & 0b1111);
					channel2.VolumeRight = (byte)((value >> 0) & 0b1111);
					break;
				case 0x8A:
					/* REG_SND_CH3_VOL */
					channel3.VolumeLeft = (byte)((value >> 4) & 0b1111);
					channel3.VolumeRight = (byte)((value >> 0) & 0b1111);
					break;
				case 0x8B:
					/* REG_SND_CH4_VOL */
					channel4.VolumeLeft = (byte)((value >> 4) & 0b1111);
					channel4.VolumeRight = (byte)((value >> 0) & 0b1111);
					break;

				case 0x8C:
					/* REG_SND_SWEEP_VALUE */
					channel3.SweepValue = (sbyte)value;
					break;

				case 0x8D:
					/* REG_SND_SWEEP_TIME */
					channel3.SweepTime = (byte)(value & 0b11111);
					break;

				case 0x8E:
					/* REG_SND_NOISE */
					channel4.NoiseMode = (byte)(value & 0b111);
					channel4.NoiseReset = IsBitSet(value, 3);
					channel4.NoiseEnable = IsBitSet(value, 4);
					break;

				case 0x8F:
					/* REG_SND_WAVE_BASE */
					waveTableBase = value;
					break;

				case 0x90:
					/* REG_SND_CTRL */
					channel1.IsEnabled = IsBitSet(value, 0);
					channel2.IsEnabled = IsBitSet(value, 1);
					channel3.IsEnabled = IsBitSet(value, 2);
					channel4.IsEnabled = IsBitSet(value, 3);
					channel2.IsVoiceEnabled = IsBitSet(value, 5);
					channel3.IsSweepEnabled = IsBitSet(value, 6);
					channel4.IsNoiseEnabled = IsBitSet(value, 7);
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
					channel2.PcmRightFull = IsBitSet(value, 0);
					channel2.PcmRightHalf = IsBitSet(value, 1);
					channel2.PcmLeftFull = IsBitSet(value, 2);
					channel2.PcmLeftHalf = IsBitSet(value, 3);
					break;

				case 0x95:
					/* REG_SND_HYPERVOICE */
					channelHyperVoice.Data = value;
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
					/* REG_SND_VOLUME */
					masterVolume = (byte)(value & 0b11);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register write {register:X2}");
			}
		}
	}
}

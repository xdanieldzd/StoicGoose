﻿using static StoicGoose.Utilities;

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

		public override int[] GenerateSample()
		{
			var mixedLeft = 0;
			if (channel1.IsEnabled) mixedLeft += channel1.OutputLeft;
			if (channel2.IsEnabled) mixedLeft += channel2.OutputLeft;
			if (channel3.IsEnabled) mixedLeft += channel3.OutputLeft;
			if (channel4.IsEnabled) mixedLeft += channel4.OutputLeft;
			if (channelHyperVoice.IsEnabled && headphonesConnected) mixedLeft += channelHyperVoice.OutputLeft;
			mixedLeft = (mixedLeft & 0x07FF) << 5;

			var mixedRight = 0;
			if (channel1.IsEnabled) mixedRight += channel1.OutputRight;
			if (channel2.IsEnabled) mixedRight += channel2.OutputRight;
			if (channel3.IsEnabled) mixedRight += channel3.OutputRight;
			if (channel4.IsEnabled) mixedRight += channel4.OutputRight;
			if (channelHyperVoice.IsEnabled && headphonesConnected) mixedRight += channelHyperVoice.OutputRight;
			mixedRight = (mixedRight & 0x07FF) << 5;

			return new[] { mixedLeft, mixedRight };
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

				case 0x95:
					/* REG_SND_HYPERVOICE */
					retVal |= channelHyperVoice.Data;
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
					retVal = 0;
					break;

				default:
					/* Fall through to common */
					retVal |= base.ReadRegister(register);
					break;
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

				default:
					/* Fall through to common */
					base.WriteRegister(register, value);
					break;
			}
		}
	}
}

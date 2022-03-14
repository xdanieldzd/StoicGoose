using System;
using System.Collections.Generic;

using StoicGoose.Emulation.Machines;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Sound
{
	public sealed partial class SoundController : IComponent
	{
		/* http://daifukkat.su/docs/wsman/#hw_sound */

		// TODO: split like ASWAN/SPHINX display controllers, then add SPHINX-specific stuff

		const int numChannels = 4, maxMasterVolume = 2;

		readonly int sampleRate, numOutputChannels;

		readonly ISoundChannel[] channels = new ISoundChannel[numChannels];

		readonly List<short> mixedSampleBuffer;
		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		readonly double clockRate, refreshRate;
		readonly int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int cycleCount;

		int masterVolume, masterVolumeChange;

		public delegate byte MemoryReadDelegate(uint address);
		readonly MemoryReadDelegate memoryReadDelegate;

		public delegate byte WaveTableReadDelegate(ushort address);

		/* REG_SND_WAVE_BASE */
		byte waveTableBase;
		/* REG_SND_OUTPUT */
		bool speakerEnable, headphoneEnable;
		bool headphonesConnected; // read-only
		byte speakerVolumeShift;
		/* REG_SND_9697 */
		ushort unknown9697;
		/* REG_SND_9899 */
		ushort unknown9899;
		/* REG_SND_9E */
		byte unknown9E;

		public SoundController(MemoryReadDelegate memoryRead, int rate, int outChannels)
		{
			memoryReadDelegate = memoryRead;

			sampleRate = rate;
			numOutputChannels = outChannels;

			channels[0] = new Wave(false, (a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (0 << 4) + a)); });
			channels[1] = new Voice((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (1 << 4) + a)); });
			channels[2] = new Wave(true, (a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (2 << 4) + a)); });
			channels[3] = new Noise((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (3 << 4) + a)); });

			mixedSampleBuffer = new List<short>();

			clockRate = WonderSwan.CpuClock;
			refreshRate = Display.DisplayControllerCommon.VerticalClock;

			samplesPerFrame = (int)(sampleRate / refreshRate);
			cyclesPerFrame = (int)(clockRate / refreshRate);
			cyclesPerSample = cyclesPerFrame / samplesPerFrame;
		}

		public void Reset()
		{
			cycleCount = 0;

			for (var i = 0; i < numChannels; i++)
				channels[i].Reset();

			FlushSamples();

			masterVolume = maxMasterVolume;
			masterVolumeChange = -1;

			ResetRegisters();
		}

		private void ResetRegisters()
		{
			waveTableBase = 0;
			speakerEnable = headphoneEnable = false;
			headphonesConnected = true; // for stereo sound
			speakerVolumeShift = 0;

			unknown9697 = 0;
			unknown9899 = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void ToggleMasterVolume()
		{
			if (masterVolumeChange != -1) return;

			masterVolumeChange = masterVolume - 1;
			if (masterVolumeChange < 0) masterVolumeChange = maxMasterVolume;
		}

		public void Step(int clockCyclesInStep)
		{
			cycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
			{
				for (var j = 0; j < numChannels; j++)
					channels[j].Step();
			}

			if (cycleCount >= cyclesPerSample)
			{
				GenerateSample();
				cycleCount -= cyclesPerSample;
			}

			if (mixedSampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				OnEnqueueSamples(new EnqueueSamplesEventArgs(numChannels, mixedSampleBuffer.ToArray()));
				FlushSamples();

				if (masterVolumeChange != -1)
				{
					masterVolume = masterVolumeChange;
					masterVolumeChange = -1;
				}
			}
		}

		private void GenerateSample()
		{
			var mixedLeft = 0;
			if (channels[0].Enable) mixedLeft += channels[0].OutputLeft;
			if (channels[1].Enable) mixedLeft += channels[1].OutputLeft;
			if (channels[2].Enable) mixedLeft += channels[2].OutputLeft;
			if (channels[3].Enable) mixedLeft += channels[3].OutputLeft;
			mixedLeft = (mixedLeft & 0x07FF) << 4;

			var mixedRight = 0;
			if (channels[0].Enable) mixedRight += channels[0].OutputRight;
			if (channels[1].Enable) mixedRight += channels[1].OutputRight;
			if (channels[2].Enable) mixedRight += channels[2].OutputRight;
			if (channels[3].Enable) mixedRight += channels[3].OutputRight;
			mixedRight = (mixedRight & 0x07FF) << 4;

			if (headphonesConnected && !headphoneEnable && !speakerEnable)
				/* Headphones connected but neither headphones nor speaker enabled? Don't output sound */
				mixedLeft = mixedRight = 0;
			else if (!headphonesConnected)
				/* Otherwise, no headphones connected? Mix down to mono */
				mixedLeft = mixedRight = (mixedLeft + mixedRight) / 2;

			mixedSampleBuffer.Add((short)(mixedLeft * (masterVolume / 2.0)));
			mixedSampleBuffer.Add((short)(mixedRight * (masterVolume / 2.0)));
		}

		public void FlushSamples()
		{
			mixedSampleBuffer.Clear();
		}

		public List<string> GetActiveIcons()
		{
			var list = new List<string>();
			if (headphonesConnected) list.Add("headphones");
			list.Add($"volume{masterVolume}");
			return list;
		}

		public byte ReadRegister(ushort register)
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
					ChangeBit(ref retVal, 7, headphonesConnected);
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
				case 0x97:
					/* REG_SND_9697 */
					retVal |= (byte)((unknown9697 >> ((register & 0b1) * 8)) & 0xFF);
					break;

				case 0x98:
				case 0x99:
					/* REG_SND_9899 */
					retVal |= (byte)((unknown9899 >> ((register & 0b1) * 8)) & 0xFF);
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

		public void WriteRegister(ushort register, byte value)
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
					unknown9697 = (ushort)((unknown9697 & 0xFF00) | (value << 0));
					break;

				case 0x97:
					/* REG_SND_9697 (high) */
					unknown9697 = (ushort)((unknown9697 & 0x00FF) | (value << 8));
					break;

				case 0x98:
					/* REG_SND_9899 (low) */
					unknown9899 = (ushort)((unknown9899 & 0xFF00) | (value << 0));
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

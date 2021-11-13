using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StoicGoose.Emulation.Machines;
using StoicGoose.WinForms;

using static StoicGoose.Utilities;

namespace StoicGoose.Emulation.Sound
{
	public sealed partial class SoundController
	{
		// http://daifukkat.su/docs/wsman/#hw_sound

		const int numChannels = 4, maxMasterVolume = 2;

		// TODO: confirm which regs are available on original WS (ex. HyperVoice is not)

		readonly int sampleRate, numOutputChannels;

		readonly ISoundChannel[] channels = new ISoundChannel[numChannels];

		readonly List<short> mixedSampleBuffer;
		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		readonly double clockRate, refreshRate;
		readonly int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int sampleCycleCount, frameCycleCount;

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

		public SoundController(MemoryReadDelegate memoryRead)
		{
			memoryReadDelegate = memoryRead;

			sampleRate = 44100;
			numOutputChannels = 2;

			channels[0] = new Wave((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (0 << 4) + a)); }, false);
			channels[1] = new Voice((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (1 << 4) + a)); });
			channels[2] = new Wave((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (2 << 4) + a)); }, true);
			channels[3] = new Noise((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (3 << 4) + a)); });

			mixedSampleBuffer = new List<short>();

			clockRate = WonderSwan.MasterClock / 4.0;   // ????
			refreshRate = Display.DisplayController.VerticalClock;

			samplesPerFrame = (int)(sampleRate / refreshRate);
			cyclesPerFrame = (int)(clockRate / refreshRate);
			cyclesPerSample = cyclesPerFrame / samplesPerFrame;
		}

		public void Reset()
		{
			sampleCycleCount = frameCycleCount = 0;

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
		}

		public void ToggleMasterVolume()
		{
			if (masterVolumeChange != -1) return;

			masterVolumeChange = masterVolume - 1;
			if (masterVolumeChange < 0) masterVolumeChange = maxMasterVolume;
		}

		public void Step(int clockCyclesInStep)
		{
			sampleCycleCount += clockCyclesInStep;
			frameCycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
			{
				for (var j = 0; j < numChannels; j++)
					channels[j].Step();
			}

			if (sampleCycleCount >= cyclesPerSample)
			{
				GenerateSample();

				sampleCycleCount -= cyclesPerSample;
			}

			if (mixedSampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				OnEnqueueSamples(new EnqueueSamplesEventArgs(numChannels, mixedSampleBuffer.ToArray()));

				FlushSamples();
			}

			if (frameCycleCount >= cyclesPerFrame)
			{
				frameCycleCount -= cyclesPerFrame;
				sampleCycleCount = frameCycleCount;

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

			if (headphonesConnected)
			{
				if (headphoneEnable)
					mixedLeft = mixedRight = (mixedLeft + mixedRight) / 2;
				else
					mixedLeft = mixedRight = 0;
			}
			else if (!speakerEnable)
				mixedLeft = mixedRight = 0;

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

				//

				case 0x94:
					/* REG_SND_VOICE_CTRL */
					(channels[1] as Voice).PcmRightFull = IsBitSet(value, 0);
					(channels[1] as Voice).PcmRightHalf = IsBitSet(value, 1);
					(channels[1] as Voice).PcmLeftFull = IsBitSet(value, 2);
					(channels[1] as Voice).PcmLeftHalf = IsBitSet(value, 3);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register write {register:X2}");
			}
		}
	}
}

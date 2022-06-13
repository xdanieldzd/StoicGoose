using System;
using System.Collections.Generic;

using StoicGoose.Common.Attributes;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.Machines;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Sound
{
	public delegate byte WaveTableReadDelegate(ushort address);

	public abstract partial class SoundControllerCommon : IPortAccessComponent
	{
		/* http://daifukkat.su/docs/wsman/#hw_sound */

		public abstract byte MaxMasterVolume { get; }
		public abstract int NumChannels { get; }

		readonly int sampleRate, numOutputChannels;

		protected readonly SoundChannel1 channel1 = default;
		protected readonly SoundChannel2 channel2 = default;
		protected readonly SoundChannel3 channel3 = default;
		protected readonly SoundChannel4 channel4 = default;

		protected readonly List<short>[] channelSampleBuffers = default;
		protected readonly List<short> mixedSampleBuffer = new();

		public short[][] LastEnqueuedChannelSamples { get; private set; } = default;
		public short[] LastEnqueuedMixedSamples { get; private set; } = Array.Empty<short>();

		public Action<short[]> SendSamples { get; set; } = default;

		readonly double clockRate, refreshRate;
		readonly int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int cycleCount;

		protected readonly IMachine machine = default;

		/* REG_SND_WAVE_BASE */
		protected byte waveTableBase;
		/* REG_SND_OUTPUT */
		protected bool speakerEnable, headphoneEnable, headphonesConnected;
		protected byte speakerVolumeShift;
		/* REG_SND_VOLUME */
		protected byte masterVolume;

		public SoundControllerCommon(IMachine machine, int rate, int outChannels)
		{
			this.machine = machine;

			sampleRate = rate;
			numOutputChannels = outChannels;

			channel1 = new SoundChannel1((a) => this.machine.ReadMemory((uint)((waveTableBase << 6) + (0 << 4) + a)));
			channel2 = new SoundChannel2((a) => this.machine.ReadMemory((uint)((waveTableBase << 6) + (1 << 4) + a)));
			channel3 = new SoundChannel3((a) => this.machine.ReadMemory((uint)((waveTableBase << 6) + (2 << 4) + a)));
			channel4 = new SoundChannel4((a) => this.machine.ReadMemory((uint)((waveTableBase << 6) + (3 << 4) + a)));

			channelSampleBuffers = new List<short>[NumChannels];
			for (var i = 0; i < channelSampleBuffers.Length; i++) channelSampleBuffers[i] = new();

			LastEnqueuedChannelSamples = new short[NumChannels][];

			clockRate = MachineCommon.CpuClock;
			refreshRate = Display.DisplayControllerCommon.VerticalClock;

			samplesPerFrame = (int)(sampleRate / refreshRate);
			cyclesPerFrame = (int)(clockRate / refreshRate);
			cyclesPerSample = cyclesPerFrame / samplesPerFrame;
		}

		public virtual void Reset()
		{
			cycleCount = 0;

			channel1.Reset();
			channel2.Reset();
			channel3.Reset();
			channel4.Reset();

			FlushSamples();

			ResetRegisters();
		}

		public virtual void ResetRegisters()
		{
			waveTableBase = 0;
			speakerEnable = headphoneEnable = false;
			headphonesConnected = true; /* NOTE: always set for stereo sound */
			speakerVolumeShift = 0;
			masterVolume = MaxMasterVolume;
		}

		public void Shutdown()
		{
			/* Nothing to do */
		}

		public void ChangeMasterVolume()
		{
			var newMasterVolume = MasterVolume - 1;
			if (newMasterVolume < 0) newMasterVolume = MaxMasterVolume;
			else if (newMasterVolume > MaxMasterVolume) newMasterVolume = 0;

			masterVolume = (byte)newMasterVolume;
		}

		public void Step(int clockCyclesInStep)
		{
			cycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
				StepChannels();

			if (cycleCount >= cyclesPerSample)
			{
				ProcessSample(GenerateSample());
				cycleCount -= cyclesPerSample;
			}

			if (mixedSampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				var sampleArray = mixedSampleBuffer.ToArray();

				LastEnqueuedMixedSamples = sampleArray;
				for (var i = 0; i < NumChannels; i++) LastEnqueuedChannelSamples[i] = channelSampleBuffers[i].ToArray();

				SendSamples?.Invoke(sampleArray.Clone() as short[]);

				FlushSamples();
			}
		}

		public virtual void StepChannels()
		{
			channel1.Step();
			channel2.Step();
			channel3.Step();
			channel4.Step();
		}

		public abstract int[] GenerateSample();

		public virtual void ProcessSample(int[] lrSamples)
		{
			if (headphonesConnected && !headphoneEnable && !speakerEnable)
				/* Headphones connected but neither headphones nor speaker enabled? Don't output sound */
				lrSamples[0] = lrSamples[1] = 0;
			else if (!headphonesConnected)
				/* Otherwise, no headphones connected? Mix down to mono, perform volume shift */
				lrSamples[0] = lrSamples[1] = ((lrSamples[0] + lrSamples[1]) / 2) >> speakerVolumeShift;

			mixedSampleBuffer.Add((short)(lrSamples[0] * (masterVolume / (double)MaxMasterVolume))); /* Left */
			mixedSampleBuffer.Add((short)(lrSamples[1] * (masterVolume / (double)MaxMasterVolume))); /* Right */
		}

		public void FlushSamples()
		{
			foreach (var buffer in channelSampleBuffers)
				buffer.Clear();

			mixedSampleBuffer.Clear();
		}

		public virtual byte ReadPort(ushort port)
		{
			var retVal = (byte)0;

			switch (port)
			{
				case 0x80:
				case 0x81:
					/* REG_SND_CH1_PITCH */
					retVal |= (byte)(channel1.Pitch >> ((port & 0b1) * 8));
					break;
				case 0x82:
				case 0x83:
					/* REG_SND_CH2_PITCH */
					retVal |= (byte)(channel2.Pitch >> ((port & 0b1) * 8));
					break;
				case 0x84:
				case 0x85:
					/* REG_SND_CH3_PITCH */
					retVal |= (byte)(channel3.Pitch >> ((port & 0b1) * 8));
					break;
				case 0x86:
				case 0x87:
					/* REG_SND_CH4_PITCH */
					retVal |= (byte)(channel4.Pitch >> ((port & 0b1) * 8));
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
					/* Noise reset (bit 3) always reads 0 */
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
					ChangeBit(ref retVal, 7, headphonesConnected);
					retVal |= (byte)((speakerVolumeShift & 0b11) << 1);
					break;

				case 0x92:
				case 0x93:
					/* REG_SND_RANDOM */
					retVal |= (byte)((channel4.NoiseLfsr >> ((port & 0b1) * 8)) & 0xFF);
					break;

				case 0x94:
					/* REG_SND_VOICE_CTRL */
					ChangeBit(ref retVal, 0, channel2.PcmRightFull);
					ChangeBit(ref retVal, 1, channel2.PcmRightHalf);
					ChangeBit(ref retVal, 2, channel2.PcmLeftFull);
					ChangeBit(ref retVal, 3, channel2.PcmLeftHalf);
					break;

				case 0x9E:
					/* REG_SND_VOLUME */
					retVal |= (byte)(masterVolume & 0b11);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register read {port:X2}");
			}

			return retVal;
		}

		public virtual void WritePort(ushort port, byte value)
		{
			switch (port)
			{
				case 0x80:
				case 0x81:
					/* REG_SND_CH1_PITCH */
					channel1.Pitch &= (ushort)((port & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel1.Pitch |= (ushort)(value << ((port & 0b1) * 8));
					break;
				case 0x82:
				case 0x83:
					/* REG_SND_CH2_PITCH */
					channel2.Pitch &= (ushort)((port & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel2.Pitch |= (ushort)(value << ((port & 0b1) * 8));
					break;
				case 0x84:
				case 0x85:
					/* REG_SND_CH3_PITCH */
					channel3.Pitch &= (ushort)((port & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel3.Pitch |= (ushort)(value << ((port & 0b1) * 8));
					break;
				case 0x86:
				case 0x87:
					/* REG_SND_CH4_PITCH */
					channel4.Pitch &= (ushort)((port & 0b1) != 0b1 ? 0x0700 : 0x00FF);
					channel4.Pitch |= (ushort)(value << ((port & 0b1) * 8));
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
					/* Headphones connected (bit 7) is read-only */
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

				case 0x9E:
					/* REG_SND_VOLUME */
					masterVolume = (byte)(value & 0b11);
					break;

				default:
					throw new NotImplementedException($"Unimplemented sound register write {port:X2}");
			}
		}

		[Port("REG_SND_CH1_PITCH", 0x080, 0x081)]
		[BitDescription("Channel 1 pitch (frequency reload)", 0, 10)]
		public ushort Channel1Pitch => channel1.Pitch;
		[Port("REG_SND_CH2_PITCH", 0x082, 0x083)]
		[BitDescription("Channel 2 pitch (frequency reload)", 0, 10)]
		public ushort Channel2Pitch => channel2.Pitch;
		[Port("REG_SND_CH3_PITCH", 0x084, 0x085)]
		[BitDescription("Channel 3 pitch (frequency reload)", 0, 10)]
		public ushort Channel3Pitch => channel3.Pitch;
		[Port("REG_SND_CH4_PITCH", 0x086, 0x087)]
		[BitDescription("Channel 4 pitch (frequency reload)", 0, 10)]
		public ushort Channel4Pitch => channel4.Pitch;
		[Port("REG_SND_CH1_VOL", 0x088)]
		[BitDescription("Channel 1 volume right", 0, 3)]
		public byte Channel1VolumeRight => channel1.VolumeRight;
		[Port("REG_SND_CH1_VOL", 0x088)]
		[BitDescription("Channel 1 volume left", 4, 7)]
		public byte Channel1VolumeLeft => channel1.VolumeLeft;
		[Port("REG_SND_CH2_VOL", 0x089)]
		[BitDescription("Channel 2 volume right", 0, 3)]
		public byte Channel2VolumeRight => channel2.VolumeRight;
		[Port("REG_SND_CH2_VOL", 0x089)]
		[BitDescription("Channel 2 volume left", 4, 7)]
		public byte Channel2VolumeLeft => channel2.VolumeLeft;
		[Port("REG_SND_CH3_VOL", 0x08A)]
		[BitDescription("Channel 3 volume right", 0, 3)]
		public byte Channel3VolumeRight => channel3.VolumeRight;
		[Port("REG_SND_CH3_VOL", 0x08A)]
		[BitDescription("Channel 3 volume left", 4, 7)]
		public byte Channel3VolumeLeft => channel3.VolumeLeft;
		[Port("REG_SND_CH4_VOL", 0x08B)]
		[BitDescription("Channel 4 volume right", 0, 3)]
		public byte Channel4VolumeRight => channel4.VolumeRight;
		[Port("REG_SND_CH4_VOL", 0x08B)]
		[BitDescription("Channel 4 volume left", 4, 7)]
		public byte Channel4VolumeLeft => channel4.VolumeLeft;
		[Port("REG_SND_SWEEP_VALUE", 0x08C)]
		[BitDescription("Channel 3 sweep value")]
		public sbyte Channel3SweepValue => channel3.SweepValue;
		[Port("REG_SND_SWEEP_TIME", 0x08D)]
		[BitDescription("Channel 3 sweep time", 0, 4)]
		public byte Channel3SweepTime => channel3.SweepTime;
		[Port("REG_SND_NOISE", 0x08E)]
		[BitDescription("Channel 4 noise mode", 0, 2)]
		public byte Channel4NoiseMode => channel4.NoiseMode;
		[Port("REG_SND_NOISE", 0x08E)]
		[BitDescription("Is channel 4 noise enabled?", 4)]
		public bool Channel4NoiseEnable => channel4.NoiseEnable;
		[Port("REG_SND_WAVE_BASE", 0x08F)]
		[BitDescription("Wavetable base address")]
		[Format("X4", 6)]
		public byte WaveTableBase => waveTableBase;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 1 enabled?", 0)]
		public bool Channel1IsEnabled => channel1.IsEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 2 enabled?", 1)]
		public bool Channel2IsEnabled => channel2.IsEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 3 enabled?", 2)]
		public bool Channel3IsEnabled => channel3.IsEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 4 enabled?", 3)]
		public bool Channel4IsEnabled => channel4.IsEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 2 in voice mode?", 5)]
		public bool Channel2IsVoiceEnabled => channel2.IsVoiceEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 3 in sweep mode?", 6)]
		public bool Channel3IsSweepEnabled => channel3.IsSweepEnabled;
		[Port("REG_SND_CTRL", 0x090)]
		[BitDescription("Is channel 4 in noise mode?", 7)]
		public bool Channel4IsNoiseEnabled => channel4.IsNoiseEnabled;
		[Port("REG_SND_OUTPUT", 0x091)]
		[BitDescription("Is speaker enabled?", 0)]
		public bool SpeakerEnable => speakerEnable;
		[Port("REG_SND_OUTPUT", 0x091)]
		[BitDescription("Speaker PWM volume bitshift", 1, 2)]
		public byte SpeakerVolumeShift => speakerVolumeShift;
		[Port("REG_SND_OUTPUT", 0x091)]
		[BitDescription("Are headphones enabled?", 3)]
		public bool HeadphoneEnable => headphoneEnable;
		[Port("REG_SND_OUTPUT", 0x091)]
		[BitDescription("Are headphones connected?", 7)]
		public bool HeadphonesConnected => headphonesConnected;
		[Port("REG_SND_RANDOM", 0x092, 0x093)]
		[BitDescription("Current noise LFSR value", 0, 14)]
		[Format("X4")]
		public ushort Channel4NoiseLfsr => channel4.NoiseLfsr;
		[Port("REG_SND_VOLUME", 0x09E)]
		[BitDescription("Master volume level", 0, 1)]
		public byte MasterVolume => masterVolume;
	}
}

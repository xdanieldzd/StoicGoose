using System;
using System.Collections.Generic;

using StoicGoose.Emulation.Machines;
using StoicGoose.Interface.Attributes;
using StoicGoose.WinForms;

namespace StoicGoose.Emulation.Sound
{
	public abstract partial class SoundControllerCommon : IComponent
	{
		/* http://daifukkat.su/docs/wsman/#hw_sound */

		public abstract byte MaxMasterVolume { get; }

		readonly int sampleRate, numOutputChannels;

		protected readonly SoundChannel1 channel1 = default;
		protected readonly SoundChannel2 channel2 = default;
		protected readonly SoundChannel3 channel3 = default;
		protected readonly SoundChannel4 channel4 = default;

		protected readonly List<short> mixedSampleBuffer;
		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		readonly double clockRate, refreshRate;
		readonly int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int cycleCount;

		readonly MemoryReadDelegate memoryReadDelegate;

		/* REG_SND_WAVE_BASE */
		protected byte waveTableBase;
		/* REG_SND_OUTPUT */
		protected bool speakerEnable, headphoneEnable;

		[ImGuiRegister(0x091, "REG_SND_OUTPUT")]
		[ImGuiBitDescription("Are headphones connected?", 7)]
		public bool HeadphonesConnected { get; protected set; } = false; // read-only

		protected byte speakerVolumeShift;

		/* REG_SND_VOLUME */
		protected byte masterVolume;

		public byte MasterVolume => masterVolume;

		public SoundControllerCommon(MemoryReadDelegate memoryRead, int rate, int outChannels)
		{
			memoryReadDelegate = memoryRead;

			sampleRate = rate;
			numOutputChannels = outChannels;

			channel1 = new SoundChannel1((a) => memoryReadDelegate((uint)((waveTableBase << 6) + (0 << 4) + a)));
			channel2 = new SoundChannel2((a) => memoryReadDelegate((uint)((waveTableBase << 6) + (1 << 4) + a)));
			channel3 = new SoundChannel3((a) => memoryReadDelegate((uint)((waveTableBase << 6) + (2 << 4) + a)));
			channel4 = new SoundChannel4((a) => memoryReadDelegate((uint)((waveTableBase << 6) + (3 << 4) + a)));

			mixedSampleBuffer = new List<short>();

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
			HeadphonesConnected = true; // for stereo sound
			speakerVolumeShift = 0;
			masterVolume = MaxMasterVolume;
		}

		public void Shutdown()
		{
			//
		}

		public void Step(int clockCyclesInStep)
		{
			cycleCount += clockCyclesInStep;

			for (int i = 0; i < clockCyclesInStep; i++)
				StepChannels();

			if (cycleCount >= cyclesPerSample)
			{
				GenerateSample();
				cycleCount -= cyclesPerSample;
			}

			if (mixedSampleBuffer.Count >= (samplesPerFrame * numOutputChannels))
			{
				OnEnqueueSamples(new EnqueueSamplesEventArgs(mixedSampleBuffer.ToArray()));
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

		public abstract void GenerateSample();

		public void FlushSamples() => mixedSampleBuffer.Clear();

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);
	}
}

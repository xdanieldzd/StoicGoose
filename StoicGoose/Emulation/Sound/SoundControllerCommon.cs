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

		protected const int numChannels = 4;

		readonly int sampleRate, numOutputChannels;

		protected readonly ISoundChannel[] channels = new ISoundChannel[numChannels];

		protected readonly List<short> mixedSampleBuffer;
		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples;
		public void OnEnqueueSamples(EnqueueSamplesEventArgs e) { EnqueueSamples?.Invoke(this, e); }

		readonly double clockRate, refreshRate;
		readonly int samplesPerFrame, cyclesPerFrame, cyclesPerSample;
		int cycleCount;

		public int MasterVolume { get; protected set; } = 0;

		public abstract int MaxMasterVolume { get; }

		int masterVolumeChange;

		readonly MemoryReadDelegate memoryReadDelegate;

		public delegate byte WaveTableReadDelegate(ushort address);

		/* REG_SND_WAVE_BASE */
		protected byte waveTableBase;
		/* REG_SND_OUTPUT */
		protected bool speakerEnable, headphoneEnable;

		[ImGuiRegister(0x091, "REG_SND_OUTPUT")]
		[ImGuiBitDescription("Are headphones connected?", 7)]
		public bool HeadphonesConnected { get; protected set; } = false; // read-only

		protected byte speakerVolumeShift;

		/* REG_SND_9E */
		protected byte unknown9E;

		public SoundControllerCommon(MemoryReadDelegate memoryRead, int rate, int outChannels)
		{
			memoryReadDelegate = memoryRead;

			sampleRate = rate;
			numOutputChannels = outChannels;

			channels[0] = new Wave(false, (a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (0 << 4) + a)); });
			channels[1] = new Voice((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (1 << 4) + a)); });
			channels[2] = new Wave(true, (a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (2 << 4) + a)); });
			channels[3] = new Noise((a) => { return memoryReadDelegate((uint)((waveTableBase << 6) + (3 << 4) + a)); });

			mixedSampleBuffer = new List<short>();

			clockRate = MachineCommon.CpuClock;
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

			MasterVolume = MaxMasterVolume;
			masterVolumeChange = -1;

			ResetRegisters();
		}

		public virtual void ResetRegisters()
		{
			waveTableBase = 0;
			speakerEnable = headphoneEnable = false;
			HeadphonesConnected = true; // for stereo sound
			speakerVolumeShift = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void ToggleMasterVolume()
		{
			if (masterVolumeChange != -1) return;

			masterVolumeChange = MasterVolume - 1;
			if (masterVolumeChange < 0) masterVolumeChange = MaxMasterVolume;
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
				OnEnqueueSamples(new EnqueueSamplesEventArgs(numChannels, mixedSampleBuffer.ToArray()));
				FlushSamples();

				if (masterVolumeChange != -1)
				{
					MasterVolume = masterVolumeChange;
					masterVolumeChange = -1;
				}
			}
		}

		public abstract void StepChannels();

		public abstract void GenerateSample();

		public void FlushSamples() => mixedSampleBuffer.Clear();

		public abstract byte ReadRegister(ushort register);
		public abstract void WriteRegister(ushort register, byte value);
	}
}

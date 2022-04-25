using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Audio.OpenAL;

namespace StoicGoose.GLWindow
{
	public class SoundHandler : IDisposable
	{
		const int numBuffers = 4, maxQueueLength = 2;

		public int SampleRate { get; private set; } = 0;
		public int NumChannels { get; private set; } = 0;

		readonly ALContext context = default;
		readonly int source = -1;
		readonly int[] buffers = new int[numBuffers];

		readonly Queue<short[]> sampleQueue = new();
		short[] lastSamples = new short[512];

		float volume = 1.0f;

		public SoundHandler(int sampleRate, int numChannels)
		{
			SampleRate = sampleRate;
			NumChannels = numChannels;

			var device = ALC.OpenDevice(null);
			context = ALC.CreateContext(device, new ALContextAttributes());
			ALC.MakeContextCurrent(context);

			source = AL.GenSource();

			buffers = AL.GenBuffers(numBuffers);
			for (int i = 0; i < buffers.Length; i++) GenerateBuffer(buffers[i]);
			AL.SourcePlay(source);
		}

		~SoundHandler()
		{
			Dispose();
		}

		public void Dispose()
		{
			foreach (var buffer in buffers.Where(x => AL.IsBuffer(x)))
				AL.DeleteBuffer(buffer);

			GC.SuppressFinalize(this);
		}

		public void Update()
		{
			AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int buffersProcessed);
			while (buffersProcessed-- > 0)
			{
				int buffer = AL.SourceUnqueueBuffer(source);
				if (buffer != 0)
					GenerateBuffer(buffer);
			}

			AL.GetSource(source, ALGetSourcei.SourceState, out int state);
			if ((ALSourceState)state != ALSourceState.Playing)
				AL.SourcePlay(source);
		}

		public void SetVolume(float value)
		{
			AL.Source(source, ALSourcef.Gain, volume = value);
		}

		public void SetMute(bool mute)
		{
			AL.Source(source, ALSourcef.Gain, mute ? 0.0f : volume);
		}

		public void EnqueueSamples(short[] samples)
		{
			if (sampleQueue.Count > maxQueueLength)
			{
				var samplesToDrop = sampleQueue.Count - maxQueueLength;
				for (int i = 0; i < samplesToDrop; i++)
					if (sampleQueue.Count != 0)
						sampleQueue.Dequeue();
			}

			sampleQueue.Enqueue(samples.ToArray());
		}

		public void ClearSampleBuffer()
		{
			sampleQueue.Clear();

			if (lastSamples != null)
			{
				for (int i = 0; i < lastSamples.Length; i++)
					lastSamples[i] = 0;
			}
		}

		private void GenerateBuffer(int buffer)
		{
			if (sampleQueue.Count > 0)
				lastSamples = sampleQueue.Dequeue();

			if (lastSamples != null)
			{
				AL.BufferData(buffer, ALFormat.Stereo16, lastSamples, SampleRate);
				AL.SourceQueueBuffer(source, buffer);
			}
		}
	}
}

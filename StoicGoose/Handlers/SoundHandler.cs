﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;

using StoicGoose.IO;
using StoicGoose.WinForms;

namespace StoicGoose.Handlers
{
	public class SoundHandler
	{
		readonly static string threadName = $"{Application.ProductName}Audio";

		Thread thread = default;
		volatile bool threadRunning = false, threadPaused = false;

		volatile bool isPauseRequested = false, newPauseState = false;

		public bool IsRunning => threadRunning;
		public bool IsPaused => threadPaused;

		const int numBuffers = 4;

		public int SampleRate { get; private set; } = 0;
		public int NumChannels { get; private set; } = 0;

		public int MaxQueueLength { get; set; } = 2;

		ALContext context = default;
		int source = -1, filter = -1;
		int[] buffers = new int[numBuffers];

		readonly Queue<short[]> sampleQueue = new();
		short[] lastSamples = new short[512];

		float volume = 1.0f;

		WaveFileWriter wavWriter = default;

		public bool IsRecording { get; private set; }

		public SoundHandler(int sampleRate, int numChannels)
		{
			SampleRate = sampleRate;
			NumChannels = numChannels;

			InitializeOpenAL();
			InitializeFilters();

			Startup();
		}

		private void InitializeOpenAL()
		{
			var device = ALC.OpenDevice(null);
			context = ALC.CreateContext(device, new ALContextAttributes());
			ALC.MakeContextCurrent(context);

			source = AL.GenSource();
		}

		private void InitializeFilters()
		{
			filter = EFX.GenFilter();
			EFX.Filter(filter, FilterInteger.FilterType, (int)FilterType.Lowpass);
			EFX.Filter(filter, FilterFloat.LowpassGain, 0.9f);
			EFX.Filter(filter, FilterFloat.LowpassGainHF, 0.75f);
		}

		public void Startup()
		{
			buffers = AL.GenBuffers(numBuffers);
			for (int i = 0; i < buffers.Length; i++) GenerateBuffer(buffers[i]);
			AL.SourcePlay(source);

			threadRunning = true;
			threadPaused = false;

			thread = new Thread(ThreadMainLoop) { Name = threadName, Priority = ThreadPriority.BelowNormal, IsBackground = true };
			thread.Start();
		}

		public void Pause()
		{
			isPauseRequested = true;
			newPauseState = true;
		}

		public void Unpause()
		{
			isPauseRequested = true;
			newPauseState = false;
		}

		public void Shutdown()
		{
			threadRunning = false;
			threadPaused = false;

			thread?.Join();

			foreach (var buffer in buffers.Where(x => AL.IsBuffer(x)))
				AL.DeleteBuffer(buffer);

			sampleQueue.Clear();
		}

		public void BeginRecording()
		{
			wavWriter = new(SampleRate, NumChannels);

			IsRecording = true;
		}

		public void SaveRecording(string filename)
		{
			using var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			wavWriter.Save(stream);

			IsRecording = false;
		}

		public void CancelRecording()
		{
			IsRecording = false;
		}

		private void ThreadMainLoop()
		{
			while (true)
			{
				if (!threadRunning) break;

				if (isPauseRequested)
				{
					threadPaused = newPauseState;
					isPauseRequested = false;
				}

				if (!threadPaused)
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
			}
		}

		public void SetVolume(float value)
		{
			AL.Source(source, ALSourcef.Gain, volume = value);
		}

		public void SetMute(bool mute)
		{
			AL.Source(source, ALSourcef.Gain, mute ? 0.0f : volume);
		}

		public void SetLowPassFilter(bool enable)
		{
			AL.Source(source, ALSourcei.EfxDirectFilter, enable ? filter : 0);
		}

		public void EnqueueSamples(object sender, EnqueueSamplesEventArgs e)
		{
			if (sampleQueue.Count > MaxQueueLength)
			{
				var samplesToDrop = sampleQueue.Count - MaxQueueLength;
				for (int i = 0; i < samplesToDrop; i++)
					if (sampleQueue.Count != 0)
						sampleQueue.Dequeue();
			}

			sampleQueue.Enqueue(e.Samples.ToArray());

			if (IsRecording)
				wavWriter.Write(e.Samples);
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

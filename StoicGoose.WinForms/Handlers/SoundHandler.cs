using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using OpenTK.Audio.OpenAL;

using StoicGoose.Common.Utilities;

namespace StoicGoose.WinForms.Handlers
{
	public class SoundHandler
	{
		readonly static string threadName = $"{Application.ProductName}Audio";

		public bool IsAvailable { get; private set; } = false;

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

		public SoundHandler(int sampleRate, int numChannels)
		{
			SampleRate = sampleRate;
			NumChannels = numChannels;

			try
			{
				InitializeOpenAL();
				InitializeFilters();

				IsAvailable = true;
			}
			catch (DllNotFoundException e)
			{
				if (e.TargetSite.Module.Assembly == typeof(AL).Assembly)
				{
					var filename = Regex.Match(e.Message, "'(.*?)'").Groups[1].Value;
					var message = !string.IsNullOrEmpty(filename) ?
						$"The OpenAL library '{filename}' was not found.\n\nSound emulation will be disabled." :
						"An OpenAL implementation was not found.\n\nSound emulation will be disabled.";

					MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

					IsAvailable = false;
				}
				else
					throw;
			}

			Startup();

			Log.WriteEvent(IsAvailable ? LogSeverity.Information : LogSeverity.Error, this, $"Initialization {(IsAvailable ? "successful" : "failed")}.");
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
			filter = ALC.EFX.GenFilter();
			ALC.EFX.Filter(filter, FilterInteger.FilterType, (int)FilterType.Lowpass);
			ALC.EFX.Filter(filter, FilterFloat.LowpassGain, 0.9f);
			ALC.EFX.Filter(filter, FilterFloat.LowpassGainHF, 0.75f);
		}

		public void Startup()
		{
			if (IsAvailable)
			{
				buffers = AL.GenBuffers(numBuffers);
				for (int i = 0; i < buffers.Length; i++) GenerateBuffer(buffers[i]);
				AL.SourcePlay(source);
			}

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

			if (IsAvailable)
				foreach (var buffer in buffers.Where(x => AL.IsBuffer(x)))
					AL.DeleteBuffer(buffer);

			sampleQueue.Clear();
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

				if (IsAvailable && !threadPaused)
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
			if (!IsAvailable) return;

			AL.Source(source, ALSourcef.Gain, volume = value);
		}

		public void SetMute(bool mute)
		{
			if (!IsAvailable) return;

			AL.Source(source, ALSourcef.Gain, mute ? 0.0f : volume);
		}

		public void SetLowPassFilter(bool enable)
		{
			if (!IsAvailable) return;

			AL.Source(source, ALSourcei.EfxDirectFilter, enable ? filter : 0);
		}

		public void EnqueueSamples(short[] samples)
		{
			if (sampleQueue.Count > MaxQueueLength)
			{
				var samplesToDrop = sampleQueue.Count - MaxQueueLength;
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

			if (!IsAvailable) return;

			if (lastSamples != null)
			{
				AL.BufferData(buffer, ALFormat.Stereo16, lastSamples, SampleRate);
				AL.SourceQueueBuffer(source, buffer);
			}
		}
	}
}

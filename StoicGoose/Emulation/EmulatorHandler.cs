using System;
using System.Diagnostics;
using System.Threading;

using StoicGoose.DataStorage;
using StoicGoose.Emulation.Machines;
using StoicGoose.WinForms;

namespace StoicGoose.Emulation
{
	public class EmulatorHandler
	{
		readonly WonderSwan emulator;

		Thread emulationThread;

		volatile bool emulationThreadRunning = false, emulationThreadPaused = false;
		volatile bool limitFps = true, isResetRequested = false, isPauseToggleRequested = false;
		int currentFps;

		public bool IsRunning => emulationThreadRunning;
		public bool IsPaused => emulationThreadPaused;

		public bool LimitFps
		{
			get { return limitFps; }
			set { limitFps = value; }
		}

		public int CurrentFps => currentFps;

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { emulator.RenderScreen += value; }
			remove { emulator.RenderScreen -= value; }
		}

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { emulator.EnqueueSamples += value; }
			remove { emulator.EnqueueSamples -= value; }
		}

		public event EventHandler<PollInputEventArgs> PollInput
		{
			add { emulator.PollInput += value; }
			remove { emulator.PollInput -= value; }
		}

		public event EventHandler<StartOfFrameEventArgs> StartOfFrame
		{
			add { emulator.StartOfFrame += value; }
			remove { emulator.StartOfFrame -= value; }
		}

		public event EventHandler<EventArgs> EndOfFrame
		{
			add { emulator.EndOfFrame += value; }
			remove { emulator.EndOfFrame -= value; }
		}

		public EmulatorHandler()
		{
			emulator = new WonderSwan();
		}

		public void Startup()
		{
			emulationThreadRunning = true;
			emulationThreadPaused = false;

			emulator.Reset();

			emulationThread = new Thread(ThreadMainLoop) { Name = "StoicGooseEmulation", Priority = ThreadPriority.AboveNormal, IsBackground = false };
			emulationThread.Start();
		}

		public void Reset()
		{
			isResetRequested = true;
		}

		public void Pause()
		{
			isPauseToggleRequested = true;
		}

		public void Shutdown()
		{
			emulationThreadRunning = false;
			emulationThreadPaused = false;

			emulationThread?.Join();
		}

		public void LoadBootstrap(byte[] data) => emulator.LoadBootstrap(data);
		public bool IsBootstrapLoaded => emulator.IsBootstrapLoaded;

		public void LoadInternalEeprom(byte[] data) => emulator.LoadInternalEeprom(data);

		public void LoadRom(byte[] data) => emulator.LoadRom(data);
		public void LoadSaveData(byte[] data) => emulator.LoadSaveData(data);

		public byte[] GetInternalEeprom() => emulator.GetInternalEeprom();
		public byte[] GetSaveData() => emulator.GetSaveData();

		public ObjectStorage GetMetadata() => WonderSwan.Metadata;

		public byte[] GetInternalRam() => emulator.GetInternalRam();

		private void ThreadMainLoop()
		{
			// TODO: FPS limiter & counter taken from Essgee -- can be improved?
			// FIXME: FPS doesn't seem to be limiting to 75hz properly? seems to be too fast?


			var stopWatch = Stopwatch.StartNew();

			TimeSpan accumulatedTime = TimeSpan.Zero, lastStartTime = TimeSpan.Zero, lastEndTime = TimeSpan.Zero;

			var frameCounter = 0;
			var sampleTimespan = TimeSpan.FromSeconds(0.5);

			var targetElapsedTime = TimeSpan.FromTicks((long)Math.Round(TimeSpan.TicksPerSecond / emulator.GetRefreshRate()));


			//var intervalMs = 1000.0 / emulator.GetRefreshRate();

			while (true)
			{
				// break if thread should stop

				if (!emulationThreadRunning)
					break;

				// check for requested changes (reset, pause)

				if (isResetRequested)
				{
					emulator.Reset();
					isResetRequested = false;
				}

				if (isPauseToggleRequested)
				{
					emulationThreadPaused = !emulationThreadPaused;
					isPauseToggleRequested = false;
				}

				// run emulation & limit fps if requested
				/*
				if (!emulationThreadPaused)
				{
					var startTime = stopWatch.Elapsed.TotalMilliseconds;
					emulator.RunFrame();
					var endTime = stopWatch.Elapsed.TotalMilliseconds;
					if (!SpinWait.SpinUntil(() => !limitFps && (stopWatch.Elapsed.TotalMilliseconds - startTime) >= intervalMs, (int)intervalMs))
						Thread.Sleep((int)(endTime - startTime));

					//Console.WriteLine($"{intervalMs}, {(stopWatch.Elapsed.TotalMilliseconds - startTime)}");
				}
				*/










				var startTime = stopWatch.Elapsed;
				if (!emulationThreadPaused)
				{
					if (limitFps)
					{
						var elapsedTime = (startTime - lastStartTime);
						lastStartTime = startTime;

						if (elapsedTime < targetElapsedTime)
						{
							accumulatedTime += elapsedTime;

							while (accumulatedTime >= targetElapsedTime)
							{
								emulator.RunFrame();
								frameCounter++;

								accumulatedTime -= targetElapsedTime;
							}
						}
					}
					else
					{
						emulator.RunFrame();
						frameCounter++;
					}

					if ((stopWatch.Elapsed - lastEndTime) >= sampleTimespan)
					{
						currentFps = (int)(frameCounter * 1000.0 / sampleTimespan.TotalMilliseconds);
						frameCounter = 0;
						lastEndTime = stopWatch.Elapsed;
					}
				}
				else
				{
					lastEndTime = stopWatch.Elapsed;
				}
			}
		}
	}
}

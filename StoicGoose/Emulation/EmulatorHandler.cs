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

		public bool IsRunning => emulationThreadRunning;
		public bool IsPaused { get; private set; }

		public bool LimitFps
		{
			get { return limitFps; }
			set { limitFps = value; }
		}

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

			IsPaused = !emulationThreadPaused;
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
			var stopWatch = Stopwatch.StartNew();

			var interval = 1000.0 / emulator.GetRefreshRate();
			var lastTime = 0.0;

			while (true)
			{
				// break if thread should stop
				if (!emulationThreadRunning) break;

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
				if (!emulationThreadPaused)
				{
					if (limitFps)
					{
						while ((stopWatch.Elapsed.TotalMilliseconds - lastTime) < interval)
							Thread.Sleep(0);

						lastTime += interval;
					}
					else
						lastTime = stopWatch.Elapsed.TotalMilliseconds;

					emulator.RunFrame();
				}
				else
				{
					lastTime = stopWatch.Elapsed.TotalMilliseconds;
				}
			}
		}
	}
}

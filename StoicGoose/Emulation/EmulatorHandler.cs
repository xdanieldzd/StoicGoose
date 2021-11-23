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
		const string threadName = "StoicGooseEmulation";

		Thread thread = default;
		volatile bool threadRunning = false, threadPaused = false;

		volatile bool isResetRequested = false;
		volatile bool isPauseRequested = false, newPauseState = false;
		volatile bool isFpsLimiterChangeRequested = false, limitFps = true, newLimitFps = false;

		public bool IsRunning => threadRunning;
		public bool IsPaused => threadPaused;

		readonly IMachine machine = null;

		public event EventHandler<RenderScreenEventArgs> RenderScreen
		{
			add { machine.RenderScreen += value; }
			remove { machine.RenderScreen -= value; }
		}

		public event EventHandler<EnqueueSamplesEventArgs> EnqueueSamples
		{
			add { machine.EnqueueSamples += value; }
			remove { machine.EnqueueSamples -= value; }
		}

		public event EventHandler<PollInputEventArgs> PollInput
		{
			add { machine.PollInput += value; }
			remove { machine.PollInput -= value; }
		}

		public event EventHandler<StartOfFrameEventArgs> StartOfFrame
		{
			add { machine.StartOfFrame += value; }
			remove { machine.StartOfFrame -= value; }
		}

		public event EventHandler<EventArgs> EndOfFrame
		{
			add { machine.EndOfFrame += value; }
			remove { machine.EndOfFrame -= value; }
		}

		public EmulatorHandler(Type machineType)
		{
			machine = Activator.CreateInstance(machineType) as IMachine;
			machine.Initialize();
		}

		public void Startup()
		{
			machine.Reset();

			threadRunning = true;
			threadPaused = false;

			thread = new Thread(ThreadMainLoop) { Name = threadName, Priority = ThreadPriority.AboveNormal, IsBackground = false };
			thread.Start();
		}

		public void Reset()
		{
			isResetRequested = true;
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
		}

		public void SetFpsLimiter(bool value)
		{
			isFpsLimiterChangeRequested = true;
			newLimitFps = value;
		}

		public void LoadBootstrap(byte[] data) => machine.LoadBootstrap(data);
		public bool IsBootstrapLoaded => machine.IsBootstrapLoaded;

		public void LoadInternalEeprom(byte[] data) => machine.LoadInternalEeprom(data);

		public void LoadRom(byte[] data) => machine.LoadRom(data);
		public void LoadSaveData(byte[] data) => machine.LoadSaveData(data);

		public byte[] GetInternalEeprom() => machine.GetInternalEeprom();
		public byte[] GetSaveData() => machine.GetSaveData();

		public ObjectStorage Metadata => machine.Metadata;

		public byte[] GetInternalRam() => machine.GetInternalRam();

		private void ThreadMainLoop()
		{
			var stopWatch = Stopwatch.StartNew();
			var interval = 1000.0 / machine.GetRefreshRate();
			var lastTime = 0.0;

			while (true)
			{
				if (!threadRunning) break;

				if (isResetRequested)
				{
					machine.Reset();
					stopWatch.Restart();
					lastTime = 0.0;

					isResetRequested = false;
				}

				if (isPauseRequested)
				{
					threadPaused = newPauseState;
					isPauseRequested = false;
				}

				if (isFpsLimiterChangeRequested)
				{
					limitFps = newLimitFps;
					isFpsLimiterChangeRequested = false;
				}

				if (!threadPaused)
				{
					if (limitFps)
					{
						while ((stopWatch.Elapsed.TotalMilliseconds - lastTime) < interval)
							Thread.Sleep(0);

						lastTime += interval;
					}
					else
						lastTime = stopWatch.Elapsed.TotalMilliseconds;

					machine.RunFrame();
				}
				else
					lastTime = stopWatch.Elapsed.TotalMilliseconds;
			}
		}
	}
}

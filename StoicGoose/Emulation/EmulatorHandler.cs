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

		readonly WonderSwan emulator = null;

		readonly Stopwatch stopWatch = Stopwatch.StartNew();
		readonly double interval = 0.0;
		double lastTime = 0.0;

		bool isResetRequested = false, isFpsLimiterChangeRequested = false;
		bool limitFps = true, newLimitFps = false;

		public bool IsRunning => ThreadManager.GetState(threadName).HasFlag(ThreadState.Running);
		public bool IsPaused => ThreadManager.GetState(threadName).HasFlag(ThreadState.Paused);

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
			interval = 1000.0 / emulator.GetRefreshRate();
		}

		public void Startup()
		{
			emulator.Reset();

			ThreadManager.Create(threadName, ThreadPriority.AboveNormal, false, ThreadMainLoop, ThreadMainPaused);
		}

		public void Reset()
		{
			isResetRequested = true;
		}

		public void Pause()
		{
			ThreadManager.Pause(threadName);
		}

		public void Unpause()
		{
			ThreadManager.Unpause(threadName);
		}

		public void Shutdown()
		{
			ThreadManager.Stop(threadName);
		}

		public void SetFpsLimiter(bool value)
		{
			isFpsLimiterChangeRequested = true;
			newLimitFps = value;
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
			if (isResetRequested)
			{
				emulator.Reset();
				isResetRequested = false;
			}

			if (isFpsLimiterChangeRequested)
			{
				limitFps = newLimitFps;
				isFpsLimiterChangeRequested = false;
			}

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

		private void ThreadMainPaused()
		{
			lastTime = stopWatch.Elapsed.TotalMilliseconds;
		}
	}
}

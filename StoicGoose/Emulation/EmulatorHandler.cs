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

		readonly IMachine machine = null;

		readonly Stopwatch stopWatch = new();
		readonly double interval = 0.0;
		double lastTime = 0.0;

		bool isResetRequested = false, isFpsLimiterChangeRequested = false;
		bool limitFps = true, newLimitFps = false;

		public bool IsRunning => ThreadManager.GetState(threadName).HasFlag(ThreadState.Running);
		public bool IsPaused => ThreadManager.GetState(threadName).HasFlag(ThreadState.Paused);

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
			interval = 1000.0 / machine.GetRefreshRate();
		}

		public void Startup()
		{
			machine.Reset();
			stopWatch.Restart();

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
			if (isResetRequested)
			{
				machine.Reset();
				stopWatch.Restart();
				lastTime = 0.0;

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

			machine.RunFrame();
		}

		private void ThreadMainPaused()
		{
			lastTime = stopWatch.Elapsed.TotalMilliseconds;
		}
	}
}

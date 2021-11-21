using System;
using System.Collections.Generic;
using System.Threading;

namespace StoicGoose
{
	[Flags]
	public enum ThreadState
	{
		Stopped = 0,
		Running = 1 << 0,
		Paused = 1 << 1,
		Stopping = 1 << 2,
		DoesNotExist = 1 << 3
	}

	public static class ThreadManager
	{
		readonly static Dictionary<string, ThreadInfo> threadList = new();

		public static void Create(string name, ThreadPriority priority, bool isBackground, Action mainLoopWhenRunning, Action mainLoopWhenPaused)
		{
			var thread = new Thread(new ParameterizedThreadStart((param) =>
			{
				var name = param as string;

				while (true)
				{
					if (threadList[name].State.HasFlag(ThreadState.Stopping))
						break;

					if (!threadList[name].State.HasFlag(ThreadState.Paused))
						mainLoopWhenRunning?.Invoke();
					else
						mainLoopWhenPaused?.Invoke();
				}

				threadList[name].State &= ~ThreadState.Stopping;
				threadList[name].State |= ThreadState.Stopped;
			}))
			{ Name = name, Priority = priority, IsBackground = isBackground };
			thread.Start(name);

			if (threadList.ContainsKey(name))
			{
				if (threadList[name].State != ThreadState.Stopped)
				{
					threadList[name].State = ThreadState.Stopping;
					threadList[name].Thread.Join();
				}

				threadList.Remove(name);
			}

			threadList.Add(name, new ThreadInfo(thread));
		}

		public static ThreadState GetState(string name)
		{
			if (threadList.ContainsKey(name))
				return threadList[name].State;
			else
				return ThreadState.DoesNotExist;
		}

		public static void Stop()
		{
			foreach (var (name, _) in threadList)
				Stop(name);
		}

		public static void Stop(string name)
		{
			if (threadList.ContainsKey(name))
			{
				threadList[name].State = ThreadState.Stopping;
				threadList[name].Thread.Join();
			}
		}

		public static void Pause()
		{
			foreach (var (name, _) in threadList)
				Pause(name);
		}

		public static void Pause(string name)
		{
			if (threadList.ContainsKey(name))
				threadList[name].State |= ThreadState.Paused;
		}

		public static void Unpause()
		{
			foreach (var (name, _) in threadList)
				Unpause(name);
		}

		public static void Unpause(string name)
		{
			if (threadList.ContainsKey(name))
				threadList[name].State &= ~ThreadState.Paused;
		}
	}

	internal class ThreadInfo
	{
		public Thread Thread { get; internal set; } = null;
		public ThreadState State { get; internal set; } = ThreadState.Stopped;

		public ThreadInfo(Thread thread)
		{
			Thread = thread;
			State = ThreadState.Running;
		}
	}
}

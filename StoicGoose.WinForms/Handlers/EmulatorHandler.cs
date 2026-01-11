using StoicGoose.Core.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Handlers
{
    public class EmulatorHandler
    {
        readonly static string threadName = $"{Application.ProductName}Emulation";

        Thread thread = default;
        volatile bool threadRunning = false, threadPaused = false;

        volatile bool isResetRequested = false;
        volatile bool isPauseRequested = false, newPauseState = false;
        volatile bool isFpsLimiterChangeRequested = false, limitFps = true, newLimitFps = false;

        public bool IsRunning => threadRunning;
        public bool IsPaused => threadPaused;

        public IMachine Machine { get; } = default;

        public EmulatorHandler(Type machineType)
        {
            Machine = Activator.CreateInstance(machineType) as IMachine;
            Machine.Initialize();
        }

        public void Startup()
        {
            Machine.Reset();

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

            Machine.Shutdown();
        }

        public void SetFpsLimiter(bool value)
        {
            isFpsLimiterChangeRequested = true;
            newLimitFps = value;
        }

        private void ThreadMainLoop()
        {
            var stopWatch = Stopwatch.StartNew();
            var interval = 1000.0 / Machine.RefreshRate;
            var lastTime = 0.0;

            while (true)
            {
                if (!threadRunning) break;

                if (isResetRequested)
                {
                    Machine.Reset();
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

                    Machine.RunFrame();
                }
                else
                    lastTime = stopWatch.Elapsed.TotalMilliseconds;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Commons.Local
{
    internal abstract class ServiceHealthChecker
    {
        protected readonly ILog Log;

        protected ServiceHealthChecker(ILog log)
        {
            Log = log;
        }

        public bool WaitStarted(TimeSpan timeout)
        {
            Log.Debug("Waiting for the service to start..");

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (IsStarted())
                {
                    Log.Debug($"Service has successfully started in {sw.Elapsed.TotalSeconds:0.##} second(s).");
                    return true;
                }

                Thread.Sleep(0.5.Seconds());
            }

            Log.Warn($"Service hasn't started in {timeout}.");
            return false;
        }

        protected abstract bool IsStarted();
    }
}
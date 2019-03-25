using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;

namespace Vostok.Commons.Process
{
    [PublicAPI]
    public abstract class ProcessWrapper
    {
        private readonly ILog log;
        private readonly string displayName;
        private readonly WindowsProcessKillJob processKillJob;
        private volatile System.Diagnostics.Process process;
        
        protected abstract string FileName { get; }
        protected abstract string Arguments { get; }
        protected abstract string WorkingDirectory { get; }

        protected ProcessWrapper(ILog log, string displayName)
        {
            this.log = log.ForContext(displayName);
            this.displayName = displayName;

            processKillJob = Environment.OSVersion.Platform == PlatformID.Unix ? null : new WindowsProcessKillJob(this.log);
        }

        /// <summary>
        /// Returns whether this process is currently running.
        /// </summary>
        public bool IsRunning => process?.HasExited == false;

        public virtual void Start()
        {
            if (IsRunning)
                return;

            var processStartInfo = new ProcessStartInfo(FileName)
            {
                Arguments = Arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = WorkingDirectory
            };

            process = new System.Diagnostics.Process
            {
                StartInfo = processStartInfo
            };

            if (!process.Start())
                throw new Exception($"Failed to start process of {displayName}.");

            Task.Run(
                async () =>
                {
                    while (!process.StandardError.EndOfStream)
                        log.Error(await process.StandardError.ReadLineAsync().ConfigureAwait(false));
                });

            EnsureSuccessfullyStarted();

            processKillJob?.AddProcess(process);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                log.Debug($"Stopping {displayName}..");

                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch
                {
                    // ignored
                }
            }

            process = null;
        }

        protected abstract void EnsureSuccessfullyStarted();
    }
}
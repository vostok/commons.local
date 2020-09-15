using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Windows;
using Vostok.Logging.Abstractions;

namespace Vostok.Commons.Local.Helpers
{
    [PublicAPI]
    public abstract class ProcessWrapper
    {
        protected readonly ILog Log;
        protected volatile Process Process;
        private readonly string displayName;
        private readonly bool captureStandardOutput;
        private readonly WindowsProcessKillJob processKillJob;

        protected ProcessWrapper(ILog log, string displayName, bool captureStandardOutput = false)
        {
            Log = log.ForContext(displayName);
            this.displayName = displayName;
            this.captureStandardOutput = captureStandardOutput;

            processKillJob = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new WindowsProcessKillJob() : null;
        }

        /// <summary>
        /// Returns whether this process is currently running.
        /// </summary>
        public bool IsRunning => Process?.HasExited == false;

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

            Process = new System.Diagnostics.Process
            {
                StartInfo = processStartInfo
            };

            if (!Process.Start())
                throw new Exception($"Failed to start process of {displayName}.");

            if (captureStandardOutput)
            {
                Task.Run(
                    async () =>
                    {
                        while (!Process.StandardOutput.EndOfStream)
                            Log.Info(await Process.StandardOutput.ReadLineAsync().ConfigureAwait(false));
                    });
            }

            Task.Run(
                async () =>
                {
                    while (!Process.StandardError.EndOfStream)
                        Log.Error(await Process.StandardError.ReadLineAsync().ConfigureAwait(false));
                });

            processKillJob?.AddProcess(Process);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                Log.Debug($"Stopping {displayName}..");

                try
                {
                    Process.Kill();
                    Process.WaitForExit();
                }
                catch
                {
                    // ignored
                }
            }

            Process = null;
        }

        protected abstract string FileName { get; }
        protected abstract string Arguments { get; }
        protected abstract string WorkingDirectory { get; }
    }
}
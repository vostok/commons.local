using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Windows;
using Vostok.Commons.Local.Helpers;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Commons.Local
{
    [PublicAPI]
    public class ShellRunner
    {
        private readonly ShellRunnerSettings settings;
        private readonly ILog log;
        private readonly ProcessStartInfo startInfo;
        private readonly WindowsProcessKillJob processKillJob;
        private volatile Process process;
        private volatile Task readStandardOutputTask;
        private volatile Task readStandardErrorTask;

        public ShellRunner([NotNull] ShellRunnerSettings settings, [CanBeNull] ILog log)
        {
            log = (log ?? LogProvider.Get()).ForContext<ShellRunner>();

            this.settings = settings;
            this.log = log;

            startInfo = new ProcessStartInfo
            {
                FileName = OsHelper.GetCommandLineName(),
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                Arguments = OsHelper.GetCommandLineArguments() + "\"" + settings.CommandWithArguments + "\"",
                WorkingDirectory = settings.WorkingDirectory
            };

            processKillJob = OsHelper.TryCreateWindowsProcessKillJob();
        }

        /// <summary>
        /// Returns whether this process is currently running.
        /// </summary>
        public bool IsRunning => process?.HasExited == false;

        public void Start()
        {
            if (IsRunning)
                return;

            log.Info("Starting '{Command}' in '{Directory}'..", settings.CommandWithArguments, settings.WorkingDirectory);

            var stopwatch = Stopwatch.StartNew();
            process = new Process {StartInfo = startInfo};

            if (!process.Start())
                throw new Exception($"Failed to start '{settings.CommandWithArguments}' process.");

            readStandardOutputTask = Task.Run(
                async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                        log.Info(await process.StandardOutput.ReadLineAsync().ConfigureAwait(false));

                    log.Info("Finished '{Command}' in {Elapsed}.", settings.CommandWithArguments, stopwatch.Elapsed.ToPrettyString());
                });

            readStandardErrorTask = Task.Run(
                async () =>
                {
                    while (!process.StandardError.EndOfStream)
                        log.Error(await process.StandardError.ReadLineAsync().ConfigureAwait(false));
                });

            processKillJob?.AddProcess(process);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                log.Info("Stopping '{Command}'..", settings.CommandWithArguments);

                try
                {
                    log.Info("Killing '{Command}'..", settings.CommandWithArguments);
                    process.Kill(true);
                    log.Info("Waiting '{Command}'..", settings.CommandWithArguments);
                    process.WaitForExit();

                    log.Info("Reading '{Command}'..", settings.CommandWithArguments);
                    readStandardOutputTask.GetAwaiter().GetResult();
                    readStandardErrorTask.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to stop '{Command}'.", settings.CommandWithArguments);
                }
            }

            process = null;
        }

        public async Task RunAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Start();

            using (var timeoutCancellation = new CancellationTokenSource())
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellation.Token))
            {
                var timeoutTask = Task.Delay(timeout, linkedCancellation.Token);

                var finished = await Task.WhenAny(readStandardOutputTask, readStandardErrorTask, timeoutTask)
                    .ConfigureAwait(false);

                if (finished == timeoutTask)
                {
                    Stop();
                    log.Error("Failed to complete '{Command}' within {Timeout} timeout.", settings.CommandWithArguments, timeout.ToPrettyString());
                    throw new TimeoutException($"Failed to complete '{settings.CommandWithArguments}' within {timeout.ToPrettyString()} timeout");
                }

                await readStandardOutputTask.ConfigureAwait(false);
                await readStandardErrorTask.ConfigureAwait(false);
            }
        }
    }
}
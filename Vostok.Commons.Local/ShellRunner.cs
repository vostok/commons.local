using System;
using System.Diagnostics;
using System.Text;
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
        private volatile Stopwatch stopwatch;

        public ShellRunner([NotNull] ShellRunnerSettings settings, [CanBeNull] ILog log)
        {
            log = log ?? LogProvider.Get();

            this.settings = settings;
            this.log = log;

            startInfo = new ProcessStartInfo
            {
                FileName = settings.Command,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                Arguments = settings.Arguments ?? string.Empty,
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

            log.Info("Starting '{Command}' command with '{Arguments}' arguments in '{Directory}' directory..", settings.Command, settings.Arguments, settings.WorkingDirectory);

            settings.EnvironmentSetup?.Invoke(startInfo.Environment);
            stopwatch = Stopwatch.StartNew();
            process = new Process {StartInfo = startInfo};

            try
            {
                if (!process.Start())
                    throw new Exception("Failed to start process (process.Start returned false).");
            }
            catch (Exception error)
            {
                log.Error(error, "Failed to start '{Command}' command with '{Arguments}' arguments in '{Directory}' directory.", settings.Command, settings.Arguments, settings.WorkingDirectory);
                process = null;
                throw new Exception($"Failed to start '{settings.Command}' command process.", error);
            }

            readStandardOutputTask = Task.Run(
                async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var outputMessage = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                        log.Info(outputMessage);
                        settings.StandardOutputHandler?.Invoke(outputMessage);
                    }
                });

            readStandardErrorTask = Task.Run(
                async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        var error = await process.StandardError.ReadLineAsync().ConfigureAwait(false);
                        log.Error(error);
                        settings.StandardErrorHandler?.Invoke(error);
                    }
                });

            processKillJob?.AddProcess(process);

            log.Info("Successfully started '{Command}' command.", settings.Command, settings.WorkingDirectory);
        }

        public void Stop()
        {
            if (IsRunning)
            {
                log.Info("Killing '{Command}' command process {Process}..", settings.Command, process.Id);

                try
                {
#if NETCOREAPP3_1_OR_GREATER
                    process.Kill(true);
#else
                    process.Kill();
#endif

                    process.WaitForExit();

                    readStandardOutputTask.GetAwaiter().GetResult();
                    readStandardErrorTask.GetAwaiter().GetResult();
                    stopwatch.Stop();

                    log.Info("Successfully killed '{Command}' command in {Elapsed}.", settings.Command, stopwatch.Elapsed.ToPrettyString());
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed to stop '{Command}' command.", settings.Command);
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

                timeoutCancellation.Cancel();

                if (finished == timeoutTask)
                {
                    Stop();
                    log.Error("Failed to complete '{Command}' command within {Timeout} timeout.", settings.Command, timeout.ToPrettyString());
                    throw new TimeoutException($"Failed to complete '{settings.Command}' command within {timeout.ToPrettyString()} timeout");
                }

                process.WaitForExit();

                await readStandardOutputTask.ConfigureAwait(false);
                await readStandardErrorTask.ConfigureAwait(false);
                stopwatch.Stop();
            }

            if (process.ExitCode != 0)
            {
                log.Error("Command '{Command}' failed in {Elapsed} with exit code = {ExitCode}.", settings.Command, stopwatch.Elapsed.ToPrettyString(), process.ExitCode);
                throw new Exception($"Command '{settings.Command}' completed in {stopwatch.Elapsed.ToPrettyString()} with exit code = {process.ExitCode}.");
            }

            log.Info("Command '{Command}' successfully completed in {Elapsed}.", settings.Command, stopwatch.Elapsed.ToPrettyString(), process.ExitCode);
        }

        public async Task<bool> TrySendMessageAsync(string message)
        {
            if (!IsRunning)
            {
                log.Error("Cant send '{Message}' message to not running command.", message);
                return false;
            }

            try
            {
                await process.StandardInput.WriteLineAsync(message).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                log.Error(e, "Fail to send '{Message}' message.", message);
                return false;
            }
        }
    }
}
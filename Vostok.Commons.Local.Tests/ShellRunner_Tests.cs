using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Time;
using Vostok.Logging.Console;

namespace Vostok.Commons.Local.Tests
{
    [TestFixture]
    internal class ShellRunner_Tests
    {
        [Test]
        public void Start_Stop_should_work()
        {
            var command = GetTimeoutCommandWithArgs(100, out var args);
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = args
                },
                new SynchronousConsoleLog());

            runner.Start();

            Thread.Sleep(5.Seconds());

            runner.Stop();
        }

        [Test]
        public void Run_should_work()
        {
            var command = GetTimeoutCommandWithArgs(2, out var args);
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = args
                },
                new SynchronousConsoleLog());

            runner.RunAsync(5.Seconds(), CancellationToken.None)
                .Wait(5.Seconds())
                .Should()
                .BeTrue();
        }

        [Test]
        public void Run_should_stop_after_timeout()
        {
            var command = GetTimeoutCommandWithArgs(100, out var args);
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = args
                },
                new SynchronousConsoleLog());

            new Action(() => runner.Run(5.Seconds(), CancellationToken.None))
                .Should()
                .Throw<TimeoutException>();
        }

        [Test]
        public void Run_should_throw_on_start_fail()
        {
            var runner = new ShellRunner(
                new ShellRunnerSettings("asdf"),
                new SynchronousConsoleLog());

            new Action(() => runner.Run(5.Seconds(), CancellationToken.None))
                .Should()
                .Throw<Exception>();
        }

        [Test]
        public void Run_should_throw_on_non_zero_exit_code()
        {
            var command = GetTimeoutCommandWithArgs(100, out _);
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = "asdf"
                },
                new SynchronousConsoleLog());

            new Action(() => runner.Run(5.Seconds(), CancellationToken.None))
                .Should()
                .Throw<Exception>();
        }

        [Test]
        [Platform("Win,Linux")]
        public void Run_should_setup_environment()
        {
            var variables = new List<string>();
            var command = GetPrintEnvironmentCommandWithArgs(out var args);
            var varName = "TEST_VAR";
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = args,
                    StandardOutputHandler = s => variables.Add(s),
                    EnvironmentSetup = e => e.Add(varName, "kontur")
                },
                new SynchronousConsoleLog());

            //note check not changed for current process
            System.Environment.GetEnvironmentVariables()[varName].Should().BeNull();

            runner
                .RunAsync(5.Seconds(), CancellationToken.None)
                .Wait(10.Seconds())
                .Should()
                .BeTrue();

            variables.Should().Contain("TEST_VAR=kontur");
            System.Environment.GetEnvironmentVariables()[varName].Should().BeNull();
        }

        private static string GetPrintEnvironmentCommandWithArgs(out string args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                args = "/c set";
                return "cmd";
            }

            args = null;
            return "printenv";
        }

        private static string GetTimeoutCommandWithArgs(int timeout, out string args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                args = $"localhost -n {timeout}";
                return "ping";
            }

            args = $"{timeout}";
            return "sleep";
        }
    }
}
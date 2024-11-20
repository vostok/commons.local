using System;
using System.Collections.Generic;
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
            var runner = new ShellRunner(
                new ShellRunnerSettings("ping")
                {
                    Arguments = GetPingArgs(100)
                },
                new SynchronousConsoleLog());

            runner.Start();

            Thread.Sleep(5.Seconds());

            runner.Stop();

            Thread.Sleep(5.Seconds());
        }

        [Test]
        public void Run_should_work()
        {
            var runner = new ShellRunner(
                new ShellRunnerSettings("ping")
                {
                    Arguments = GetPingArgs(2)
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
            var runner = new ShellRunner(
                new ShellRunnerSettings("ping")
                {
                    Arguments = GetPingArgs(100)
                },
                new SynchronousConsoleLog());

            runner.RunAsync(5.Seconds(), CancellationToken.None)
                .Wait(10.Seconds())
                .Should()
                .BeTrue();
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
            var runner = new ShellRunner(
                new ShellRunnerSettings("ping")
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
            var command = GetCommandWithArgs(out var args);
            var runner = new ShellRunner(
                new ShellRunnerSettings(command)
                {
                    Arguments = args,
                    StandardOutputHandler = s => variables.Add(s),
                    EnvironmentSetup = e => e.Add("TEST_VAR", "kontur")
                },
                new SynchronousConsoleLog());

            runner
                .RunAsync(5.Seconds(), CancellationToken.None)
                .Wait(10.Seconds())
                .Should()
                .BeTrue();

            variables.Should().Contain("TEST_VAR=kontur");
        }

        private static string GetCommandWithArgs(out string args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                args = "/c set";
                return "cmd";
            }

            args = null;
            return "printenv";
        }

        private static string GetPingArgs(int limit) =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? $"localhost -n {limit}"
                : $"localhost -c {limit}";
    }
}
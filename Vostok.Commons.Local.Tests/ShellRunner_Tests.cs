using System;
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
                    Arguments = "localhost -n 100"
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
                    Arguments = "localhost -n 2"
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
                    Arguments = "localhost -n 100"
                },
                new SynchronousConsoleLog());

            runner.RunAsync(5.Seconds(), CancellationToken.None)
                .ContinueWith(_ => {})
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
    }
}
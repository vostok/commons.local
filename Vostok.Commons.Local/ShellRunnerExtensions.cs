using System;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Commons.Local
{
    [PublicAPI]
    public static class ShellRunnerExtensions
    {
        public static void Run(this ShellRunner shellRunner, TimeSpan timeout, CancellationToken cancellationToken) =>
            shellRunner.RunAsync(timeout, cancellationToken).GetAwaiter().GetResult();
    }
}
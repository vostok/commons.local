using System;
using JetBrains.Annotations;
using Vostok.Commons.Environment;
using Vostok.Commons.Time;

namespace Vostok.Commons.Local
{
    public class ShellRunnerSettings
    {
        public ShellRunnerSettings([NotNull] string commandWithArguments)
        {
            CommandWithArguments = commandWithArguments ?? throw new ArgumentNullException(nameof(commandWithArguments));
        }

        [NotNull]
        public string CommandWithArguments { get; }

        [NotNull]
        public string WorkingDirectory { get; set; } = EnvironmentInfo.BaseDirectory;
    }
}
using JetBrains.Annotations;
using Vostok.Commons.Environment;

namespace Vostok.Commons.Local
{
    public class ShellRunnerSettings
    {
        public ShellRunnerSettings([NotNull] string command)
        {
            Command = command;
        }

        [NotNull]
        public string Command { get; }

        [CanBeNull]
        public string Arguments { get; set; }

        [NotNull]
        public string WorkingDirectory { get; set; } = EnvironmentInfo.BaseDirectory;
    }
}
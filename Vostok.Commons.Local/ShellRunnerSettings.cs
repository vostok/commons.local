using System;
using System.Collections.Generic;
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

        /// <summary>
        /// This handler is called synchronously on the StandardOutput read stream. Be careful with long-term processing.
        /// </summary>
        [CanBeNull]
        public Action<string> StandardOutputHandler { get; set; }

        /// <summary>
        /// This handler is called synchronously on the StandardOutput read stream. Be careful with long-term processing.
        /// </summary>
        [CanBeNull]
        public Action<string> StandardErrorHandler { get; set; }

        /// <summary>
        /// Called before process start, can be used to modify new process environment variables.
        /// </summary>
        [CanBeNull]
        public Action<IDictionary<string, string>> EnvironmentSetup { get; set; }
    }
}
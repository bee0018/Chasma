using System.Diagnostics;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Class describing utility methods for interacting with the system shell.
    /// </summary>
    public static class ShellUtility
    {
        /// <summary>
        /// Gets the process to execute standard shell commands based on the operating system.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workingDirectory">The directory to execute commands in.</param>
        /// <returns>The process for executing generic shell commands.</returns>
        public static Process GetStandardShell(string command, string workingDirectory)
        {
            string shell;
            string shellArgs;
            if (OperatingSystem.IsWindows())
            {
                shell = "cmd.exe";
                shellArgs = $"/c {command}";
            }
            else
            {
                shell = "/bin/sh";
                shellArgs = $"-c \"{command}\"";
            }

            ProcessStartInfo processInfo = new(shell, shellArgs)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return new() { StartInfo = processInfo };
        }

        /// <summary>
        /// Gets the process to execute shell commands based on the operating system.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="filePath">The file to be executing shell commands on.</param>
        /// <param name="workingDirectory">The working directory to do the commands in.</param>
        /// <returns>The process to execute shell commands.</returns>
        public static Process GetFileProcessingShell(string command, string filePath, string workingDirectory)
        {
            string shell;
            string shellArgs;
            if (OperatingSystem.IsWindows())
            {
                shell = "cmd.exe";
                shellArgs = $"/c {command} {EscapeArgument(filePath)}";
            }
            else
            {
                shell = "/bin/sh";
                shellArgs = $"-c \"{command} {EscapeArgument(filePath)}\"";
            }

            ProcessStartInfo processInfo = new(shell, shellArgs)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return new() { StartInfo = processInfo };
        }

        /// <summary>
        /// Escapes the file path properly for Unix shells.
        /// </summary>
        /// <param name="filePath">The filepath.</param>
        /// <returns>The properly formatted filepath.</returns>
        public static string EscapeArgument(string filePath) =>
            OperatingSystem.IsWindows()
            ? $"\"{filePath.Replace("\"", "\\\"")}\""
            : $"'{filePath.Replace("'", "'\\''")}'";
    }
}

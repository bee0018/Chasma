using System.Diagnostics;
using System.Text;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Class describing utility methods for interacting with the system shell.
    /// </summary>
    public static class ShellUtility
    {
        #region Environment Variables

        /// <summary>
        /// Gets the environment variable name for Git terminal prompt.
        /// </summary>
        private static string GitTerminalPrompt = "GIT_TERMINAL_PROMPT";

        /// <summary>
        /// Gets the environment variable name for Git editor.
        /// </summary>
        private static string GitEditor = "GIT_EDITOR";

        /// <summary>
        /// Gets the environment variable name for Git optional locks.
        /// </summary>
        private static string GitOptionalLocks = "GIT_OPTIONAL_LOCKS";

        #endregion

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
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    [GitTerminalPrompt] = "0",
                    [GitEditor] = "true",
                    [GitOptionalLocks] = "0"
                },
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
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                EnvironmentVariables =
                {
                    [GitTerminalPrompt] = "0",
                    [GitEditor] = "true",
                    [GitOptionalLocks] = "0"
                },
            };
            Process process = new() { StartInfo = processInfo };
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            return process;
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

        /// <summary>
        /// Tries to execute a shell command and captures any error messages that occur during execution.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workingDirectory">The working directory to execute commands in.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the command executed successfully, false otherwise.</returns>
        public static bool TryExecuteShellCommand(string command, string workingDirectory, out string errorMessage)
        {
            try
            {
                errorMessage = string.Empty;
                using Process process = GetStandardShell(command, workingDirectory);
                process.Start();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    errorMessage = $"Command '{command}' failed with error: {error}\n";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"An exception occurred while executing command: {ex.Message}";
                return false;
            }
        }
    }
}

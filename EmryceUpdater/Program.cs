using Serilog;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Emryce");
string logPath = Path.Combine(appDataDirectory, "logs", "emryce-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File(
    logPath,
    rollingInterval: RollingInterval.Day,
    fileSizeLimitBytes: 10 * 1024 * 1024,
    retainedFileCountLimit: 7,
    rollOnFileSizeLimit: true))
    .CreateLogger();

if (args.Length != 4)
{
    string errorPrompt = "Invalid arguments given. Should be the following:" +
        "\n\t- File path to the .zip/.tar file" +
        "\n\t- File path where the parent process is running" +
        "\n\t- Emryce executable full file path" +
        "\n\t- Parent process ID";
    Log.Error(errorPrompt);
    SafeExit(-1);
}

string artifactFilePath = args[0];
string processDirectory = args[1];
string parentExecutablePath = args[2];
int processId = int.Parse(args[3]);

Log.Information("Initializing update sequence...");

try
{
    Process parentProcess = Process.GetProcessById(processId);
    Log.Information("Waiting for main application to close...");
    parentProcess.WaitForExit(10000);
}
catch (ArgumentException)
{
    // Process ID is already gone
}

Thread.Sleep(3000);
string zipDirectory = Path.GetDirectoryName(artifactFilePath);
string folderName = Path.GetFileNameWithoutExtension(artifactFilePath);
string extractPath = Path.Combine(zipDirectory, $"extracted_{Guid.NewGuid()}");
try
{
    Log.Information("Performing file extraction...");
    if (OperatingSystem.IsWindows())
    {
        ZipFile.ExtractToDirectory(artifactFilePath, extractPath, overwriteFiles: true);
    }
    else if (OperatingSystem.IsLinux())
    {
        TarFile.ExtractToDirectory(artifactFilePath, extractPath, overwriteFiles: true);
    }
    else
    {
        Log.Error("OS is not supported and cannot deploy update!");
        SafeExit(-5);
    }

    Log.Information("Download extraction is complete. Applying file updates!");
    TotalFolderSwap(extractPath, processDirectory);
    Log.Information("Update applied successfully.");

    Log.Information("Cleaning up installation temporary files...");
    File.Delete(artifactFilePath);
}
catch (Exception ex)
{
    Log.Error("Error applying update: {Message}", ex.Message);
    SafeExit(-2);
}
finally
{
    try
    {
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }
    }
    catch (Exception ex)
    {
        Log.Warning("Could not delete temporary extraction folder: {Message}", ex.Message);
    }
}

if (OperatingSystem.IsLinux())
{
    ProcessStartInfo chmodInfo = new()
    {
        FileName = "chmod",
        Arguments = $"+x \"{parentExecutablePath}\"",
        RedirectStandardOutput = true,
        UseShellExecute = false
    };

    using (Process proccess = Process.Start(chmodInfo))
    {
        proccess?.WaitForExit();
    }

    Log.Information("Successfully restored Linux execution permissions.");
}

if (File.Exists(parentExecutablePath))
{
    Log.Information("Launching updated application: {Path}", parentExecutablePath);
    Process.Start(new ProcessStartInfo
    {
        FileName = parentExecutablePath,
        UseShellExecute = true
    });
}

SafeExit(0);

#region Private Methods

/// <summary>
/// Safely exits the program.
/// </summary>
/// <param name="exitCode">The exit code.</param>
static void SafeExit(int exitCode)
{
    Log.CloseAndFlush();
    Environment.Exit(exitCode);
}

/// <summary>
/// Performs a total folder swap from the extracted location to the working directory location.
/// </summary>
/// <param name="extractPath">The build artifact path.</param>
/// <param name="processDirectory">The working directory where the parent application is running.</param>
static void TotalFolderSwap(string extractPath, string processDirectory)
{
    string trueSourcePath = extractPath;
    string[] subDirectories = Directory.GetDirectories(extractPath);
    if (Directory.GetFiles(extractPath).Length == 0 && subDirectories.Length == 1)
    {
        trueSourcePath = subDirectories[0];
        Log.Information("Detected nested archive layout. Adjusting payload root source path to: {Path}", trueSourcePath);
    }

    if (!Directory.Exists(processDirectory))
    {
        Directory.CreateDirectory(processDirectory);
    }

    string backupConfigFilePath = Path.Combine(Path.GetTempPath(), $"emryce_config_backup_{Guid.NewGuid()}.xml");
    bool backupCreated = false;
    string existingConfigFilePath = Path.Combine(processDirectory, "config.xml");
    if (File.Exists(existingConfigFilePath))
    {
        try
        {
            string xmlText = File.ReadAllText(existingConfigFilePath);
            File.WriteAllText(backupConfigFilePath, xmlText, Encoding.UTF8);
            backupCreated = true;
        }
        catch (Exception ex)
        {
            Log.Warning("Could not backup existing config.xml: {Message}.", ex.Message);
        }
    }

    Log.Information("Copying new deployment files into target directory: {Target}", processDirectory);
    CopyFolderContents(trueSourcePath, processDirectory, processDirectory);
    if (backupCreated && File.Exists(backupConfigFilePath))
    {
        try
        {
            string newConfigFilePath = Path.Combine(processDirectory, "config.xml");
            CopyFileWithRetry(backupConfigFilePath, newConfigFilePath);
            File.Delete(backupConfigFilePath);
            Log.Information("Configuration file successfully preserved.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to restore config.xml backup: {Message}", ex.Message);
        }
    }
}

/// <summary>
/// Copies the folder contents from one to another.
/// </summary>
/// <param name="sourceFolder">The source installation folder.</param>
/// <param name="destFolder">The working directory where the parent application is running.</param>
/// <param name="rootProcessDirectory">The root process directory.</param>
static void CopyFolderContents(string sourceFolder, string destFolder, string rootProcessDirectory)
{
    string[] files = Directory.GetFiles(sourceFolder);
    foreach (string file in files)
    {
        // Skip updater and target root configs
        string name = Path.GetFileName(file);
        if (name.Equals("EmryceUpdater.exe", StringComparison.OrdinalIgnoreCase) ||
           (name.Equals("config.xml", StringComparison.OrdinalIgnoreCase) && destFolder.Equals(rootProcessDirectory, StringComparison.OrdinalIgnoreCase)))
        {
            continue;
        }

        string dest = Path.Combine(destFolder, name);
        CopyFileWithRetry(file, dest);
    }

    string[] folders = Directory.GetDirectories(sourceFolder);
    foreach (string folder in folders)
    {
        string name = Path.GetFileName(folder);
        string dest = Path.Combine(destFolder, name);

        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
        }

        CopyFolderContents(folder, dest, rootProcessDirectory);
    }
}

/// <summary>
/// Copies the file from one source to another.
/// </summary>
/// <param name="sourcePath">The source path of the file.</param>
/// <param name="destPath">The destination path of the file.</param>
static void CopyFileWithRetry(string sourcePath, string destPath)
{
    int maxRetries = 5;
    int delayMs = 1500;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            return;
        }
        catch (IOException) when (i < maxRetries - 1)
        {
            Log.Warning("File {FileName} is locked. Retrying in {Delay}ms... (Attempt {Current}/{Max})",
                Path.GetFileName(destPath), delayMs, i + 1, maxRetries);
            Thread.Sleep(delayMs);
        }
    }
}

#endregion
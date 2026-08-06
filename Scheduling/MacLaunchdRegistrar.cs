using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using GooglePhotosUploader.Config;

namespace GooglePhotosUploader.Scheduling;

/// <summary>Creates/removes a user LaunchAgent with a StartCalendarInterval per time slot.</summary>
[SupportedOSPlatform("macos")]
public partial class MacLaunchdRegistrar : IBackgroundScheduler
{
    private const string Label = "com.jorgediegocrespo.googlephotosuploader";

    [LibraryImport("libc")]
    private static partial uint getuid();

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{Label}.plist");

    public async Task RegisterAsync(IReadOnlyList<ScheduleEntry> entries, string executablePath)
    {
        var logsDir = Path.Combine(AppConfig.AppDataFolder, "logs");
        Directory.CreateDirectory(logsDir);
        Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);

        var plist = BuildPlist(entries, executablePath, logsDir);
        await File.WriteAllTextAsync(PlistPath, plist);

        // In case it was already loaded with an earlier version of the plist.
        await RunLaunchctlAsync($"bootout {GuiDomain()} \"{PlistPath}\"", ignoreFailure: true);
        await RunLaunchctlAsync($"bootstrap {GuiDomain()} \"{PlistPath}\"", ignoreFailure: false);
    }

    public async Task UnregisterAsync()
    {
        await RunLaunchctlAsync($"bootout {GuiDomain()} \"{PlistPath}\"", ignoreFailure: true);

        if (File.Exists(PlistPath))
        {
            File.Delete(PlistPath);
        }
    }

    public async Task<bool> IsRegisteredAsync()
    {
        var (exitCode, _) = await RunLaunchctlAsync($"print {GuiDomain()}/{Label}", ignoreFailure: true);
        return exitCode == 0;
    }

    private static string GuiDomain() => $"gui/{getuid()}";

    private static string BuildPlist(IReadOnlyList<ScheduleEntry> entries, string executablePath, string logsDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">""");
        sb.AppendLine("<plist version=\"1.0\"><dict>");
        sb.AppendLine($"  <key>Label</key><string>{Label}</string>");
        sb.AppendLine("  <key>ProgramArguments</key><array>");
        sb.AppendLine($"    <string>{executablePath}</string>");
        sb.AppendLine("    <string>--run-scheduled</string>");
        sb.AppendLine("  </array>");
        sb.AppendLine("  <key>StartCalendarInterval</key><array>");
        foreach (var entry in entries)
        {
            sb.AppendLine("    <dict>");
            sb.AppendLine($"      <key>Weekday</key><integer>{(int)entry.DayOfWeek}</integer>");
            sb.AppendLine($"      <key>Hour</key><integer>{entry.Hour}</integer>");
            sb.AppendLine($"      <key>Minute</key><integer>{entry.Minute}</integer>");
            sb.AppendLine("    </dict>");
        }
        sb.AppendLine("  </array>");
        sb.AppendLine($"  <key>StandardOutPath</key><string>{Path.Combine(logsDir, "launchd-stdout.log")}</string>");
        sb.AppendLine($"  <key>StandardErrorPath</key><string>{Path.Combine(logsDir, "launchd-stderr.log")}</string>");
        sb.AppendLine("</dict></plist>");
        return sb.ToString();
    }

    private static async Task<(int ExitCode, string Output)> RunLaunchctlAsync(string arguments, bool ignoreFailure)
    {
        var psi = new ProcessStartInfo("launchctl", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !ignoreFailure)
        {
            throw new InvalidOperationException($"launchctl {arguments} failed ({process.ExitCode}): {error}");
        }

        return (process.ExitCode, output);
    }
}

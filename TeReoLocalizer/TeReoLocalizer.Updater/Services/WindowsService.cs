using System;
using System.Diagnostics;

namespace TeReoLocalizer.Updater.Services;

public static class WindowsService
{
    public static void ForceKill(Process proc)
    {
        // Accessing ProcessName could throw an exception if the process has already been killed.
        string processName = string.Empty;
        try { processName = proc.ProcessName; } catch (Exception ex) { }

        // ProcessId can be accessed after the process has been killed but we'll do this safely anyways.
        int pId = 0;
        try { pId = proc.Id; } catch (Exception ex) { }

        // Will only work if started by this instance of the dll.
        try { proc.Kill(); } catch (Exception ex) { }

        // Fallback to task kill
        if (pId > 0)
        {
            ProcessStartInfo taskKilPsi = new ProcessStartInfo("taskkill")
            {
                Arguments = $"/pid {proc.Id} /T /F",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            Process taskKillProc = Process.Start(taskKilPsi);
            taskKillProc.WaitForExit();
            
            string taskKillOutput = taskKillProc.StandardOutput.ReadToEnd(); // Contains success
            string taskKillErrorOutput = taskKillProc.StandardError.ReadToEnd();
        }

        // Fallback to wmic delete process.
        if (!string.IsNullOrEmpty(processName))
        {
            // https://stackoverflow.com/a/38757852/591285
            ProcessStartInfo wmicPsi = new ProcessStartInfo("wmic")
            {
                Arguments = $@"process where ""name='{processName}.exe'"" delete",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            Process wmicProc = Process.Start(wmicPsi);
            wmicProc.WaitForExit();
            string wmicOutput = wmicProc.StandardOutput.ReadToEnd(); // Contains success
            string wmicErrorOutput = wmicProc.StandardError.ReadToEnd();
        }
    }
}
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TeReoLocalizer.Launcher;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            string relativePath = Path.Combine("TeReoLocalizer", "TeReoLocalizer.exe");
            string fullPath = Path.Combine(baseDirectory, relativePath);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                fullPath = fullPath.Replace("\\", "/");
            }
            
            if (!File.Exists(fullPath))
            {
                return;
            }
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.Combine(baseDirectory, "TeReoLocalizer")
            };

            using Process? process = Process.Start(startInfo);
        }
        catch
        {
            
        }
    }
}
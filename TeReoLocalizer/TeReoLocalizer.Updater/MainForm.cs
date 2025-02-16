using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Windows.Forms;
using Octokit;

namespace TeReoLocalizer.Updater
{
    public partial class MainForm : Form
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly GitHubClient githubClient = new GitHubClient(new ProductHeaderValue("TeReoUpdater"));

        readonly string tempPath;
        readonly string parentPath;

        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);

            tempPath = Path.Combine(Path.GetTempPath(), "TeReoUpdate");
  
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            parentPath = Directory.GetParent(Directory.GetParent(currentPath).FullName).FullName;
        }

        async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                button1.Visible = false;

                try
                {
                    label1.Text = "Získávání informací o poslední verzi";

                    string localVersionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reoVersion.txt");
                    if (!File.Exists(localVersionPath))
                    {
                        progressBar1.Visible = false;
                        label1.Text = "Není možné určit aktuální verzi, protože chybí soubor reoVersion.txt";
                        return;
                    }

                    string localVersion = File.ReadAllText(localVersionPath).Trim();
                    Version currentVersion = Version.Parse(localVersion);

                    IReadOnlyList<Release> releases = await githubClient.Repository.Release.GetAll("lofcz", "tereo");
                    Release latestRelease = releases[0];

                    ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(a => a.Name.StartsWith("TeReoLocalizer-v") && a.Name.EndsWith(".zip"));

                    if (asset == null)
                    {
                        MessageBox.Show("Nebyl nalezen ZIP soubor ke stažení.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string upstreamVersionStr = asset.Name.Replace("TeReoLocalizer-v", "").Replace(".zip", "");
                    Version upstreamVersion = Version.Parse(upstreamVersionStr);

                    if (currentVersion >= upstreamVersion)
                    {
                        progressBar1.Visible = false;
                        label1.Text = $"Reo je aktuální, verze {localVersion}";
                        return;
                    }

                    string updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.zip");
                    
                    if (File.Exists(updatePath))
                    {
                        try
                        {
                            File.Delete(updatePath);
                        }
                        catch (Exception fe)
                        {
                            
                        }
                    }

                    label1.Text = $"Probíhá stažení aktualizace {localVersion}\u2192{upstreamVersionStr}";

                    using (HttpResponseMessage response = await httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long totalBytes = response.Content.Headers.ContentLength ?? -1L;

                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = File.Create(updatePath))
                        {
                            byte[] buffer = new byte[8192];
                            long bytesRead = 0L;
                            int count;

                            while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, count);
                                bytesRead += count;

                                if (totalBytes != -1)
                                {
                                    int percentage = (int)((bytesRead * 100) / totalBytes);
                                    progressBar1.Value = percentage;
                                }
                            }
                        }
                    }

                    label1.Text = "Probíhá rozbalení aktualizace";

                    try
                    {
                        if (Directory.Exists(tempPath))
                        {
                            try
                            {
                                Directory.Delete(tempPath, true);
                            }
                            catch (Exception e2)
                            {
                            }
                        }

                        Directory.CreateDirectory(tempPath);

                        ZipFile.ExtractToDirectory(updatePath, tempPath);

                        if (!CanUpdateFiles())
                        {
                            button1.Visible = true;
                            label1.Text = "Některé procesy blokují aktualizaci. Ukončete je a stiskněte tlačítko pokračovat.";
                            return;
                        }

                        PerformUpdate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Chyba při aktualizaci: {ex.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Došlo k chybě při aktualizaci: {ex.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception eOuter)
            {
                MessageBox.Show($"Došlo k neošetřené chybě při aktualizaci: {eOuter.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        bool CanUpdateFiles()
        {
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                try
                {
                    string processPath = process.MainModule?.FileName;
                    if (string.IsNullOrEmpty(processPath)) continue;

                    if (processPath.StartsWith(parentPath) && !processPath.EndsWith("updater.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        label1.Text = $"Proces {process.ProcessName} blokuje dokončení aktualizace, ukončete proces a stiskněte tlačítko pokračovat";
                        return false;
                    }
                }
                catch
                {
                    
                }
            }

            return true;
        }

        void PerformUpdate()
        {
            try
            {
                string logPath = Path.Combine(Path.GetTempPath(), "update_log.txt");
                using (StreamWriter log = new StreamWriter(logPath, true))
                {
                    log.WriteLine($"\n=== Update started at {DateTime.Now} ===");

                    // backup reoboot.json
                    string reoBootPath = Path.Combine(parentPath, "reoBoot.json");
                    string reoBootBackup = null;
                    if (File.Exists(reoBootPath))
                    {
                        reoBootBackup = Path.Combine(Path.GetTempPath(), "reoBoot.json.backup");
                        File.Copy(reoBootPath, reoBootBackup, true);
                        log.WriteLine($"Backed up reoBoot.json to: {reoBootBackup}");
                    }
                    
                    // Rename the running updater.exe to updater.bak
                    string currentUpdaterPath = Path.Combine(parentPath, "updater", "updater.exe");
                    string backupUpdaterPath = Path.Combine(parentPath, "updater", "updater.bak");

                    if (File.Exists(currentUpdaterPath))
                    {
                        File.Move(currentUpdaterPath, backupUpdaterPath);
                        log.WriteLine($"Renamed current updater to backup: {backupUpdaterPath}");
                    }

                    // Copy the new updater.exe from the temp directory to the target directory
                    string newUpdaterPath = Path.Combine(tempPath, "updater", "updater.exe");
                    if (File.Exists(newUpdaterPath))
                    {
                        File.Copy(newUpdaterPath, currentUpdaterPath, true);
                        log.WriteLine($"Copied new updater to target: {currentUpdaterPath}");
                    }

                    // Copy all other files from the temp directory to the parent directory
                    foreach (string file in Directory.GetFiles(tempPath))
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(parentPath, fileName);
                        try
                        {
                            if (File.Exists(destFile))
                            {
                                File.SetAttributes(destFile, FileAttributes.Normal);
                                File.Delete(destFile);
                            }

                            File.Copy(file, destFile);
                            log.WriteLine($"Copied file: {fileName} -> {destFile}");
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"Error copying {fileName}: {ex.Message}");
                            throw;
                        }
                    }

                    // Copy all subdirectories except the updater directory
                    foreach (string dir in Directory.GetDirectories(tempPath))
                    {
                        string dirName = new DirectoryInfo(dir).Name;
                        if (dirName.Equals("updater", StringComparison.OrdinalIgnoreCase))
                        {
                            log.WriteLine($"Skipping updater directory: {dir}");
                            continue;
                        }

                        string targetPath = Path.Combine(parentPath, dirName);
                        log.WriteLine($"Processing directory: {dirName}");

                        if (Directory.Exists(targetPath))
                        {
                            try
                            {
                                Directory.Delete(targetPath, true);
                                log.WriteLine($"Deleted existing directory: {targetPath}");
                            }
                            catch (Exception e)
                            {
                                log.WriteLine($"Deleted directory failed: {targetPath} {e.Message}");
                            }
                        }

                        CopyDirectoryWithLogging(dir, targetPath, true, log);
                    }
                    
                    // restore reoboot.json
                    if (reoBootBackup != null && File.Exists(reoBootBackup))
                    {
                        File.Copy(reoBootBackup, reoBootPath, true);
                        log.WriteLine($"Restored reoBoot.json from backup");
                        try
                        {
                            File.Delete(reoBootBackup);
                            log.WriteLine("Deleted reoBoot.json backup");
                        }
                        catch (Exception ex)
                        {
                            log.WriteLine($"Failed to delete reoBoot.json backup: {ex.Message}");
                        }
                    }

                    // Create a batch file to complete the update
                    string batchPath = Path.Combine(Path.GetTempPath(), "complete_update.bat");
                    string reoPath = Path.Combine(parentPath, "reo.exe");

                    string batchContent = $@"
@echo off
:retry
timeout /t 2 /nobreak > nul
tasklist /FI ""IMAGENAME eq updater.exe"" 2>nul | find /I /N ""updater.exe"">nul
if ""%ERRORLEVEL%""==""0"" goto retry

rem Wait a bit more to be sure
timeout /t 1 /nobreak > nul

rem Start the new version of the application
start """" ""{reoPath}""

rem Delete the backup updater
del ""{backupUpdaterPath}""

rem Delete this batch file
del ""%~f0""
exit
";

                    File.WriteAllText(batchPath, batchContent);
                    log.WriteLine($"Created batch file: {batchPath}");

                    // Clean up the temp directory
                    try
                    {
                        if (Directory.Exists(tempPath))
                        {
                            try
                            {
                                Directory.Delete(tempPath, true);
                                log.WriteLine("Cleaned temp directory");
                            }
                            catch (Exception e)
                            {
                                log.WriteLine($"Cleanining temp directory failed: {e.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.WriteLine($"Error cleaning temp directory: {ex.Message}");
                    }

                    // Run the batch file and exit the updater
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });

                    log.WriteLine("Update completed, exiting updater");
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při aktualizaci: {ex.Message}\nZkontrolujte log v %temp%\\update_log.txt Stacktrace: {ex.StackTrace}",
                    "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        void CopyDirectoryWithLogging(string sourceDir, string targetDir, bool overwrite, StreamWriter log)
        {
            Directory.CreateDirectory(targetDir);
            log.WriteLine($"Created directory: {targetDir}");

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                try
                {
                    File.Copy(file, destFile, overwrite);
                    log.WriteLine($"Copied file: {fileName} -> {destFile}");
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Error copying {fileName}: {ex.Message}");
                }
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectoryWithLogging(dir, destDir, overwrite, log);
            }
        }

        void progressBar1_Click(object sender, EventArgs e)
        {
        }

        void label1_Click(object sender, EventArgs e)
        {
        }

        void button1_Click(object sender, EventArgs e)
        {
            if (CanUpdateFiles())
            {
                button1.Visible = false;
                PerformUpdate();
            }
        }
    }
}
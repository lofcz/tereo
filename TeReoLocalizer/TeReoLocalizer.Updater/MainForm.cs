using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;
using TeReoLocalizer.Updater.Services;
using Application = System.Windows.Forms.Application;
using Exception = System.Exception;
using Label = System.Windows.Forms.Label;

namespace TeReoLocalizer.Updater
{
    public partial class MainForm : Form
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly GitHubClient githubClient = new GitHubClient(new ProductHeaderValue("TeReoUpdater"));

        readonly string tempPath;
        readonly string parentPath;
        
        Label processLabel;
        Button killButton;
        Timer processTimer;
        bool canProceed;

        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ShowInTaskbar = true;
            
            tempPath = Path.Combine(Path.GetTempPath(), "TeReoUpdate");
  
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            parentPath = Directory.GetParent(Directory.GetParent(currentPath).FullName).FullName;
        }

        void Form1_Load(object sender, EventArgs e)
        {
            SetMainUiVisible(false);
            label1.Visible = true;
            label1.Text = "Probíhá příprava na aktualizaci";
            label2.Visible = false;
            
            try
            {
                button1.Visible = false;
                CheckProcesses();
            }
            catch (Exception eOuter)
            {
                MessageBox.Show($"Došlo k neošetřené chybě při aktualizaci: {eOuter.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void CheckProcesses()
        {
            processTimer = new Timer();
            processTimer.Interval = 1000;
            processTimer.Tick += ProcessTimer_Tick;
            processTimer.Start();
            
            CheckAndUpdateUi();
        }
        
        void ProcessTimer_Tick(object sender, EventArgs e)
        {
            CheckAndUpdateUi();
            
            if (canProceed)
            {
                processTimer.Stop();
                processTimer.Dispose();
                processLabel?.Dispose();
                killButton?.Dispose();
                SetMainUiVisible(false);
                StartDownload();
            }
        }

        void SetMainUiVisible(bool visible)
        {
            if (!visible)
            {
                button1.Hide();
                progressBar1.Hide();
                label1.Hide();   
            }
            else
            {
                button1.Show();
                progressBar1.Show();
                label1.Show();
            }
        }
        
        void CheckAndUpdateUi()
        {
            Process[] runningInstances = Process.GetProcessesByName("TeReoLocalizer");

            if (runningInstances.Length > 0)
            {
                canProceed = false;

                SetMainUiVisible(false);
                
                if (processLabel == null)
                {
                    processLabel = new Label();
                    processLabel.AutoSize = true;
                    processLabel.Location = new Point(10, 10);
                    Controls.Add(processLabel);
                }
                
                processLabel.Text = $"Před aktualizací je potřeba ukončit běžící procesy Rea ({runningInstances.Length}):\n" +
                                    string.Join("\n", runningInstances.Select(p => $"PID: {p.Id}"));
                
                if (killButton == null)
                {
                    killButton = new Button();
                    killButton.AutoSize = true;
                    killButton.Text = "Ukončit";
                    killButton.Location = new Point(10, processLabel.Bottom + 10);
                    killButton.Click += KillButton_Click;
                    Controls.Add(killButton);
                }
            }
            else
            {
                canProceed = true;
            }
        }

        void KillButton_Click(object sender, EventArgs e)
        {
            Process[] runningInstances = Process.GetProcessesByName("TeReoLocalizer");
            List<int> failedProcesses = [];
            
            foreach (Process proc in runningInstances)
            {
                try
                {
                    WindowsService.ForceKill(proc);
                }
                catch (Exception)
                {
                    failedProcesses.Add(proc.Id);
                }
            }
            
            CheckAndUpdateUi();

            if (failedProcesses.Count > 0)
            {
                MessageBox.Show(
                    $"Nepodařilo se ukončit následující procesy:\n{string.Join(", ", failedProcesses)}",
                    "Varování",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        async void StartDownload()
        {
            try
            {
                SetMainUiVisible(true);
                button1.Visible = false;
                label1.Text = "Získávání informací o poslední verzi";

                string localVersionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reoVersion.txt");
                if (!File.Exists(localVersionPath))
                {
                    button1.Visible = false;
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
                    button1.Hide();
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
                        byte[] buffer = new byte[16_384];
                        long bytesRead = 0L;
                        int count;

                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, count);
                            bytesRead += count;

                            if (totalBytes != -1)
                            {
                                double percentage = ((bytesRead * 100) / (double)totalBytes);
                                double mbRead = bytesRead / (1024.0 * 1024.0);
                                double mbTotal = totalBytes / (1024.0 * 1024.0);

                                progressBar1.Maximum = 100;
                                progressBar1.Value = (int)percentage;
   
                                label2.Visible = true;
                                label2.Text = $"Staženo {mbRead:F2}/{mbTotal:F2} MB ({percentage:F2}%)";
                                label2.Refresh();
                            }
                        }
                    }
                }

                progressBar1.Hide();
                label2.Hide();
                progressBar1.Refresh();
                label2.Refresh();
                
                label1.Text = "Probíhá rozbalení aktualizace, okamžik strpení..";
                label1.Refresh();
                Application.DoEvents();
                
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
                
                string reoBootPath = Path.Combine(parentPath, "TeReoLocalizer", "reoBoot.json");
                string reoBootContent = null;
                if (File.Exists(reoBootPath))
                {
                    reoBootContent = File.ReadAllText(reoBootPath);
                }

                using StreamWriter log = new StreamWriter(logPath, true);
                log.WriteLine($"\n=== Update started at {DateTime.Now} ===");
                    
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
                    
                if (reoBootContent != null)
                {
                    File.WriteAllText(reoBootPath, reoBootContent);
                    log.WriteLine("Restored reoBoot.json content");
                }

                // Create a batch file to complete the update
                string batchPath = Path.Combine(Path.GetTempPath(), "complete_update.bat");
                string reoPath = Directory.GetFiles(parentPath, "reo.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (string.IsNullOrEmpty(reoPath))
                {
                    reoPath = Directory.GetFiles(parentPath, "TeReoLocalizer.exe", SearchOption.AllDirectories).FirstOrDefault();

                    if (string.IsNullOrEmpty(reoPath))
                    {
                        log.WriteLine("Error: reo.exe/TeReoLocalizer.exe not found in any subdirectory");
                        MessageBox.Show("Aktualizace dokončena úspěšně. Nová verze reo.exe nenalezena, spusťe aplikaci ručně.", "Aktualizace dokončena");
                        return;   
                    }
                }

                string batchContent = $@"
@echo off
title Te Reo - dokončení aktualizace

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
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při aktualizaci: {ex.Message}\nZkontrolujte log v %temp%\\update_log.txt Stacktrace: {ex.StackTrace}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
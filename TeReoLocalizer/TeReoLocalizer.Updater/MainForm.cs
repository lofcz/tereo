using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;
using Application = System.Windows.Forms.Application;

namespace TeReoLocalizer.Updater
{
    public partial class MainForm : Form
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly GitHubClient githubClient = new GitHubClient(new ProductHeaderValue("TeReoUpdater"));

        private string tempPath;
        private string updatePath;
        private string parentPath;
        
        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        async void Form1_Load(object sender, EventArgs e)
        {
            button1.Visible = false; // Skryjeme tlačítko na začátku
            tempPath = Path.Combine(Path.GetTempPath(), "TeReoUpdate");
            updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.zip");
            parentPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName;
            
            try
            {
                label1.Text = "Získávání informací o poslední verzi";
                
                // Kontrola existence souboru s verzí
                string localVersionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reoVersion.txt");
                if (!File.Exists(localVersionPath))
                {
                    progressBar1.Visible = false;
                    label1.Text = "Není možné určit aktuální verzi, protože chybí soubor reoVersion.txt";
                    return;
                }

                // Získání lokální verze
                string localVersion = File.ReadAllText(localVersionPath).Trim();
                Version currentVersion = Version.Parse(localVersion);

                // Získání posledního releasu
                IReadOnlyList<Release> releases = await githubClient.Repository.Release.GetAll("lofcz", "tereo");
                Release latestRelease = releases[0];

                // Najít správný asset (ZIP soubor)
                ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(a => a.Name.StartsWith("TeReoLocalizer-v") && a.Name.EndsWith(".zip"));
                
                if (asset == null)
                {
                    MessageBox.Show("Nebyl nalezen ZIP soubor ke stažení.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Získání upstream verze
                string upstreamVersionStr = asset.Name.Replace("TeReoLocalizer-v", "").Replace(".zip", "");
                Version upstreamVersion = Version.Parse(upstreamVersionStr);

                // Porovnání verzí
                if (currentVersion >= upstreamVersion)
                {
                    progressBar1.Visible = false;
                    label1.Text = $"Reo je aktuální, verze {localVersion}";
                    return;
                }

                // Smazání starého update souboru pokud existuje
                string updatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.zip");
                if (File.Exists(updatePath))
                {
                    File.Delete(updatePath);
                }

                label1.Text = $"Probíhá stažení verze {upstreamVersionStr}";

                // Stažení souboru s progress reportingem
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
                    // Vyčistíme temp složku pokud existuje
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                    Directory.CreateDirectory(tempPath);

                    // Rozbalíme ZIP
                    ZipFile.ExtractToDirectory(updatePath, tempPath);

                    // Kontrola běžících procesů
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
        private bool CanUpdateFiles()
    {
        var processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            try
            {
                string processPath = process.MainModule?.FileName;
                if (string.IsNullOrEmpty(processPath)) continue;

                if (processPath.StartsWith(parentPath) && 
                    !processPath.EndsWith("updater.exe", StringComparison.OrdinalIgnoreCase))
                {
                    label1.Text = $"Proces {process.ProcessName} blokuje dokončení aktualizace, ukončete proces a stiskněte tlačítko pokračovat";
                    return false;
                }
            }
            catch
            {
                // Ignorujeme chyby při čtení procesu
            }
        }
        return true;
    }

  private void PerformUpdate()
{
    try
    {
        string logPath = Path.Combine(Path.GetTempPath(), "update_log.txt");
        using (StreamWriter log = new StreamWriter(logPath, true))
        {
            log.WriteLine($"=== Update started at {DateTime.Now} ===");
            log.WriteLine($"Parent path: {parentPath}");
            log.WriteLine($"Temp path: {tempPath}");
            
            log.WriteLine("\nFiles in temp folder:");
            foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
            {
                log.WriteLine($"- {file}");
            }

            // Připravíme nový updater, pokud existuje
            string currentUpdater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater.exe");
            string newUpdater = Path.Combine(tempPath, "updater", "updater.exe");
            string newUpdaterTemp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "updater_new.exe");
            
            bool hasNewUpdater = File.Exists(newUpdater);
            log.WriteLine($"\nNew updater exists: {hasNewUpdater}");
            
            if (hasNewUpdater)
            {
                File.Copy(newUpdater, newUpdaterTemp, true);
                log.WriteLine("Copied new updater to temp location");
            }
            
            log.WriteLine("\nCopying root files:");
            foreach (var file in Directory.GetFiles(tempPath))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(parentPath, fileName);
                try
                {
                    File.Copy(file, destFile, true);
                    log.WriteLine($"Copied: {fileName} -> {destFile}");
                }
                catch (IOException ex)
                {
                    log.WriteLine($"Error copying {fileName}: {ex.Message}");
                    File.Delete(destFile);
                    File.Copy(file, destFile, true);
                    log.WriteLine($"Retry successful: {fileName}");
                }
            }

            // Kopírujeme všechny podsložky kromě updater
            log.WriteLine("\nCopying directories:");
            foreach (var dir in Directory.GetDirectories(tempPath))
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
                    Directory.Delete(targetPath, true);
                    log.WriteLine($"Deleted existing directory: {targetPath}");
                }
                
                CopyDirectoryWithLogging(dir, targetPath, true, log);
            }
            
            
            // Vytvoříme batch soubor pro dokončení aktualizace
            string batchPath = Path.Combine(Path.GetTempPath(), "complete_update.bat");
            string reoPath = Path.Combine(parentPath, "reo.exe");
            string startCommand = File.Exists(reoPath) ? $@"start """" ""{reoPath}""" : "echo Reo.exe not found";

            log.WriteLine($"\nReo.exe exists: {File.Exists(reoPath)}");
            
            string batchContent = $@"
@echo off
:retry
timeout /t 2 /nobreak > nul
tasklist /FI ""IMAGENAME eq updater.exe"" 2>nul | find /I /N ""updater.exe"">nul
if ""%ERRORLEVEL%""==""0"" goto retry

{(hasNewUpdater ? $@"
del ""{currentUpdater}""
move ""{newUpdaterTemp}"" ""{currentUpdater}""" : "")}
{startCommand}
del ""%~f0""
exit
";


            File.WriteAllText(batchPath, batchContent);

            // Vyčistíme temp složku
            try
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
            catch
            {
                // Ignorujeme chyby při mazání temp složky
            }

            // Spustíme launcher a ukončíme updater
            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            // Ukončíme současný proces
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.CloseMainWindow();
            Environment.Exit(0);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Chyba při dokončování aktualizace: {ex.Message}", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}



private void CopyDirectoryWithLogging(string sourceDir, string targetDir, bool overwrite, StreamWriter log)
{
    Directory.CreateDirectory(targetDir);
    log.WriteLine($"Created directory: {targetDir}");

    foreach (var file in Directory.GetFiles(sourceDir))
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

    foreach (var dir in Directory.GetDirectories(sourceDir))
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
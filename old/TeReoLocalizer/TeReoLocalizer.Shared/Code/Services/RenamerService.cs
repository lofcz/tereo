using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
namespace TeReoLocalizer.Shared.Code.Services;

public class RenameResult
{
    public bool Success { get; set; }
    public int TotalReplacements { get; set; }
}

public class ProgressMessage
{
    public string Text { get; set; }
}

public class CommandProgress
{
    public double Percentage { get; }
    public List<ProgressMessage> Messages { get; set; } = [];
    
    public CommandProgress(double percentage)
    {
        Percentage = percentage;
    }
    
    public CommandProgress(double percentage, List<ProgressMessage> messages)
    {
        Percentage = percentage;
        Messages = messages;
    }
    
    public CommandProgress(double percentage, IEnumerable<ProgressMessage> messages)
    {
        Percentage = percentage;
        Messages = messages.ToList();
    }
}

public class RenameProgress
{
    public double Percentage { get; }
    public int ReplacementsCount { get; }
    public Dictionary<string, int> FileReplacements { get; }

    public RenameProgress(double percentage, int replacementsCount, Dictionary<string, int> fileReplacements)
    {
        Percentage = percentage;
        ReplacementsCount = replacementsCount;
        FileReplacements = fileReplacements;
    }
}

public static partial class SymbolRenamer
{
    static readonly Regex ReoSymbolBackendRegex = ReoRegexBackendSrc();
    static readonly Regex ReoSymbolFrontendRegex = ReoRegexFrontendSrc();

    private static readonly HashSet<string> backendExtensions = [".cs", ".razor", ".cshtml", ".razor.cs"];
    private static readonly string[] frontendFileExtensions = ["*.ts", "*.js", "*.js.min"];
    private static readonly string[] backendFileExtensions = backendExtensions.Select(x => $"*{x}").ToArray();

    public static async Task<RenameResult> RenameSymbol(string projectDirectory, string oldSymbol, string newSymbol, IProgress<CommandProgress>? progress = null, bool renameBackend = true, bool renameFrontend = false)
    {
        ConcurrentBag<string> files = [];

        List<string> skipShared =
        [
            Path.Combine(projectDirectory, "obj"),
            Path.Combine(projectDirectory, "bin"),
            Path.Combine(projectDirectory, "lfs")
        ];
        
        List<string> skipBackendOnly = [
            Path.Combine(projectDirectory, "wwwroot")
        ];
        
        List<string> skipFrontendOnly = [
            Path.Combine(projectDirectory, "Code"),
            Path.Combine(projectDirectory, "Controllers"),
            Path.Combine(projectDirectory, "Dao"),
            Path.Combine(projectDirectory, "Hubs"),
            Path.Combine(projectDirectory, "I18N"),
        ];

        List<string> skip = renameFrontend && renameBackend ? skipShared : renameBackend ? [..skipShared, ..skipBackendOnly] : [..skipShared, ..skipFrontendOnly];
        string[] patterns = renameFrontend && renameBackend ? [..backendFileExtensions, ..frontendFileExtensions] : renameBackend ? backendFileExtensions : frontendFileExtensions;
        
        foreach (string file in projectDirectory.GetFiles(patterns, SearchOption.AllDirectories, x => !skip.Any(y => x.StartsWith(y, StringComparison.OrdinalIgnoreCase))))
        {
            files.Add(file);
        }

        int totalFiles = files.Count;
        int processedFiles = 0;
        int totalReplacements = 0;
        ConcurrentDictionary<string, int> fileReplacements = [];

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (filePath, ctx) =>
        {
            int localFileReplacements = 0;
            string content = await File.ReadAllTextAsync(filePath, ctx);
            string extension = Path.GetExtension(filePath);

            string newContent;
            
            if (backendExtensions.Contains(extension))
            {
                newContent = ReoSymbolBackendRegex.Replace(content, m =>
                {
                    string? capturedSymbol = null;
                    string prefix = string.Empty;
                    string suffix = string.Empty;
                    
                    if (!string.IsNullOrEmpty(m.Groups[2].Value))
                    {
                        // Reo.KeyXXX
                        capturedSymbol = m.Groups[2].Value;
                        prefix = "Key";
                    }
                    else if (!string.IsNullOrEmpty(m.Groups[3].Value))
                    {
                        // Reo.XXXString
                        capturedSymbol = m.Groups[3].Value;
                        suffix = "String";
                    }
                    else if (!string.IsNullOrEmpty(m.Groups[4].Value))
                    {
                        // Reo.XXX
                        capturedSymbol = m.Groups[4].Value;
                    }
                    
                    if (capturedSymbol == oldSymbol)
                    {
                        localFileReplacements++;
                        return $"Reo.{prefix}{newSymbol}{suffix}";
                    }
    
                    return m.Value;
                });
            }
            else
            {
                newContent = ReoSymbolFrontendRegex.Replace(content, m =>
                {
                    if (m.Groups[1].Value == oldSymbol)
                    {
                        localFileReplacements++;
                        return $"reo.{newSymbol}";
                    }
                    return m.Value;
                });
            }

            if (localFileReplacements > 0)
            {
                await File.WriteAllTextAsync(filePath, newContent, ctx);
                fileReplacements.TryAdd(filePath, localFileReplacements);
                Interlocked.Add(ref totalReplacements, localFileReplacements);
            }

            int currentProcessed = Interlocked.Increment(ref processedFiles);
            double progressPercentage = (double)currentProcessed / totalFiles * 100;

            if (progress is not null && (progressPercentage % 10 < 0.1 || currentProcessed == totalFiles))
            {
                progress.Report(new CommandProgress(progressPercentage)); // , totalReplacements, new Dictionary<string, int>(fileReplacements)
            }
        });
        
        return new RenameResult { Success = true, TotalReplacements = totalReplacements };
    }

    [GeneratedRegex(@"\bReo\.(Key(\w+)|(\w+)String|(\w+))\b", RegexOptions.Compiled)]
    private static partial Regex ReoRegexBackendSrc();
    
    [GeneratedRegex(@"\breo\.(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex ReoRegexFrontendSrc();
}
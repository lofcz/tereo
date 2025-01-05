using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

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
    private static readonly Regex ReoSymbolRegex = ReoRegexSrc();

    public static async Task<RenameResult> RenameSymbol(string projectDirectory, string oldSymbol, string newSymbol, IProgress<CommandProgress>? progress = null)
    {
        ConcurrentBag<string> files = [];

        List<string> skip =
        [
            Path.Combine(projectDirectory, "wwwroot"),
            Path.Combine(projectDirectory, "obj"),
            Path.Combine(projectDirectory, "bin")
        ];

        string[] patterns = ["*.cs", "*.razor", "*.cshtml", "*.razor.cs"];

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
            string newContent = ReoSymbolRegex.Replace(content, m =>
            {
                if (m.Groups[1].Value == oldSymbol)
                {
                    localFileReplacements++;
                    return $"Reo.{newSymbol}";
                }
                return m.Value;
            });

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

    private static (int replacements, int newSize) ReplaceInSpan(Span<byte> span, string oldSymbol, string newSymbol)
    {
        int replacements = 0;
        string content = Encoding.UTF8.GetString(span);
        string newContent = ReoSymbolRegex.Replace(content, m =>
        {
            if (m.Groups[1].Value == oldSymbol)
            {
                replacements++;
                return $"Reo.{newSymbol}";
            }

            return m.Value;
        });

        if (replacements > 0)
        {
            byte[] newBytes = Encoding.UTF8.GetBytes(newContent);
            int copyLength = Math.Min(newBytes.Length, span.Length);
            newBytes.AsSpan(0, copyLength).CopyTo(span);
            return (replacements, newBytes.Length);
        }

        return (0, span.Length);
    }

    [GeneratedRegex(@"\bReo\.(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex ReoRegexSrc();
}
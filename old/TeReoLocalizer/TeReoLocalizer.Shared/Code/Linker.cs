using System.Reflection;

namespace TeReoLocalizer.Shared.Code;

public static class Linker
{
    public static string? LinkFileContent(string endingFileName)
    {
        string fp = $"{AppDomain.CurrentDomain.BaseDirectory}\\{endingFileName}";
        return !File.Exists(fp) ? null : File.ReadAllText(fp);
    }
    
    public static string[]? LinkFileContent(string endingFileName, int skipFirst, int skipLast)
    {
        string fp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, endingFileName);

        if (!File.Exists(fp))
        {
            return null;
        }
        
        string[] allLines = File.ReadAllLines(fp);

        if (allLines.Length <= skipFirst + skipLast)
        {
            return [];
        }
        
        return allLines
            .Skip(skipFirst)
            .Take(allLines.Length - skipFirst - skipLast)
            .ToArray();
    }

}

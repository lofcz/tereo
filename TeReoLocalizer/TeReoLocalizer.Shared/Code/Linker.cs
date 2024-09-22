using System.Reflection;

namespace TeReoLocalizer.Shared.Code;

public static class Linker
{
    public static string? LinkFileContent(string endingFileName)
    {
        string fp = $"{AppDomain.CurrentDomain.BaseDirectory}\\{endingFileName}";
        return !File.Exists(fp) ? null : File.ReadAllText(fp);
    }
}

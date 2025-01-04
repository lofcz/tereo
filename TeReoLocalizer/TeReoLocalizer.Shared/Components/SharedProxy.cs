namespace TeReoLocalizer.Shared.Components;

public static class SharedProxy
{
    public static bool IsMaui { get; set; } = true;
    public static string? Repository { get; set; }
    public static string? Sln { get; set; }
}
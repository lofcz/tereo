using System.Text.RegularExpressions;

internal static partial class GitignoreRegexPatterns
{
    const string MatchEmptyRegexPattern = "$^";
    const string RangeRegexPattern = @"^((?:[^\[\\]|(?:\\.))*)\[((?:[^\]\\]|(?:\\.))*)\]";
    const string BackslashRegexPattern = @"\\(.)";
    const string SpecialCharactersRegexPattern = @"[\-\[\]\{\}\(\)\+\.\\\^\$\|]";
    const string QuestionMarkRegexPattern = @"\?";
    const string SlashDoubleAsteriskSlashRegexPattern = @"\/\*\*\/";
    const string DoubleAsteriskSlashRegexPattern = @"^\*\*\/";
    const string SlashDoubleAsteriskRegexPattern = @"\/\*\*$";
    const string DoubleAsteriskRegexPattern = @"\*\*";
    const string SlashAsteriskEndOrSlashRegexPattern = @"\/\*(\/|$)";
    const string AsteriskRegexPattern = @"\*";
    const string SlashRegexPattern = @"\/";

    [GeneratedRegex(MatchEmptyRegexPattern)]
    private static partial Regex GeneratedMatchEmptyRegex();
    [GeneratedRegex(RangeRegexPattern)]
    private static partial Regex GeneratedRangeRegex();
    [GeneratedRegex(BackslashRegexPattern)]
    private static partial Regex GeneratedBackslashRegex();
    [GeneratedRegex(SpecialCharactersRegexPattern)]
    private static partial Regex GeneratedSpecialCharactersRegex();
    [GeneratedRegex(QuestionMarkRegexPattern)]
    private static partial Regex GeneratedQuestionMarkRegex();
    [GeneratedRegex(SlashDoubleAsteriskSlashRegexPattern)]
    private static partial Regex GeneratedSlashDoubleAsteriksSlashRegex();
    [GeneratedRegex(DoubleAsteriskSlashRegexPattern)]
    private static partial Regex GeneratedDoubleAsteriksSlashRegex();
    [GeneratedRegex(SlashDoubleAsteriskRegexPattern)]
    private static partial Regex GeneratedSlashDoubleAsteriksRegex();
    [GeneratedRegex(DoubleAsteriskRegexPattern)]
    private static partial Regex GeneratedDoubleAsteriksRegex();
    [GeneratedRegex(SlashAsteriskEndOrSlashRegexPattern)]
    private static partial Regex GeneratedSlashAsteriksEndOrSlashRegex();
    [GeneratedRegex(AsteriskRegexPattern)]
    private static partial Regex GeneratedAsteriksRegex();
    [GeneratedRegex(SlashRegexPattern)]
    private static partial Regex GeneratedSlashRegex();

    public static readonly Regex MatchEmptyRegex = GeneratedMatchEmptyRegex();
    public static readonly Regex RangeRegex = GeneratedRangeRegex();
    public static readonly Regex BackslashRegex = GeneratedBackslashRegex();
    public static readonly Regex SpecialCharactersRegex = GeneratedSpecialCharactersRegex();
    public static readonly Regex QuestionMarkRegex = GeneratedQuestionMarkRegex();
    public static readonly Regex SlashDoubleAsteriskSlashRegex = GeneratedSlashDoubleAsteriksSlashRegex();
    public static readonly Regex DoubleAsteriskSlashRegex = GeneratedDoubleAsteriksSlashRegex();
    public static readonly Regex SlashDoubleAsteriskRegex = GeneratedSlashDoubleAsteriksRegex();
    public static readonly Regex DoubleAsteriskRegex = GeneratedDoubleAsteriksRegex();
    public static readonly Regex SlashAsteriskEndOrSlashRegex = GeneratedSlashAsteriksEndOrSlashRegex();
    public static readonly Regex AsteriskRegex = GeneratedAsteriksRegex();
    public static readonly Regex SlashRegex = GeneratedSlashRegex();
}
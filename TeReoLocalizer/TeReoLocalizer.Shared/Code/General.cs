using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using NanoidDotNet;

namespace TeReoLocalizer.Shared.Code;

public static partial class General
{
    const string IiidAlphabet = "_0123456789abcdefghijklmnopqrstuvwxzyABCDEFGHCIJKLMNOPQRSTUVWXYZ";
    
    public static bool Disable(string reason)
    {
        return false;
    }
    
    public static bool Obsolete(string reason)
    {
        return false;
    }
    
    public static string IIID()
    {
        return $"_{Nanoid.Generate(IiidAlphabet)}";
    }
    
    public static string HumanAlphaDigitIIID()
    {
        return $"{Nanoid.Generate("0123456789abcdefghijklmnopqrstuvwxzy")}";
    }

    public static string GetOrFallbackProfilePicture(string rawImg)
    {
        if (rawImg.IsNullOrWhiteSpace() || rawImg == "E")
        {
            return "/uploads/images/defaultUser.png";
        }

        return rawImg;
    }

    public static MarkupString FormatHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return (MarkupString)"";
        }

        return (MarkupString)html.Replace("\n", "<br/>");
    }

    static Dictionary<char, string> ConvertMap = new Dictionary<char, string>()
    {
        {' ', "-"},
        {'+', ""},
        {'.', ""},
        {':', ""},
        {'!', ""},
        {'\\', ""},
        {'\'', ""},
        {'?', ""},
        {'_', "-"},
        {',', "-"},
        {'$', ""},
        {'/', ""},
        {'(', ""},
        {')', ""},
        {'=', ""},
        {'[', ""},
        {']', ""},
        {'{', ""},
        {'}', ""},
        {'#', ""},
        {'@', ""},
        {'|', ""},
        {'€', ""},
        {'~', ""},
        {'ˇ', ""},
        {'÷', ""},
        {'ß', ""},
        {'×', ""},
        {'¤', ""},
        {'<', ""},
        {'>', ""},
        {'*', ""},
        {'„', ""},
        {'“', ""},
        {'`', ""},
    };

    public static string GetUrlName(string? name, bool limitToOneConsecutiveDash = true)
    {
        string s = name ?? "";
        s = s.ToLowerInvariant();
        s = s.Trim();

        StringBuilder sb = new StringBuilder();
        foreach (char c in s)
        {
            if (ConvertMap.TryGetValue(c, out string? str))
            {
                sb.Append(str);
            }
            else
            {
                sb.Append(c);
            }
        }

        string accentedStr = sb.ToString();
        byte[] tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(accentedStr);
        string asciiStr = Encoding.UTF8.GetString(tempBytes);

        if (limitToOneConsecutiveDash)
        {
            asciiStr = MyRegex().Replace(asciiStr, "-");
        }

        if (asciiStr.EndsWith('-'))
        {
            asciiStr = asciiStr.Remove(asciiStr.Length - 1);
        }
        
        return asciiStr;
    }

    [GeneratedRegex("[\\s-]+", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
using System.Runtime.CompilerServices;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    static (bool contains, bool startsWith, bool exactMatch) CompareStrings(string source, string search, bool ignoreCase)
    {
        if (source.Length < search.Length)
        {
            return (false, false, false);
        }

        ReadOnlySpan<char> sourceSpan = source.AsSpan();
        ReadOnlySpan<char> searchSpan = search.AsSpan();

        bool startsWith = true;
        bool exactMatch = source.Length == search.Length;

        for (int i = 0; i < searchSpan.Length; i++)
        {
            if (!CharsEqual(sourceSpan[i], searchSpan[i], ignoreCase))
            {
                startsWith = false;
                exactMatch = false;
                break;
            }
        }

        if (startsWith)
        {
            return (true, true, exactMatch);
        }

        for (int i = 1; i <= sourceSpan.Length - searchSpan.Length; i++)
        {
            bool found = true;

            for (int j = 0; j < searchSpan.Length; j++)
            {
                if (!CharsEqual(sourceSpan[i + j], searchSpan[j], ignoreCase))
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                return (true, false, false);
            }
        }

        return (false, false, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool CharsEqual(char c1, char c2, bool ignoreCase)
    {
        if (ignoreCase)
        {
            return char.ToUpperInvariant(c1) == char.ToUpperInvariant(c2);
        }

        return c1 == c2;
    }

    static readonly ParallelOptions FullDop = new ParallelOptions
    {
        MaxDegreeOfParallelism = Environment.ProcessorCount
    };
}
using System.Text;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

internal static class KalDataGridSearch
{
    internal static List<(int Start, int End)> GetHighlightRanges(string text, string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return [];
        }

        var normalized = new StringBuilder(text.Length);
        var originalIndexes = new List<int>(text.Length);

        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsLetterOrDigit(text[index]))
            {
                normalized.Append(text[index]);
                originalIndexes.Add(index);
            }
        }

        var normalizedText = normalized.ToString();
        var ranges = new List<(int Start, int End)>();
        var filterTerms = filterText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var term in filterTerms)
        {
            var normalizedTerm = Normalize(term);
            if (normalizedTerm.Length == 0)
            {
                continue;
            }

            var searchFrom = 0;
            while (searchFrom < normalizedText.Length)
            {
                var matchIndex = normalizedText.IndexOf(normalizedTerm, searchFrom, StringComparison.CurrentCultureIgnoreCase);
                if (matchIndex < 0)
                {
                    break;
                }

                var start = originalIndexes[matchIndex];
                var end = originalIndexes[matchIndex + normalizedTerm.Length - 1] + 1;
                ranges.Add((start, end));
                searchFrom = matchIndex + normalizedTerm.Length;
            }
        }

        var mergedRanges = new List<(int Start, int End)>();
        foreach (var range in ranges.OrderBy(range => range.Start))
        {
            if (mergedRanges.Count == 0 || range.Start > mergedRanges[^1].End)
            {
                mergedRanges.Add(range);
                continue;
            }

            mergedRanges[^1] = (mergedRanges[^1].Start, Math.Max(mergedRanges[^1].End, range.End));
        }

        return mergedRanges;
    }

    internal static bool Contains(string? value, string? searchTerm)
    {
        var normalizedTerm = Normalize(searchTerm);
        return normalizedTerm.Length > 0
            && Normalize(value).Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase);
    }

    internal static bool Equals(string? value, string? searchTerm)
    {
        var normalizedTerm = Normalize(searchTerm);
        return normalizedTerm.Length > 0
            && string.Equals(Normalize(value), normalizedTerm, StringComparison.CurrentCultureIgnoreCase);
    }

    internal static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var normalized = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                normalized.Append(character);
            }
        }

        return normalized.ToString();
    }
}

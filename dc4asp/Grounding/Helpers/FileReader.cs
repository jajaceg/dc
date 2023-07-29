using System.Text.RegularExpressions;

namespace dc4asp.Grounding.Helpers;

internal static class FileReader
{
    public static IEnumerable<string> ReadFile(string path)
    {
        var fileLines = File.ReadLines(path);
        var withoutComments = fileLines
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => !x.StartsWith('%'));

        string pattern = @"(?<!\.)\.(?!\.)"; // Matches a single period not followed by another period
        var lines = new List<string>();
        foreach (var line in withoutComments)
        {
            var splitted = Regex.Split(line, pattern)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());
            lines.AddRange(splitted);
        }

        return lines;
    }
}

using System.Text.RegularExpressions;

namespace dc4asp.Grounding.Helpers
{
    internal static class FileReader
    {
        public static IEnumerable<string> ReadFile(string path)
        {
            var file = File.ReadAllText(path);
            string pattern = @"(?<!\.)\.(?!\.)"; // Matches a single period not followed by another period
            return Regex.Split(file, pattern)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x=>x.Trim());
        }
    }
}

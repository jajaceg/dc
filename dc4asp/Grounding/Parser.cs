using System.Text.RegularExpressions;

namespace dc4asp.Grounding
{
    internal static class Parser
    {
        public static List<string> VariablesToConstants(IEnumerable<string> lines)
        {
            List<string> result = new();

            foreach (var line in lines)
            {
                if (!line.Contains('(') && !line.Contains(')'))
                {
                    result.Add(line);
                    continue;
                }

                string varName = GetName(line);
                string varValue = GetValue(line);

                if (varValue.Contains(".."))
                {
                    List<int> values = ParseRange(varValue);
                    foreach (int value in values)
                    {
                        result.Add($"{varName}({value})");
                    }
                }
                else
                {
                    result.Add(line);
                }
            }

            return result;
        }

        private static string GetName(string line)
        {
            string pattern = @"(.*?)\(";
            Match match = Regex.Match(line, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            throw new Exception("Getting variable name failed.");
        }

        private static string GetValue(string line)
        {
            string pattern = @"\((.*?)\)";
            Match match = Regex.Match(line, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            throw new Exception("Getting variable value failed.");
        }

        private static List<int> ParseRange(string range)
        {
            string[] parts = range.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 || !int.TryParse(parts[0], out int start) || !int.TryParse(parts[1], out int end))
            {
                throw new Exception("Invalid range format.");
            }

            List<int> values = new();
            for (int i = start; i <= end; i++)
            {
                values.Add(i);
            }

            return values;
        }
    }
}

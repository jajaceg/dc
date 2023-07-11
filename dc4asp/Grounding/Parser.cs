using dc4asp.Grounding.Model;
using System.Text.RegularExpressions;

namespace dc4asp.Grounding;

//todo uporządkować interanl, public, private itd.
internal static class Parser
{
    public static List<Fact> ParseFacts(IEnumerable<string> lines)
    {
        List<Fact> factList = new();
        int index = 1;


        foreach (var line in lines)
        {
            if (!line.Contains('(') && !line.Contains(')'))
            {
                factList.Add(new Fact(line, line, index++));
                continue;
            }
            string varName = GetName(line);
            string varValue = GetValue(line);

            if (varValue.Contains(".."))
            {
                List<int> values = ParseRange(varValue);
                List<Fact> rangeFacts = values.Select(value =>
                    new Fact($"{varName}({value})", varName, index++, new List<string> { value.ToString() }))
                    .ToList();
                factList.AddRange(rangeFacts);
            }
            else
            {
                List<string> arguments = varValue.Split(',').Select(part => part.Trim()).ToList();
                factList.Add(new Fact(line, varName, index++, arguments));
            }
        }

        return factList;
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

    public static List<ParsedRule> PrepareRulesForGrounder(IEnumerable<string> rules)
    {
        List<ParsedRule> resultList = new();

        foreach (string rule in rules)
        {
            var parsedRule = new ParsedRule();

            string pattern = @"([^(),]+(?:\([^()]*\))?)(?:,\s*)?";
            MatchCollection matches = Regex.Matches(rule, pattern);

            if (rule.StartsWith(":-"))
            {
                parsedRule.Kind = Kind.Constraint;

                for (int i = 0; i < matches.Count; i++)
                {
                    string bodyElement;
                    if (i == 0)
                        bodyElement = matches[0].Groups[1].Value.Replace(":-", string.Empty).RemoveNot().Trim();
                    else
                        bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                    parsedRule.BodyAtoms.Add(new Atom
                    {
                        NameWithArgs = bodyElement,
                        Name = GetAtomName(bodyElement),
                        Arguments = new(GetArgumentNames(bodyElement)),
                        IsNegation = matches[i].Groups[1].Value.IsNegation()
                    });
                }

                ParsedRule withExtractedSpecialContructs = ExtractSpecialConstructs(parsedRule);
                resultList.Add(withExtractedSpecialContructs);
            }
            else
            {
                parsedRule.Kind = Kind.Rule;

                var head = rule.Substring(0, rule.IndexOf(":-")).Trim();
                parsedRule.Head = new Atom()
                {
                    NameWithArgs = head,
                    Name = GetAtomName(head),
                    Arguments = new(GetArgumentNames(head))
                };

                for (int i = 1; i < matches.Count; i++)
                {
                    string bodyElement;
                    if (i == 1)
                        bodyElement = matches[i].Groups[1].Value.Replace(":-", string.Empty).RemoveNot().Trim();
                    else
                        bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                    parsedRule.BodyAtoms.Add(new Atom
                    {
                        NameWithArgs = bodyElement,
                        Name = GetAtomName(bodyElement),
                        Arguments = new(GetArgumentNames(bodyElement)),
                        IsNegation = matches[i].Groups[1].Value.IsNegation()
                    });
                }

                ParsedRule withExtractedSpecialContructs = ExtractSpecialConstructs(parsedRule);
                resultList.Add(withExtractedSpecialContructs);
            }

        }
        return resultList;
    }

    public static string GetAtomName(string atom)
    {
        return atom.Split('(').First().Trim();
    }

    public static List<string> GetArgumentNames(string atomWithArgs)
    {
        string pattern = @"\(([^)]+)\)";

        return Regex.Matches(atomWithArgs, pattern)
            .SelectMany(match => match.Groups[1].Captures.Cast<Capture>())
            .Select(capture => capture.Value.Split(',').Select(part => part.Trim()))
            .SelectMany(parts => parts)
            .Distinct()
            .ToList();
    }

    public static string RemoveNot(this string value)
    {
        return value.Replace("not ", string.Empty);
    }
    public static bool IsNegation(this string value)
    {
        return value.Contains("not ");
    }

    private static ParsedRule ExtractSpecialConstructs(ParsedRule parsedRule)
    {
        parsedRule.BodyAtoms.RemoveAll(item =>
        {
            if (item.NameWithArgs.Contains('=') && item.NameWithArgs.Contains('+'))
            {
                parsedRule.Construct = new(ConstructType.CreateNewConstant, item.NameWithArgs);
                parsedRule.RoleHasSpecialConstructs = true;
                return true;
            }
            if (item.NameWithArgs.Contains("!="))
            {
                parsedRule.Construct = new(ConstructType.IsDifferent, item.NameWithArgs);
                parsedRule.RoleHasSpecialConstructs = true;
                return true;
            }
            return false;
        });

        return parsedRule;
    }
}

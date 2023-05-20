using System.Text.RegularExpressions;

namespace dc4asp.Grounding
{
    //TODO rename this class and props

    enum Kind
    {
        Rule,
        Constraint
    }
    class ParsedRule
    {
        public Kind Kind { get; set; }
        public string Head { get; set; }
        public List<string> BodyElements { get; set; } = new();
        public bool RoleHasSpecialConstructs { get; set; }
        public List<IConstructs> Constructs { get; set; } = new();
    }

    interface IConstructs
    {

    }

    //TODO rename these props
    class IsDifferent : IConstructs
    {
        public string IsDifferentConstruct { get; set; } // I1 != I2

        public IsDifferent(string isDifferentConstruct)
        {
            IsDifferentConstruct = isDifferentConstruct;
        }
    }
    class CreateNewConstant : IConstructs
    {
        public string CreateNewConstantConstruct { get; set; } // Z = X + Y

        public CreateNewConstant(string createNewConstantConstruct)
        {
            CreateNewConstantConstruct = createNewConstantConstruct;
        }
    }


    internal static class Grounder
    {
        public static List<ParsedRule> NaiveGrounding(List<string> facts, IEnumerable<string> rules)
        {
            var parsedRules = ExtractGroups(rules);

            return parsedRules;
        }

        static List<ParsedRule> ExtractGroups(IEnumerable<string> rules)
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

                    var firstBodyElement = matches[0].Groups[1].Value.Replace(":-", string.Empty).Trim();
                    parsedRule.BodyElements.Add(firstBodyElement);

                    for (int i = 1; i < matches.Count; i++)
                    {
                        var bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                        parsedRule.BodyElements.Add(bodyElement);
                    }

                    ParsedRule withExtractedSpecialContructs = ExtractSpecialConstructs(parsedRule);
                    resultList.Add(withExtractedSpecialContructs);
                }
                else
                {
                    parsedRule.Kind = Kind.Rule;

                    var head = rule.Substring(0, rule.IndexOf(":-")).Trim();
                    parsedRule.Head = head;

                    var firstBodyElement = matches[1].Groups[1].Value.Replace(":-", string.Empty).RemoveNot().Trim();
                    parsedRule.BodyElements.Add(firstBodyElement);

                    for (int i = 2; i < matches.Count; i++)
                    {
                        string bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                        parsedRule.BodyElements.Add(bodyElement);
                    }

                    ParsedRule withExtractedSpecialContructs = ExtractSpecialConstructs(parsedRule);
                    resultList.Add(withExtractedSpecialContructs);
                }

            }
            return resultList;
        }

        private static ParsedRule ExtractSpecialConstructs(ParsedRule parsedRule)
        {
            parsedRule.BodyElements.RemoveAll(item =>
            {
                if (item.Contains('=') && item.Contains('+'))
                {
                    parsedRule.Constructs.Add(new CreateNewConstant(item));
                    parsedRule.RoleHasSpecialConstructs = true;
                    return true;
                }
                if (item.Contains("!="))
                {
                    parsedRule.Constructs.Add(new IsDifferent(item));
                    parsedRule.RoleHasSpecialConstructs = true;
                    return true;
                }
                return false;
            });

            return parsedRule;
        }
    }

    public static class StringExtension
    {
        public static string RemoveNot(this string value)
        {
            return value.Replace("not ", string.Empty);
        }
    }
}

using System.Text.RegularExpressions;

namespace dc4asp.Grounding
{
    enum Kind
    {
        Rule,
        Constraint
    }

    //TODO rename this class and props
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
        public static List<ParsedRule> NaiveGrounding(Dictionary<string, int> atoms, IEnumerable<string> rules)
        {
            var parsedRules = PrepareRolesForGrounder(rules);

            foreach (var rule in parsedRules.Where(x => x.Kind == Kind.Rule))
            {
                var namesOfFacts = atoms.Keys.Select(x => x.Split('(').First()).Distinct(); //to będą wszystkie które są już faktami
                var namesOfElementsInBody = rule.BodyElements.Select(x => x.Split('(').First());
                var existingOnes = rule.BodyElements.Where(element => namesOfFacts.Any(fact => element.Contains(fact)));
                var knownArgumentNames = existingOnes.Select(x => x.Split('(', ')')[1]).ToList(); // to są te L , I które już znamy

                HashSet<string> allArgumentNames = new();
                foreach (var item in rule.BodyElements)
                {
                    var letters = Regex.Matches(item, @"\((.*?)\)")
                        .Cast<Match>()
                        .Select(match => match.Groups[1].Value)
                        .SelectMany(x => x.Split(','))
                        .Select(x => x.Trim())
                        .Distinct();
                    allArgumentNames.UnionWith(letters);
                }

                if (allArgumentNames.All(x => knownArgumentNames.Contains(x)))
                {
                    List<ParsedRule> rulesWithAllAtoms = new();
                    foreach (var item in existingOnes)
                    {
                        var factsForThisAtom = atoms.Where(x => x.Key.Contains(item.Split('(')[0]));
                        var varName = existingOnes.Select(x => x.Split('(', ')')[1]).First();

                        foreach (var atom in factsForThisAtom)
                        {
                            string modifiedText = Regex.Replace(rule.Head, @"\((.*?)\)", match =>
                            {
                                string valueWithinBrackets = match.Groups[1].Value;
                                return "(" + valueWithinBrackets.Replace(varName, atom.Value.ToString()) + ")";
                            });
                        }
                    }
                }
                else
                {
                    continue;
                }
            }

            return parsedRules;
        }

        static List<ParsedRule> PrepareRolesForGrounder(IEnumerable<string> rules)
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

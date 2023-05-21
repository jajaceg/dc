using System.Data;
using System.Text.RegularExpressions;

namespace dc4asp.Grounding
{
    enum Kind
    {
        Rule,
        Constraint
    }

    //TODO rename this class and props
    class ParsedRule : ICloneable
    {
        public object Clone()
        {
            return new ParsedRule
            {
                Kind = Kind,
                Head = Head,
                BodyAtoms = new List<string>(BodyAtoms),
                RoleHasSpecialConstructs = RoleHasSpecialConstructs,
                Constructs = new List<IConstructs>(Constructs)
            };
        }

        public bool IsGrounded { get; set; } = false;
        public Kind Kind { get; set; }
        public string Head { get; set; }
        public List<string> BodyAtoms { get; set; } = new();
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
        public static List<ParsedRule> NaiveGrounding(Dictionary<string, int> facts, IEnumerable<string> rules)
        {
            var parsedRules = PrepareRolesForGrounder(rules);
            var groundedRules = new List<ParsedRule>();

            while (parsedRules.Where(x => x.Kind == Kind.Rule).Any(x => x.IsGrounded == false))
            {
                foreach (var rule in parsedRules.Where(x => x.Kind == Kind.Rule))
                {
                    var knownAtomsInThisRule = GetKnownAtomsInThisRule(facts.Keys, rule.BodyAtoms);
                    IEnumerable<string> knownArgumentNames = GetKnownArgumentNames(knownAtomsInThisRule);
                    List<ParsedRule> newFacts = new() { rule };
                    if (AreAllArgumentsKnownInRule(knownArgumentNames, rule.BodyAtoms))
                    {
                        foreach (var item in knownAtomsInThisRule.ToList())
                        {
                            var factsForThisAtom = facts.Where(x => x.Key.Contains(item.Split('(')[0]));
                            var varName = item.Split('(', ')')[1];

                            List<ParsedRule> tempNewFacts = new();
                            foreach (var partialyGrounded in newFacts)
                            {
                                foreach (var atom in factsForThisAtom)
                                {
                                    ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                                    newRule.BodyAtoms.Clear();
                                    //change head for example blok(L, I) -> blok(2, I)
                                    newRule.Head = Regex.Replace(partialyGrounded.Head, @"\((.*?)\)", match =>
                                    {
                                        string valueWithinBrackets = match.Groups[1].Value;
                                        return "(" + valueWithinBrackets.Replace(varName, atom.Value.ToString()) + ")";
                                    });

                                    //change all body elements blok(L, I) -> blok(2, I)
                                    for (int i = 0; i < partialyGrounded.BodyAtoms.Count; i++)
                                    {
                                        newRule.BodyAtoms.Add(Regex.Replace(partialyGrounded.BodyAtoms[i], @"\((.*?)\)", match =>
                                        {
                                            string valueWithinBrackets = match.Groups[1].Value;
                                            return "(" + valueWithinBrackets.Replace(varName, atom.Value.ToString()) + ")";
                                        }));
                                    }
                                    tempNewFacts.Add(newRule);
                                }
                            }
                            newFacts.Clear();
                            newFacts.AddRange(tempNewFacts);
                        }
                        rule.IsGrounded = true;
                        groundedRules.AddRange(newFacts);
                    }
                    else
                    {
                        continue;
                    }
                    //add new facts
                    foreach (var item in newFacts)
                    {
                        facts.TryAdd(item.Head, facts.Count + 1);
                        foreach (var item2 in item.BodyAtoms)
                        {
                            facts.TryAdd(item2, facts.Count + 1);
                        }
                    }
                }
            }


            return parsedRules;
        }

        private static bool AreAllArgumentsKnownInRule(IEnumerable<string> knownArgumentNames, List<string> bodyAtoms)
        {
            HashSet<string> allArgumentNames = new();
            foreach (var atom in bodyAtoms)
            {
                var letters = Regex.Matches(atom, @"\((.*?)\)")
                    .Cast<Match>()
                    .Select(match => match.Groups[1].Value)
                    .SelectMany(x => x.Split(','))
                    .Select(x => x.Trim())
                    .Where(x => !double.TryParse(x, out _))
                    .Distinct();
                allArgumentNames.UnionWith(letters);
            }

            return allArgumentNames.All(x => knownArgumentNames.Contains(x));
        }

        private static IEnumerable<string> GetKnownArgumentNames(IEnumerable<string> knownAtomsInThisRule)
        {
            return knownAtomsInThisRule.Select(element =>
            {
                var parts = element.Split('(', ')');
                return (parts.Length == 3) ? parts[1].Trim() : element;
            });
        }

        private static IEnumerable<string> GetKnownAtomsInThisRule(Dictionary<string, int>.KeyCollection facts, List<string> bodyAtoms)
        {
            //var factsInRule = bodyAtoms
            //    .Where(IsNumericParentheses)
            //    .Where(atom => facts.Any(fact => atom.Equals(fact))).ToList();

            var factNames = facts.Select(x => x.Split('(').First()).Distinct();
            var atomsInRule = bodyAtoms
                .Where(atom => !IsNumericParentheses(atom))
                .Where(atom => factNames.Any(fact => atom.Split('(').First().Equals(fact)) && atom.Contains('('));

            return atomsInRule;//factsInRule.Concat(atomsInRule);
        }

        public static bool IsNumericParentheses(string element)
        {
            return element.EndsWith(")") && element.Any(char.IsDigit);
        }

        private static List<string> GetKnownArgumentNames(Dictionary<string, int>.KeyCollection keys, List<string> bodyAtoms)
        {
            throw new NotImplementedException();
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
                    parsedRule.BodyAtoms.Add(firstBodyElement);

                    for (int i = 1; i < matches.Count; i++)
                    {
                        var bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                        parsedRule.BodyAtoms.Add(bodyElement);
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
                    parsedRule.BodyAtoms.Add(firstBodyElement);

                    for (int i = 2; i < matches.Count; i++)
                    {
                        string bodyElement = matches[i].Groups[1].Value.RemoveNot().Trim();
                        parsedRule.BodyAtoms.Add(bodyElement);
                    }

                    ParsedRule withExtractedSpecialContructs = ExtractSpecialConstructs(parsedRule);
                    resultList.Add(withExtractedSpecialContructs);
                }

            }
            return resultList;
        }

        private static ParsedRule ExtractSpecialConstructs(ParsedRule parsedRule)
        {
            parsedRule.BodyAtoms.RemoveAll(item =>
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

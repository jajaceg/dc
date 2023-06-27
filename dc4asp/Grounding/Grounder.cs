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
        // NOTATKI
        // 26.06.2023:
        // NAJPIERW NAPRAWIĆ PIERWSZY BŁĄD A PÓŹNIEJ DRUGI
        // - Powinienem zmienić strukturę knownArgumentNames. Tak żeby jeśli podstawię już coś np. podstawiam 'L, I' to nie muszę później już
        //   podstawiać nigdzie 'L' oraz 'I'. Muszę gdzieś zapisywać co już podstawiłem.
        // - Nie radzi sobie z regułą 'jest_w_bloku(L) :- indeks_bloku(I), blok(L, I).'. Dlatego, że z blok bierze string 'L, I' i nie potrafi
        //   później go podstawić do indeks_bloku za 'I'. Muszę oddzielnie wstawiać wartości 'L' oraz 'I' a nie obie na raz.
        // - Aktualnie grondinguje tylko Rules ale zapomniałem o Constraints. Dlatego cała ta logika powinna być w drugim etapie zastosowana
        //   również do Constraints
        public static List<ParsedRule> NaiveGrounding(Dictionary<string, int> facts, IEnumerable<string> rules)
        {
            var parsedRules = PrepareRolesForGrounder(rules);
            List<ParsedRule> groundedRules = new();

            //dopóki w liście reguł jest jakaś reguła która nie jest jeszcze grounded
            while (parsedRules.Where(x => x.Kind == Kind.Rule).Any(x => x.IsGrounded == false))
            {
                foreach (var rule in parsedRules.Where(x => x.Kind == Kind.Rule && x.IsGrounded == false))
                {
                    var knownAtomsInThisRule = GetKnownAtomsInThisRule(facts.Keys, rule.BodyAtoms).ToList(); //!!!!! TUTAJ POWINIENEM WZIĄĆ TYLKO DISTINCTY BO PO CO 2X ROBIĆ TE SAME LITERKI
                    IEnumerable<string> knownArgumentNames = GetKnownArgumentNames(knownAtomsInThisRule);
                    
                    if (!AreAllArgumentsKnownInRule(knownArgumentNames, rule.BodyAtoms))
                        continue;

                    List<ParsedRule> newFacts = new() { rule }; //nazwa może być zła bo nie wiem czy to nie powinno być new rule albo coś innego
                    foreach (var item in knownAtomsInThisRule)
                    {
                        var factsForThisAtom = facts.Where(x => x.Key.AsSpan().StartsWith(item.Split('(')[0]));
                        var varName = item.Split('(', ')')[1];

                        List<ParsedRule> tempNewFacts = new();
                        foreach (var partialyGrounded in newFacts)
                        {
                            foreach (var atom in factsForThisAtom)
                            {
                                ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                                newRule.BodyAtoms.Clear();
                                //change head for example blok(L, I) -> blok(2, I) . 2 is value
                                newRule.Head = Regex.Replace(partialyGrounded.Head, @"\((.*?)\)", match =>
                                {
                                    string valueWithinBrackets = match.Groups[1].Value;
                                    return "(" + valueWithinBrackets.Replace(varName, atom.Key.Split('(', ')')[1]) + ")";
                                });

                                //change all body elements blok(L, I) -> blok(2, I) . 2 is value (not index)
                                for (int i = 0; i < partialyGrounded.BodyAtoms.Count; i++)
                                {
                                    newRule.BodyAtoms.Add(Regex.Replace(partialyGrounded.BodyAtoms[i], @"\((.*?)\)", match =>
                                    {
                                        string valueWithinBrackets = match.Groups[1].Value;
                                        return "(" + valueWithinBrackets.Replace(varName, atom.Key.Split('(', ')')[1]) + ")";
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

                    //add new facts
                    foreach (var item in newFacts)
                    {
                        facts.TryAdd(item.Head, facts.Count);
                        //foreach (var item2 in item.BodyAtoms)
                        //{
                        //    facts.TryAdd(item2, facts.Count); // prawdopodobnie ten kawałek kodu nie jest potrzebny
                        //                                      // bo jak coś jest w body to musi być w innej regule w głowie
                        //}
                    }
                }
            }

            return groundedRules;
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
            string pattern = @"\(([^)]+)\)";

            List<string> elementsInParentheses = knownAtomsInThisRule
                .SelectMany(item => Regex.Matches(item, pattern))
                .SelectMany(match => match.Groups[1].Captures.Cast<Capture>())
                .Select(capture => capture.Value.Split(',').Select(part => part.Trim()))
                .SelectMany(parts => parts)
                .Distinct()
                .ToList();
            return elementsInParentheses;
        }

        private static IEnumerable<string> GetKnownAtomsInThisRule(Dictionary<string, int>.KeyCollection facts, List<string> bodyAtoms)
        {
            var factNames = facts.Select(x => x.Split('(').First()).Distinct();
            var atomsInRule = bodyAtoms
                .Where(atom => !IsNumericParentheses(atom))
                .Where(atom => factNames.Any(fact => atom.Split('(').First().Equals(fact)) && atom.Contains('('));

            return atomsInRule;
        }

        public static bool IsNumericParentheses(string element)
        {
            return element.EndsWith(")") && element.Any(char.IsDigit);
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

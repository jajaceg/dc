using dc4asp.Grounding.Helpers;
using dc4asp.Grounding.Model;
using NCalc;
using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Data;
using System.Text.RegularExpressions;
namespace dc4asp.Grounding;

public class Grounder
{
    public static List<ParsedRule> PrepareData(List<Fact> facts, List<ParsedRule> parsedRules)
    {
        List<ParsedRule> allRules = new();

        var rules = PrepareAtomsFromRules(facts, parsedRules.Where(x => x.Kind == Kind.Rule));
        allRules.AddRange(rules);

        var constraints = PrepareAtomsFromConstraints(facts, parsedRules.Where(x => x.Kind == Kind.Constraint && x.RoleHasSpecialConstructs == false));
        allRules.AddRange(constraints);

        foreach (var rule in parsedRules.Where(x => x.Construct.ConstructType == ConstructType.IsDifferent))
        {
            allRules.AddRange(PrepareIsDifferentConstraints(facts, rule));
        }
        foreach (var rule in parsedRules.Where(x => x.Construct.ConstructType == ConstructType.CreateNewConstant))
        {
            allRules.AddRange(PrepareCreateNewConstantConstraints(facts, rule));
        }

        return allRules;
    }

    private static List<ParsedRule> PrepareAtomsFromRules(List<Fact> facts, IEnumerable<ParsedRule> rules)
    {
        List<ParsedRule> groundedRules = new();

        while (rules.Any(x => x.IsGrounded == false))
        {
            foreach (var rule in rules.Where(x => x.IsGrounded == false))
            {
                var knownAtomsInThisRule = rule.BodyAtoms.Where(x => facts.Any(y => y.Name == x.Name));

                if (!AreAllArgumentsKnownInRule(knownAtomsInThisRule.SelectMany(x => x.Arguments), rule.BodyAtoms))
                    continue;

                List<ParsedRule> newFacts = new() { rule };
                HashSet<string> finished = new();
                foreach (var atomWithName in knownAtomsInThisRule.ToList())
                {
                    if (atomWithName.Arguments.Intersect(finished).Count() == atomWithName.Arguments.Count)
                        continue;

                    var factsForThisAtom = facts.Where(x => x.Name == atomWithName.Name);

                    List<ParsedRule> tempNewFacts = new();
                    foreach (var partialyGrounded in newFacts)
                    {
                        bool partiallyGroundedWasChanged = false;
                        foreach (var atomWithNumArg in factsForThisAtom)
                        {
                            bool wasChanged = false;
                            ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                            for (int i = 0; i < atomWithName.Arguments.Count; i++)
                            {
                                if (finished.Contains(atomWithName.Arguments[i])) continue;

                                var headArgIndex = newRule.Head.Arguments.IndexOf(atomWithName.Arguments[i]);
                                if (headArgIndex != -1)
                                {
                                    newRule.Head.Arguments[headArgIndex] = atomWithNumArg.Arguments[i];
                                    wasChanged = true;
                                    partiallyGroundedWasChanged = true;
                                }

                                newRule.BodyAtoms.ForEach((x) =>
                                {
                                    var bodyArgIndex = x.Arguments.IndexOf(atomWithName.Arguments[i]);
                                    if (bodyArgIndex != -1)
                                    {
                                        x.Arguments[bodyArgIndex] = atomWithNumArg.Arguments[i];
                                        wasChanged = true;
                                        partiallyGroundedWasChanged = true;
                                    }
                                });

                            }
                            if (wasChanged == true)
                                tempNewFacts.Add(newRule);
                        }
                        if (partiallyGroundedWasChanged is false)
                            tempNewFacts.Add(partialyGrounded);
                    }
                    newFacts.Clear();
                    newFacts.AddRange(tempNewFacts);

                    foreach (var item in atomWithName.Arguments)
                    {
                        finished.Add(item);
                    }
                }
                rule.IsGrounded = true;
                groundedRules.AddRange(newFacts);

                foreach (var item in newFacts)
                {
                    var potentialNewAtoms = new Fact(item.Head.NameWithArgs, item.Head.Name, facts.Count + 1, item.Head.Arguments);
                    if (!facts.Any(x => AreAtomsTheSame(x, potentialNewAtoms)))
                        facts.Add(potentialNewAtoms);
                }
            }
        }

        List<ParsedRule> finalResult = new();
        foreach (var item in groundedRules)
        {
            if (!finalResult.Any(x => Compare(x, item)))
                finalResult.Add(item);
        }

        return finalResult;
    }

    private static List<ParsedRule> PrepareAtomsFromConstraints(List<Fact> facts, IEnumerable<ParsedRule> constraints)
    {
        List<ParsedRule> groundedRules = new();

        while (constraints.Any(x => x.IsGrounded == false))
        {
            foreach (var constraint in constraints.Where(x => x.IsGrounded == false))
            {
                var knownAtomsInThisRule = constraint.BodyAtoms.Where(x => facts.Any(y => y.Name == x.Name));

                if (!AreAllArgumentsKnownInRule(knownAtomsInThisRule.SelectMany(x => x.Arguments), constraint.BodyAtoms))
                    throw new Exception("Ograniczenia posiadają nieznane zmienne (albo algorytm ma błąd :O )");

                List<ParsedRule> newFacts = new() { constraint };
                HashSet<string> finished = new();
                foreach (var atomWithName in knownAtomsInThisRule.ToList())
                {
                    if (atomWithName.Arguments.Intersect(finished).Count() == atomWithName.Arguments.Count)
                        continue;

                    var factsForThisAtom = facts.Where(x => x.Name == atomWithName.Name);

                    List<ParsedRule> tempNewFacts = new();
                    foreach (var partialyGrounded in newFacts)
                    {
                        bool partiallyGroundedWasChanged = false;
                        foreach (var atomWithNumArg in factsForThisAtom)
                        {
                            bool wasChanged = false;
                            ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                            for (int i = 0; i < atomWithName.Arguments.Count; i++)
                            {
                                if (finished.Contains(atomWithName.Arguments[i])) continue;

                                newRule.BodyAtoms.ForEach((x) =>
                                {
                                    var bodyArgIndex = x.Arguments.IndexOf(atomWithName.Arguments[i]);
                                    if (bodyArgIndex != -1)
                                    {
                                        x.Arguments[bodyArgIndex] = atomWithNumArg.Arguments[i];
                                        wasChanged = true;
                                        partiallyGroundedWasChanged = true;
                                    }
                                });

                            }
                            if (wasChanged == true)
                                tempNewFacts.Add(newRule);
                        }
                        if (partiallyGroundedWasChanged is false)
                            tempNewFacts.Add(partialyGrounded);
                    }
                    newFacts.Clear();
                    newFacts.AddRange(tempNewFacts);

                    foreach (var item in atomWithName.Arguments)
                    {
                        finished.Add(item);
                    }
                }
                constraint.IsGrounded = true;
                groundedRules.AddRange(newFacts);
            }
        }

        List<ParsedRule> finalResult = new();
        foreach (var item in groundedRules)
        {
            if (!finalResult.Any(x => Compare(x, item)))
                finalResult.Add(item);
        }

        return finalResult;
    }

    class Pair
    {
        public string Value1 { get; set; }
        public string Value1ArgName { get; set; }
        public string Value2 { get; set; }
        public string Value2ArgName { get; set; }
    }

    public static List<ParsedRule> PrepareIsDifferentConstraints(List<Fact> facts, ParsedRule rule)
    {
        HashSet<string> finished = new();
        var contstructValueNames = rule.Construct.ConstructValue.Split("!=").Select(x => x.Trim()).ToList();

        var bodyAtomWithContructValue = rule.BodyAtoms.First(x => x.Arguments.Any(x => x == contstructValueNames[0]));
        var indexOfBodyAtomWithContructValue = bodyAtomWithContructValue.Arguments.IndexOf(contstructValueNames[0]);
        var factsForThisConstraint = facts
            .Where(x => x.Name == bodyAtomWithContructValue.Name)
            .Select(x => x.Arguments[indexOfBodyAtomWithContructValue])
            .Distinct()
            .ToList();
        var pairs = new List<Pair>();
        for (int i = 0; i < factsForThisConstraint.Count; i++)
        {
            for (int j = i + 1; j < factsForThisConstraint.Count; j++)
            {
                pairs.Add(new Pair
                {
                    Value1 = factsForThisConstraint[i],
                    Value1ArgName = contstructValueNames[0],
                    Value2 = factsForThisConstraint[j],
                    Value2ArgName = contstructValueNames[1]
                });
            }
        }
        var newRoles = new List<ParsedRule>();
        foreach (var pair in pairs)
        {
            ParsedRule newRule = (ParsedRule)rule.Clone();
            newRule.BodyAtoms.ForEach((x) =>
            {
                var indexParam1 = x.Arguments.IndexOf(pair.Value1ArgName);
                if (indexParam1 != -1)
                    x.Arguments[indexParam1] = pair.Value1;
                var indexParam2 = x.Arguments.IndexOf(pair.Value2ArgName);
                if (indexParam2 != -1)
                    x.Arguments[indexParam2] = pair.Value2;
            });
            newRoles.Add(newRule);
        };
        finished.Add(contstructValueNames[0]);
        finished.Add(contstructValueNames[1]);

        foreach (var atomWithName in rule.BodyAtoms)
        {
            if (atomWithName.Arguments.Intersect(finished).Count() == atomWithName.Arguments.Count)
                continue;

            var factsForThisAtom = facts.Where(x => x.Name == atomWithName.Name);

            List<ParsedRule> tempNewFacts = new();

            foreach (var partialyGrounded in newRoles)
            {
                bool partiallyGroundedWasChanged = false;
                foreach (var atomWithNumArg in factsForThisAtom)
                {
                    bool wasChanged = false;
                    ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                    for (int i = 0; i < atomWithName.Arguments.Count; i++)
                    {
                        if (finished.Contains(atomWithName.Arguments[i])) continue;

                        if (contstructValueNames.Contains(atomWithName.Arguments[i])) continue;


                        newRule.BodyAtoms.ForEach((x) =>
                        {
                            var bodyArgIndex = x.Arguments.IndexOf(atomWithName.Arguments[i]);
                            if (bodyArgIndex != -1)
                            {
                                x.Arguments[bodyArgIndex] = atomWithNumArg.Arguments[i];
                                partiallyGroundedWasChanged = true;
                                wasChanged = true;
                            }
                        });

                    }
                    if (wasChanged == true)
                        tempNewFacts.Add(newRule);
                }
                if (partiallyGroundedWasChanged is false)
                    tempNewFacts.Add(partialyGrounded);
            }
            newRoles.Clear();
            newRoles.AddRange(tempNewFacts);

            foreach (var item in atomWithName.Arguments)
            {
                finished.Add(item);
            }
        }

        return newRoles;
    }

    public static bool IsEquationSatisfied(string equation, Dictionary<string, int> variables)
    {
        Expression expression = new Expression(equation);

        foreach (var variable in variables)
        {
            expression.Parameters[variable.Key] = variable.Value;
        }

        return (bool)expression.Evaluate();
    }
    public static List<string> GetUniqueTokens(string equation)
    {
        List<string> uniqueTokens = new List<string>();

        var tokens = Regex.Matches(equation, @"\b\w+\b")
            .Cast<Match>()
            .Select(m => m.Value);

        // Dodaj unikalne tokeny do listy
        uniqueTokens.AddRange(tokens.Distinct());

        return uniqueTokens;
    }

    public static List<ParsedRule> PrepareCreateNewConstantConstraints(List<Fact> facts, ParsedRule rule)
    {
        HashSet<string> finished = new();

        List<string> uniqueTokens = GetUniqueTokens(rule.Construct.ConstructValue);
        var bodyAtomWithContructValue = rule.BodyAtoms.First(x => x.Arguments.Any(x => x == uniqueTokens[0]));
        var indexOfBodyAtomWithContructValue = bodyAtomWithContructValue.Arguments.IndexOf(uniqueTokens[0]);
        var factsForThisConstraint = facts
            .Where(x => x.Name == bodyAtomWithContructValue.Name)
            .Select(x => x.Arguments[indexOfBodyAtomWithContructValue])
            .Distinct()
            .ToList();

        var combinations = CombinationGenerator.GenerateCombinations(factsForThisConstraint, uniqueTokens);
        var variables = new List<Dictionary<string, int>>();
        foreach (var combination in combinations)
        {
            var permutations = CombinationGenerator.GeneratePermutations(combination);

            foreach (var item in permutations)
            {
                Dictionary<string, int> keyValuePairs = new();
                for (int i = 0; i < uniqueTokens.Count; i++)
                {
                    keyValuePairs.Add(uniqueTokens[i], Convert.ToInt32(item[i]));
                }
                variables.Add(keyValuePairs);
            }
        }

        var newRoles = new List<ParsedRule>();
        foreach (var item in variables)
        {
            var result = IsEquationSatisfied(rule.Construct.ConstructValue, item);
            if (result)
            {
                ParsedRule newRule = (ParsedRule)rule.Clone();

                newRule.BodyAtoms.ForEach((x) =>
                {
                    foreach (var item2 in uniqueTokens)
                    {
                        var index = x.Arguments.IndexOf(item2);
                        if (index != -1)
                            x.Arguments[index] = item[item2].ToString();
                    }
                });
                newRoles.Add(newRule);
            }
        }
        foreach (var item in uniqueTokens)
        {
            finished.Add(item);
        }

        foreach (var atomWithName in rule.BodyAtoms)
        {
            if (atomWithName.Arguments.Intersect(finished).Count() == atomWithName.Arguments.Count)
                continue;

            var factsForThisAtom = facts.Where(x => x.Name == atomWithName.Name);

            List<ParsedRule> tempNewFacts = new();

            foreach (var partialyGrounded in newRoles)
            {
                bool partiallyGroundedWasChanged = false;
                foreach (var atomWithNumArg in factsForThisAtom)
                {
                    bool wasChanged = false;
                    ParsedRule newRule = (ParsedRule)partialyGrounded.Clone();
                    for (int i = 0; i < atomWithName.Arguments.Count; i++)
                    {
                        if (finished.Contains(atomWithName.Arguments[i])) continue;

                        newRule.BodyAtoms.ForEach((x) =>
                        {
                            var bodyArgIndex = x.Arguments.IndexOf(atomWithName.Arguments[i]);
                            if (bodyArgIndex != -1)
                            {
                                x.Arguments[bodyArgIndex] = atomWithNumArg.Arguments[i];
                                partiallyGroundedWasChanged = true;
                                wasChanged = true;
                            }
                        });

                    }
                    if (wasChanged == true)
                        tempNewFacts.Add(newRule);
                }
                if (partiallyGroundedWasChanged is false)
                    tempNewFacts.Add(partialyGrounded);
            }
            newRoles.Clear();
            newRoles.AddRange(tempNewFacts);

            foreach (var item in atomWithName.Arguments)
            {
                finished.Add(item);
            }
        }

        return newRoles;
    }
    private static bool AreAllArgumentsKnownInRule(IEnumerable<string> knownArgumentNames, List<Atom> bodyAtoms)
    {
        return bodyAtoms.SelectMany(x => x.Arguments).Distinct().All(x => knownArgumentNames.Contains(x));
    }

    private static bool Compare(ParsedRule a, ParsedRule b)
    {
        return JToken.DeepEquals(JToken.FromObject(a), JToken.FromObject(b));
    }

    private static bool AreAtomsTheSame(Fact a, Fact b)
    {
        if (a.Name != b.Name) return false;
        if (a.NameWithArgs != b.NameWithArgs) return false;
        if (!a.Arguments.SequenceEqual(b.Arguments)) return false;

        return true;
    }

    public static List<ImmutableList<int>> Ground(List<Fact> facts, List<ParsedRule> rules)
    {
        List<ImmutableList<int>> result = new();
        foreach (var rule in rules)
        {
            int index = 0;
            if (rule.Kind == Kind.Rule)
            {
                index = FindIndex(facts, rule.Head);
            }
            List<int> indexesFromBody = new() { index };

            foreach (var item in rule.BodyAtoms)
            {
                var indexOfBodyArg = FindIndex(facts, item);
                if (item.IsNegation)
                {
                    indexOfBodyArg *= -1;
                }
                indexesFromBody.Add(indexOfBodyArg);
            }
            result.Add(indexesFromBody.ToImmutableList());
        }

        return result;
    }

    static int FindIndex(List<Fact> facts, Atom atom)
    {
        return facts.First(
            x => x.Name == atom.Name && x.Arguments.SequenceEqual(atom.Arguments))
            .Index;
    }
}

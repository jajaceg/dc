using dc4asp.Grounding.Model;
using Newtonsoft.Json.Linq;
using System.Data;

namespace dc4asp.Grounding;

public class Fact
{
    public string NameWithArgs { get; set; }
    public string Name { get; set; }
    public List<string> Arguments { get; set; }
    public int Index { get; set; }
    public Fact(string nameWithArgs, string name, int index, List<string> arguments)
    {
        NameWithArgs = nameWithArgs;
        Name = name;
        Arguments = arguments;
        Index = index;
    }
}

public class Grounder
{
    public static List<Fact> OwnGrounder(List<Fact> facts, List<ParsedRule> rulesAndConstraints)
    {
        List<ParsedRule> groundedRules = new();

        var rules = rulesAndConstraints.Where(x => x.Kind == Kind.Rule).ToList();

        while (rules.Any(x => x.IsGrounded == false))
        {
            foreach (var rule in rules.Where(x => x.IsGrounded == false))
            {
                var knownAtomsInThisRule = rule.BodyAtoms.Where(x => facts.Any(y => y.Name == x.Name));

                var knownArgumentNames = knownAtomsInThisRule.SelectMany(x => x.Arguments);
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
                                }

                                newRule.BodyAtoms.ForEach((x) =>
                                {
                                    var bodyArgIndex = x.Arguments.IndexOf(atomWithName.Arguments[i]);
                                    if (bodyArgIndex != -1)
                                    {
                                        x.Arguments[bodyArgIndex] = atomWithNumArg.Arguments[i];
                                        wasChanged = true;
                                    }
                                });

                            }
                            if (wasChanged == true)
                                tempNewFacts.Add(newRule);
                        }
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
                    facts.Add(new Fact(item.Head.NameWithArgs, item.Head.Name, facts.Count, item.Head.Arguments));
                }
            }
        }

        return RemoveDuplicates(facts);
    }

    private static bool AreAllArgumentsKnownInRule(IEnumerable<string> knownArgumentNames, List<Atom> bodyAtoms)
    {
        return bodyAtoms.SelectMany(x => x.Arguments).Distinct().All(x => knownArgumentNames.Contains(x));
    }

    private static List<Fact> RemoveDuplicates(List<Fact> rules)
    {
        foreach (var item in rules)
        {
            item.Index = 0;
        }
        List<Fact> uniqueList = new();
        foreach (var item in rules)
        {
            if (!uniqueList.Any(x => Compare(x, item)))
                uniqueList.Add(item);
        }
        for (int i = 0; i < uniqueList.Count; i++)
        {
            uniqueList[i].Index = i;
        }

        return uniqueList;
    }

    private static bool Compare(Fact a, Fact b)
    {
        return JToken.DeepEquals(JToken.FromObject(a), JToken.FromObject(b));
    }
}

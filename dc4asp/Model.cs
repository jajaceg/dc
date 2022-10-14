using System.Collections.Immutable;

namespace dc4asp
{
    public class Model
    {
        public List<ImmutableList<int>> rules;
        // A rule has the following form: head (a positive number
        // representing atom), positive numbers (atoms in the positive
        // part of a body), negative numbers (atoms with not).
        // The length of a rule is at least 1. Constraints are created
        // with 0 in the head.

        public Model()
        {
            rules = new();
        }

        private List<ImmutableList<int>> ComputeReduct(ImmutableHashSet<int> set)
        {
            var reduct = new List<ImmutableList<int>>();
            foreach (var rule in rules)
            {
                bool to_remove = false;
                foreach (int a in rule)
                {
                    if (a < 0)
                    {
                        if (set.Contains(-a))
                        {
                            to_remove = true;
                            break;
                        }
                    }
                }
                if (!to_remove)
                {
                    reduct.Add(rule);
                }
            }
            for (int i = 0; i < reduct.Count; ++i)
            {
                reduct[i] = reduct[i].Where(a => a > 0).ToImmutableList();
            }
            return reduct;
        }

        private ImmutableHashSet<int> AtomicConclusions(List<ImmutableList<int>> reduct)
        {
            HashSet<int> facts = reduct
                .Where(r => r.Count == 1)
                .Select(r => r[0])
                .ToHashSet();
            HashSet<ImmutableList<int>> rules = reduct
                .Where(r => r.Count > 1)
                .ToHashSet();
            bool stop = false;
            while (!stop) {
                stop = true;
                foreach (var r in reduct)
                {
                    if (rules.Contains(r))
                    {
                        if (r.Skip(1).All(a => facts.Contains(a)))
                        {
                            facts.Add(r[0]);
                            rules.Remove(r);
                            stop = false;
                        }
                    }
                }
            }
            return facts.ToImmutableHashSet();
        }

        public bool IsStable(ImmutableHashSet<int> set)
        {
            List<ImmutableList<int>> reduct = ComputeReduct(set);
            ImmutableHashSet<int> atomic_conclusions = AtomicConclusions(reduct);
            return atomic_conclusions.SetEquals(set);
        }

        private bool IsConsistent(ImmutableHashSet<int> set)
        {
            foreach (int a in set)
            {
                if (set.Contains(-a))
                    return false;
            }
            return true;
        }

        private IEnumerable<ImmutableHashSet<int>> DivideAndMerge(int lb, int rb)
        {
            if (lb == rb)
            {
                if (rules[lb].Count == 1)
                {
                    yield return ImmutableHashSet.Create(rules[lb][0]);
                }
                else if (rules[lb].All(a => a > 0))
                {
                    yield return ImmutableHashSet.Create<int>();
                }
                else if (rules[lb][0] == 0)  // a constraint
                {
                    if (rules[lb].Skip(1).All(a => a < 0))
                    {
                        foreach (int a in rules[lb].Skip(1))
                        {
                            yield return ImmutableHashSet.Create(-a);
                        }
                    }
                    else
                    {
                        yield return ImmutableHashSet.Create<int>();
                        foreach (int a in rules[lb].Skip(1))
                        {
                            yield return ImmutableHashSet.Create(-a);
                        }
                    }
                }
                else if (rules[lb].Skip(1).All(a => a < 0))
                {
                    foreach (int a in rules[lb].Skip(1))
                    {
                        yield return ImmutableHashSet.Create(-a);
                    }
                    yield return ImmutableHashSet.Create(rules[lb].ToArray());
                }
                else
                {
                    yield return ImmutableHashSet.Create<int>();
                    yield return ImmutableHashSet.Create(rules[lb].ToArray());
                }
            }
            else
            {
                var left_sets = DivideAndMerge(lb, (lb + rb) / 2);
                var right_sets = DivideAndMerge(1 + (lb + rb) / 2, rb);
                foreach (var left in left_sets)
                {
                    foreach (var right in right_sets)
                    {
                        ImmutableHashSet<int> set = left.Union(right);
                        if (IsConsistent(set))
                        {
                            yield return set;
                        }
                    }
                }
            }
        }

        public IEnumerable<ImmutableHashSet<int>> AnswerSets()
        {
            rules.Shuffle();
            foreach (ImmutableHashSet<int> answer in DivideAndMerge(0, rules.Count - 1))
            {
                var set = answer.Where(a => a > 0).ToImmutableHashSet();
                if (IsStable(set))
                    yield return set;
            }
        }
    }
}

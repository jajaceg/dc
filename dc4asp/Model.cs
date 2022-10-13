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
        // with 1 in the head, and -1 at the end.

        public Model()
        {
            rules = new();
        }

        private List<ImmutableList<int>> computeReduct(ImmutableHashSet<int> set)
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

        private bool isStable(ImmutableHashSet<int> set)
        {
            List<ImmutableList<int>> reduct = computeReduct(set);
            ImmutableHashSet<int> atomic_conclusions = atomicConclusions(reduct);
            return atomic_conclusions.SetEquals(set);
        }

        public IEnumerable<ImmutableHashSet<int>> AnswerSets()
        {
            rules.Shuffle();
            foreach(ImmutableList<int> answer in DivideAndMerge(0, rules.Count - 1))
            {
                var set = answer.Where(a => a > 0).ToImmutableHashSet();
                if (isStable(set))
                    yield return set;
            }
        }
    }
}

using System.Collections.Immutable;
using org.sat4j.core;
using org.sat4j.minisat;
using org.sat4j.specs;
using org.sat4j.tools;

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
                reduct[i] = reduct[i].Where(a => a >= 0).ToImmutableList();
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

        public IEnumerable<ImmutableHashSet<int>> AnswerSets(int num_of_vars)
        {
            ISolver solver = SolverFactory.newDefault();
            solver.newVar(num_of_vars + rules.Count);
            solver.setExpectedNumberOfClauses(rules.Count);
            HashSet<int> not_present_on_lhs = Enumerable.Range(1, num_of_vars).ToHashSet();
            GateTranslator gate = new(solver);
            Dictionary<int, int> varnum_of_rulenum = new();
            List<int>[] rulenums_for_atom = new List<int>[num_of_vars + 1];
            int idx = 0;
            foreach (var r in rules)
            {
                if (r[0] == 0) // a constraint
                {
                    int[] clause = r.Skip(1).Select(a => -a).ToArray();
                    solver.addClause(new VecInt(clause));
                }
                else if (r.Count == 1)
                {
                    not_present_on_lhs.Remove(r[0]);
                    int[] clause = new int[1];
                    clause[0] = r[0];
                    solver.addClause(new VecInt(clause));
                }
                else
                {
                    not_present_on_lhs.Remove(r[0]);
                    varnum_of_rulenum[idx] = ++num_of_vars;
                    if (rulenums_for_atom[r[0]] is null)
                    {
                        rulenums_for_atom[r[0]] = new();
                    }
                    rulenums_for_atom[r[0]].Add(num_of_vars);
                    /*
                    int[] clause = r.Select(a => -a).ToArray();
                    clause[0] = r[0];
                    solver.addClause(new VecInt(clause));
                    */
                }
                ++idx;
            }
            foreach (int atom in not_present_on_lhs)
            {
                int[] clause = new int[1];
                clause[0] = -atom;
                solver.addClause(new VecInt(clause));
            }
            foreach ((int rulenum, int varnum) in varnum_of_rulenum)
            {
                int[] rhs = rules[rulenum].Skip(1).ToArray();
                IConstr[] constrs = gate.and(varnum, new VecInt(rhs));
                foreach (Constr c in constrs)
                {
                    solver.addConstr(c);
                }
            }
            for (int a = 1; a < rulenums_for_atom.Length; ++a)
            {
                if (rulenums_for_atom[a] is not null)
                {
                    int[] rulenums = rulenums_for_atom[a].ToArray();
                    IConstr[] constrs = gate.or(a, new VecInt(rulenums));
                    foreach (Constr c in constrs)
                    {
                        solver.addConstr(c);
                    }
                }
            }
            ModelIterator mi = new ModelIterator(solver);
            IProblem problem = mi;
            bool unsat = true;
            int basic_atoms = rulenums_for_atom.Length - 1;
            while (problem.isSatisfiable())
            {
                unsat = false;
                var answer = problem.model();
                var set = answer.Where(a => a > 0 && a <= basic_atoms).ToImmutableHashSet();
                if (IsStable(set))
                {
                    yield return set;
                }
            }
            if (unsat)
                yield break;
        }
    }
}

using dc4asp;
using dc4asp.Grounding;
using dc4asp.Grounding.Helpers;
using System.Collections.Immutable;

var lines = FileReader.ReadFile("C:\\Users\\jozek\\OneDrive\\mgr\\bloki.lp");

Model model = new();
var factList = Parser.ParseFacts(lines.Where(x => !x.Contains(":-")));
for (var i = 1; i <= factList.Count; i++)
{
    model.rules.Add(ImmutableList.Create(i));
}
var rulesFromFile = Parser.PrepareRulesForGrounder(lines.Where(x => x.Contains(":-")).Select(x => x.Trim()));

var rules = Grounder.PrepareData(factList, rulesFromFile);

ConsoleWriter.WriteAtoms(factList);

List<ImmutableList<int>> rulesForSolver = Grounder.MapFactsAndRulesToInts(factList, rules);

model.rules.AddRange(rulesForSolver);

var answer = model.AnswerSets(model.rules.Count).FirstOrDefault();

ConsoleWriter.WriteSolution(answer, factList);
ConsoleWriter.WriteSolutionIndexes(answer);

using com.sun.source.doctree;
using dc4asp;
using dc4asp.Grounding;
using dc4asp.Grounding.Helpers;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

var lines = FileReader.ReadFile("C:\\Users\\jozek\\OneDrive\\mgr\\bloki.lp");

Model model = new();
var factList = Parser.ParseFacts(lines.Where(x => !x.Contains(":-")));
for (var i = 1; i <= factList.Count; i++)
{
    model.rules.Add(ImmutableList.Create(i));
}

var rulesFromFile = Parser.PrepareRulesForGrounder(lines.Where(x => x.Contains(":-")).Select(x => x.Trim()));

var rules = Grounder.PrepareData(factList, rulesFromFile);

//TODO remove this and save atoms instaed facts
foreach (var item in factList)
{
    var pattern = @"\((.*?)\)";
    var replacedString = Regex.Replace(item.NameWithArgs, pattern, $"({string.Join(",", item.Arguments)})");

    item.NameWithArgs = replacedString;
}

List<ImmutableList<int>> rulesForSolver = Grounder.Ground(factList, rules);

ConsoleWriter.WriteAtoms(factList);
ConsoleWriter.WriteRulesWithIndexes(rulesForSolver, factList);

model.rules.AddRange(rulesForSolver);
var answer = model.AnswerSets(model.rules.Count).FirstOrDefault();
Console.WriteLine();
Console.WriteLine("WYNIK:");
foreach (var item in answer)
{
    var fakt = factList.FirstOrDefault(x => x.Index == item && x.Name == "blok");
    if (fakt is not null)
    {
        Console.WriteLine(item + ": " + fakt.NameWithArgs);
    }
}
Console.WriteLine();
using dc4asp;
using dc4asp.Grounding;
using dc4asp.Grounding.Helpers;
using dc4asp.Grounding.Model;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

//todo read from args
var lines = FileReader.ReadFile("C:\\Users\\jozek\\OneDrive\\mgr\\bloki.lp");

Model model = new();
var factList = Parser.ParseFacts(lines.Where(x => !x.Contains(":-")));
for (var i = 1; i <= factList.Count; i++)
{
    model.rules.Add(ImmutableList.Create(i));
}

var rulesFromFile = Parser.PrepareRulesForGrounder(lines.Where(x => x.Contains(":-")).Select(x => x.Trim()));

var rules = Grounder.PrepareData(factList, rulesFromFile);

//Change blok(L,I) to block(1,3) based on arguments
foreach (var item in rules)
{
    if (item.Kind == Kind.Rule)
    {
        var pattern = @"\((.*?)\)";
        var replacedString = Regex.Replace(item.Head.NameWithArgs, pattern, $"({string.Join(",", item.Head.Arguments)})");

        item.Head.NameWithArgs = replacedString;

    }
    foreach (var atom in item.BodyAtoms)
    {
        var pattern = @"\((.*?)\)";
        var replacedString = Regex.Replace(atom.NameWithArgs, pattern, $"({string.Join(",", atom.Arguments)})");

        atom.NameWithArgs = replacedString;
    }
}

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
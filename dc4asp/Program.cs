using dc4asp;
using dc4asp.Grounding;
using dc4asp.Grounding.Helpers;
using dc4asp.Grounding.Model;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

var lines = FileReader.ReadFile("C:\\Users\\jozek\\OneDrive\\mgr\\bloki.lp");

var initialFacts = Parser.VariablesToConstants(lines.Where(x => !x.Contains(":-")));

List<Fact> factList = new();
for (int i = 0; i < initialFacts.Count; i++)
{
    var arguments = Parser.GetArgumentNames(initialFacts[i]).ToList();
    factList.Add(new Fact(initialFacts[i].Trim(), Parser.GetAtomName(initialFacts[i]), i, arguments));
}

var rulesFromFile = Parser.PrepareRolesForGrounder(lines.Where(x => x.Contains(":-")).Select(x => x.Trim()));
var (Facts, Rules) = Grounder.PrepareData(factList, rulesFromFile);

//Change blok(L,I) to block(1,3) based on arguments
foreach (var item in Rules)
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
foreach (var item in Facts)
{
    var pattern = @"\((.*?)\)";
    var replacedString = Regex.Replace(item.NameWithArgs, pattern, $"({string.Join(",", item.Arguments)})");

    item.NameWithArgs = replacedString;
}

foreach (var item in Facts)
{
    item.Index++;
}
List<ImmutableList<int>> groundedRules = Grounder.Ground(Facts, Rules);

foreach (var item in Facts)
{
    Console.WriteLine("index: " + item.Index + ":    " + item.NameWithArgs);
}

foreach (var item in groundedRules)
{
    Console.WriteLine();
    Console.WriteLine();

    foreach (var item2 in item)
    {
        if (item2 == 0)
            Console.WriteLine(item2 + "   ograniczenie");
        else
            Console.WriteLine(item2 + "     " + Facts.First(x => x.Index == Math.Abs(item2)).NameWithArgs);

    }
}
Model model = new();
model.rules.AddRange(groundedRules);
for (int i = 1; i < 16; i++)
{
    model.rules.Add(ImmutableList.Create<int>(i));

}


var answer = model.AnswerSets(model.rules.Count).FirstOrDefault();
Console.WriteLine();

foreach (var item in answer)
{
    var fakt = Facts.FirstOrDefault(x => x.Index == item && x.Name == "blok");
    if (fakt is not null)
    {
        Console.WriteLine(item + ": " + fakt.NameWithArgs);
    }
}
Console.WriteLine();
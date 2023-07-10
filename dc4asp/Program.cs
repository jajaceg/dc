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
    item.Index++;
}
List<ImmutableList<int>> groundedRules = Grounder.Ground(Facts, Rules);

foreach (var item in Facts)
{
    Console.WriteLine("index: " + item.Index + ":    " + item.NameWithArgs + "   " + string.Join(",", item.Arguments));
}
//foreach (var item in Rules)
//{
//    Console.WriteLine();
//    if(item.Kind == dc4asp.Grounding.Model.Kind.Rule)
//     Console.WriteLine("Head: " + item.Head.Name + "        : " + string.Join(", ", item.Head.Arguments));

//    foreach (var bodyatom in item.BodyAtoms)
//    {
//        Console.WriteLine("index: " + item.BodyAtoms.IndexOf(bodyatom) + "  Name: " + bodyatom.Name + "        : " + string.Join(", ", bodyatom.Arguments));
//    }
//}
Model model = new();
model.rules.AddRange(groundedRules);




foreach (var item in groundedRules)
{
    Console.WriteLine();
    Console.WriteLine();

    foreach (var item2 in item)
    {
        Console.WriteLine(item2);
    }
}




var answer = model.AnswerSets(groundedRules.Count).FirstOrDefault();
Console.WriteLine();
//HashSet<int>[] partition = new HashSet<int>[4];
//for (b = 1; b <= 3; ++b)
//{
//    partition[b] = new();
//}
//foreach (((int number, int block_idx), int var_idx) in block)
//{
//    if (answer!.Contains(var_idx))
//    {
//        partition[block_idx].Add(number);
//    }
//}
//Console.WriteLine($"Block 1: {partition[1].AsText()}");
//Console.WriteLine($"Block 2: {partition[2].AsText()}");
//Console.WriteLine($"Block 3: {partition[3].AsText()}");


//int b, i, j, idx = 1, n = 4;
//Dictionary<string, int> dictionary = new();

//for (i = 1; i <= n; ++i)
//{
//    model.rules.Add(ImmutableList.Create(block[(i, 1)], -block[(i, 2)], -block[(i, 3)]));
//    model.rules.Add(ImmutableList.Create(block[(i, 2)], -block[(i, 1)], -block[(i, 3)]));
//    model.rules.Add(ImmutableList.Create(block[(i, 3)], -block[(i, 1)], -block[(i, 2)]));
//    model.rules.Add(ImmutableList.Create(0, -block[(i, 1)], -block[(i, 2)], -block[(i, 3)]));
//}
//for (i = 1; i <= n; ++i)
//{
//    for (j = i; j <= n; ++j)
//    {
//        if (i + j <= n)
//        {
//            for (b = 1; b <= 3; ++b)
//            {
//                model.rules.Add(ImmutableList.Create(0, block[(i, b)], block[(j, b)], block[(i + j, b)]));
//            }
//        }
//    }
//}

//var answer = model.AnswerSets(idx - 1).FirstOrDefault();
//HashSet<int>[] partition = new HashSet<int>[4];
//for (b = 1; b <= 3; ++b)
//{
//    partition[b] = new();
//}
//foreach (((int number, int block_idx), int var_idx) in block)
//{
//    if (answer!.Contains(var_idx))
//    {
//        partition[block_idx].Add(number);
//    }
//}
//Console.WriteLine($"Block 1: {partition[1].AsText()}");
//Console.WriteLine($"Block 2: {partition[2].AsText()}");
//Console.WriteLine($"Block 3: {partition[3].AsText()}");

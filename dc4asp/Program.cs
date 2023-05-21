using dc4asp;
using dc4asp.Grounding;
using dc4asp.Grounding.Helpers;








var lines = FileReader.ReadFile("C:\\Users\\jozek\\OneDrive\\mgr\\bloki.lp");

var facts = Parser.VariablesToConstants(lines.Where(x => !x.Contains(":-")));
Dictionary<string, int> intFacts = new();


for (int i = 0; i < facts.Count; i++)
{
    intFacts.Add(facts[i].Trim(), i);
}


var rules = Grounder.NaiveGrounding(intFacts, lines.Where(x => x.Contains(":-")).Select(x=>x.Trim()));

Model model = new();
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

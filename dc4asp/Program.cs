using dc4asp;
using System.Collections.Immutable;

Model model = new();
model.rules.Add(ImmutableList.Create(1, -2));    // a :- not b.
model.rules.Add(ImmutableList.Create(2, -1));    // b :- not a.
model.rules.Add(ImmutableList.Create(3, -4));    // c :- not d.
model.rules.Add(ImmutableList.Create(4, -3));    // d :- not c.

foreach (var answer in model.AnswerSets(4))
{
    Console.WriteLine(answer.AsText());
}

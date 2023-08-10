using dc4asp.Grounding.Model;
using System.Collections.Immutable;

namespace dc4asp.Grounding.Helpers;

public static class ConsoleWriter
{
    public static void WriteAtoms(List<Fact> facts)
    {
        foreach (var item in facts)
        {
            Console.WriteLine("   indeks " + item.Index + ":    " + item.NameWithArgs);
        }
    }

    public static void WriteRulesWithIndexes(List<ImmutableList<int>> rulesForSolver, List<Fact> facts)
    {
        foreach (var item in rulesForSolver)
        {
            Console.WriteLine();
            Console.WriteLine();

            foreach (var item2 in item)
            {
                if (item2 == 0)
                    Console.WriteLine(item2 + "   ograniczenie");
                else
                    Console.WriteLine(item2 + "     " + facts.First(x => x.Index == Math.Abs(item2)).NameWithArgs);

            }
        }
    }

    public static void WriteSolution(ImmutableHashSet<int>? answer, List<Fact> factList)
    {
        Console.WriteLine();
        Console.WriteLine("   WYNIK:");
        foreach (var item in answer)
        {
            var fakt = factList.FirstOrDefault(x => x.Index == item);
            if (fakt is not null)
            {
                Console.WriteLine("   Indeks " + item + ": " + fakt.NameWithArgs);
            }
        }
        Console.WriteLine();
    }

    public static void WriteSolutionIndexes(ImmutableHashSet<int>? answer)
    {
        Console.WriteLine();
        Console.WriteLine("   WYNIK (indeksy):");
        foreach (var item in answer)
        {
            Console.WriteLine("   " + item);
        }
        Console.WriteLine();
    }
}

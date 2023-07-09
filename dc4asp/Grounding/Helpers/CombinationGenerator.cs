namespace dc4asp.Grounding.Helpers;

public static class CombinationGenerator
{
    public static List<List<string>> GenerateCombinations(List<string> elements, List<string> k)
    {
        List<List<string>> combinations = new List<List<string>>();
        GenerateCombinationsRecursive(elements, k.Count, 0, new List<string>(), combinations);

        return combinations;
    }

    private static void GenerateCombinationsRecursive(List<string> elements, int k, int startIndex, List<string> currentCombination, List<List<string>> combinations)
    {
        if (k == 0)
        {
            combinations.Add(new List<string>(currentCombination));
            return;
        }

        for (int i = startIndex; i <= elements.Count - k; i++)
        {
            currentCombination.Add(elements[i]);
            GenerateCombinationsRecursive(elements, k - 1, i + 1, currentCombination, combinations);
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
    }

    public static List<List<string>> GeneratePermutations(List<string> elements)
    {
        List<List<string>> permutations = new List<List<string>>();
        GeneratePermutationsRecursive(elements, new List<string>(), permutations);
        return permutations;
    }

    private static void GeneratePermutationsRecursive(List<string> elements, List<string> currentPermutation, List<List<string>> permutations)
    {
        if (elements.Count == 0)
        {
            permutations.Add(new List<string>(currentPermutation));
            return;
        }

        for (int i = 0; i < elements.Count; i++)
        {
            string element = elements[i];
            elements.RemoveAt(i);
            currentPermutation.Add(element);
            GeneratePermutationsRecursive(elements, currentPermutation, permutations);
            currentPermutation.RemoveAt(currentPermutation.Count - 1);
            elements.Insert(i, element);
        }
    }
}

namespace dc4asp.Grounding.Model;

public class Fact
{
    public string NameWithArgs { get; set; }
    public string Name { get; set; }
    public int Index { get; set; }
    public List<string> Arguments { get; set; }
    public Fact(string nameWithArgs, string name, int index)
    {
        NameWithArgs = nameWithArgs;
        Name = name;
        Index = index;
        Arguments = new List<string>();
    }
    public Fact(string nameWithArgs, string name, int index, List<string> arguments)
    {
        NameWithArgs = nameWithArgs;
        Name = name;
        Arguments = arguments;
        Index = index;
    }
}

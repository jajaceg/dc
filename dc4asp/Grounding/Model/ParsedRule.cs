namespace dc4asp.Grounding.Model;

public enum Kind
{
    Rule,
    Constraint
}

public enum ConstructType
{
    None = 0,
    NotEqual = 1, // I1 != I2
    AssignConstant = 2 // Z = X + Y
}

public class Atom
{
    public string NameWithArgs { get; set; }
    public string Name { get; set; }
    public List<string> Arguments { get; set; }
    public bool IsNegation { get; set; }
}
public class Construct
{
    public ConstructType ConstructType { get; set; }
    public string ConstructValue { get; set; }
    public Construct()
    {
        
    }
    public Construct(ConstructType constructType, string constructValue)
    {
        ConstructType = constructType;
        ConstructValue = constructValue;
    }
}

public class ParsedRule : ICloneable
{
    public bool IsGrounded { get; set; } = false;
    public Kind Kind { get; set; }
    public Atom Head { get; set; }
    public List<Atom> BodyAtoms { get; set; } = new();
    public bool RoleHasSpecialConstructs { get; set; }
    public Construct Construct { get; set; } = new();

    public object Clone()
    {
        return new ParsedRule
        {
            Kind = Kind,
            Head = CloneAtom(Head),
            BodyAtoms = CloneAtoms(BodyAtoms),
            RoleHasSpecialConstructs = RoleHasSpecialConstructs,
            Construct = Construct is null ? null : new Construct(Construct.ConstructType, Construct.ConstructValue)
        };
    }

    private List<Atom> CloneAtoms(List<Atom> atoms)
    {
        List<Atom> result = new();
        foreach (var atom in atoms)
        {
            result.Add(
                new Atom
                {
                    NameWithArgs = atom.NameWithArgs,
                    Name = atom.Name,
                    Arguments = new List<string>(atom.Arguments),
                    IsNegation = atom.IsNegation
                });
        }
        return result;
    }
    private static Atom CloneAtom(Atom atom)
    {
        if (atom is null) return null;

        return new Atom
        {
            NameWithArgs = atom.NameWithArgs,
            Name = atom.Name,
            Arguments = new List<string>(atom.Arguments),
            IsNegation = atom.IsNegation
        };
    }
}

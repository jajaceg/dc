using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dc4asp.Grounding.Model;

public enum Kind
{
    Rule,
    Constraint
}

public class Atom
{
    public string NameWithArgs { get; set; }
    public string Name { get; set; }
    public List<string> Arguments { get; set; }
}

public interface IConstructs
{

}

//TODO rename these props
public class IsDifferent : IConstructs
{
    public string IsDifferentConstruct { get; set; } // I1 != I2

    public IsDifferent(string isDifferentConstruct)
    {
        IsDifferentConstruct = isDifferentConstruct;
    }
}
public class CreateNewConstant : IConstructs
{
    public string CreateNewConstantConstruct { get; set; } // Z = X + Y

    public CreateNewConstant(string createNewConstantConstruct)
    {
        CreateNewConstantConstruct = createNewConstantConstruct;
    }
}
public class ParsedRule : ICloneable
{
    public object Clone()
    {
        return new ParsedRule
        {
            Kind = Kind,
            Head = CloneAtom(Head),
            BodyAtoms = CloneAtoms(BodyAtoms),
            RoleHasSpecialConstructs = RoleHasSpecialConstructs,
            Constructs = new List<IConstructs>(Constructs)
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
                    Arguments = new List<string>(atom.Arguments)
                });
        }
        return result;
    }
    private Atom CloneAtom(Atom atom)
    {
        return new Atom
        { 
            NameWithArgs = atom.NameWithArgs, Name = atom.Name, Arguments = new List<string>(atom.Arguments) 
        };
    }

    public bool IsGrounded { get; set; } = false;
    public Kind Kind { get; set; }
    public Atom Head { get; set; }
    public List<Atom> BodyAtoms { get; set; } = new();
    public bool RoleHasSpecialConstructs { get; set; }
    public List<IConstructs> Constructs { get; set; } = new();
}

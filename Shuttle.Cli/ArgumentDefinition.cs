using Shuttle.Contract;

namespace Shuttle.Cli;

public class ArgumentDefinition
{
    public string Description { get; private set; } = string.Empty;
    private readonly List<string> _aliases = [];

    public ArgumentDefinition(string name , params string[] aliases)
    {
        Name = Guard.AgainstEmpty(name);

        _aliases.AddRange(aliases.Where(item => !item.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Distinct());
    }

    public ArgumentDefinition WithDescription(string description)
    {
        Description = Guard.AgainstEmpty(description);
        return this;
    }

    public IEnumerable<string> Aliases => _aliases.AsReadOnly();
    public bool IsRequired { get; private set; }

    public string Name { get; }

    public ArgumentDefinition AsRequired()
    {
        IsRequired = true;

        return this;
    }

    public bool IsSatisfiedBy(string name)
    {
        Guard.AgainstEmpty(name);

        return Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || _aliases.Any(item => item.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }
}
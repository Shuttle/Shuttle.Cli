using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using Shuttle.Contract;
using static System.Text.RegularExpressions.Regex;

namespace Shuttle.Cli;

public class Arguments
{
    private readonly Dictionary<string, ArgumentDefinition> _argumentDefinitions = new();
    private readonly StringDictionary _arguments;
    private readonly Regex _remover = new(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Arguments(params string[] commandLine)
    {
        CommandLine = commandLine;

        _arguments = new();

        string? parameter = null;

        foreach (var input in commandLine)
        {
            if (IsMatch(input, @"^(-{1,2}|/)"))
            {
                if (parameter != null)
                {
                    if (!_arguments.ContainsKey(parameter))
                    {
                        _arguments.Add(parameter.ToLower(), "true");
                    }
                }

                var match = Match(input, @"^(-{1,2}|/)([^=:]+)([=:])(.+)$");
                if (match.Success)
                {
                    var paramName = match.Groups[2].Value;
                    var paramValue = match.Groups[4].Value;

                    paramValue = _remover.Replace(paramValue, "$1");

                    if (!_arguments.ContainsKey(paramName.ToLower()))
                    {
                        _arguments.Add(paramName.ToLower(), paramValue);
                    }

                    parameter = null;
                }
                else
                {
                    parameter = Replace(input, @"^(-{1,2}|/)", "");
                }
            }
            else
            {
                if (parameter == null)
                {
                    continue;
                }

                if (!_arguments.ContainsKey(parameter.ToLower()))
                {
                    var value = _remover.Replace(input, "$1");
                    _arguments.Add(parameter.ToLower(), value);
                }

                parameter = null;
            }
        }

        if (parameter != null && !_arguments.ContainsKey(parameter.ToLower()))
        {
            _arguments.Add(parameter.ToLower(), "true");
        }
    }

    public string[] CommandLine { get; }

    public IEnumerable<ArgumentDefinition> Definitions => _argumentDefinitions.Values.ToList().AsReadOnly();

    public string? this[string name] => _arguments[name];

    public Arguments Add(string key)
    {
        return Add(key, "true");
    }

    public Arguments Add(string key, string value)
    {
        _arguments.Remove(key);
        _arguments.Add(key, value);

        return this;
    }

    public Arguments Add(ArgumentDefinition definition)
    {
        Guard.AgainstNull(definition);

        var key = definition.Name.ToLower();

        if (_argumentDefinitions.Any(existing =>
                existing.Value.IsSatisfiedBy(key) ||
                definition.Aliases.Any(alias => existing.Value.IsSatisfiedBy(alias))))
        {
            throw new InvalidOperationException(string.Format(Resources.DuplicateArgumentDefinitionException, definition.Name));
        }

        _argumentDefinitions.Add(key, definition);

        return this;
    }

    private static T ChangeType<T>(string value)
    {
        return (T)Convert.ChangeType(value, typeof(T));
    }

    public bool Contains(string name)
    {
        var key = name.ToLower();

        if (_arguments.ContainsKey(key))
        {
            return true;
        }

        if (!_argumentDefinitions.Any(pair => pair.Value.IsSatisfiedBy(name)))
        {
            return false;
        }

        var definition = _argumentDefinitions.First(pair => pair.Value.IsSatisfiedBy(name));

        key = definition.Key;

        if (_arguments.ContainsKey(key))
        {
            return true;
        }

        foreach (var alias in definition.Value.Aliases)
        {
            key = alias.ToLower();

            if (_arguments.ContainsKey(key))
            {
                return true;
            }
        }

        return false;
    }

    public static Arguments FromCommandLine()
    {
        // Skip executable name.
        return new(SplitCommandLine(Environment.CommandLine).Skip(1).ToArray());
    }

    public T Get<T>(string name)
    {
        var value = GetArgumentValue(name);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                string.Format(Resources.MissingArgumentException, name));
        }

        return ChangeType<T>(value);
    }

    public T Get<T>(string name, T @default)
    {
        var value = GetArgumentValue(name);

        return string.IsNullOrEmpty(value) ? @default : ChangeType<T>(value);
    }

    private string? GetArgumentValue(string name)
    {
        var key = name.ToLower();

        if (_arguments.ContainsKey(key))
        {
            return _arguments[key];
        }

        if (!_argumentDefinitions.Any(pair => pair.Value.IsSatisfiedBy(name)))
        {
            return string.Empty;
        }

        var definition = _argumentDefinitions.First(pair => pair.Value.IsSatisfiedBy(name));

        key = definition.Key;

        if (_arguments.ContainsKey(key))
        {
            return _arguments[key];
        }

        foreach (var alias in definition.Value.Aliases)
        {
            key = alias.ToLower();

            if (_arguments.ContainsKey(key))
            {
                return _arguments[key];
            }
        }

        return string.Empty;
    }

    public string GetDefinitionText(int consoleWidth = 80, bool required = false, string prefix = "  ")
    {
        consoleWidth = Math.Max(consoleWidth, 40);

        var definitions = required
            ? Definitions.Where(d => d.IsRequired)
            : Definitions;

        var nameColumnWidth = definitions
            .Select(d => ($"--{d.Name}" + (d.Aliases.Any() ? "|" + string.Join("|", d.Aliases.Select(a => $"-{a}")) : "") + (d.IsRequired ? " (required)" : "")).Length)
            .Max();

        var descriptionWidth = consoleWidth - nameColumnWidth - prefix.Length - 2;
        var result = new List<string>();

        foreach (var definition in definitions)
        {
            var fullName = $"--{definition.Name}";
            if (definition.Aliases.Any())
            {
                fullName += "|" + string.Join("|", definition.Aliases.Select(a => $"-{a}"));
            }

            if (definition.IsRequired)
            {
                fullName += " (required)";
            }

            if (string.IsNullOrEmpty(definition.Description))
            {
                result.Add($"{prefix}{fullName}");
                continue;
            }

            var padding = new string(' ', nameColumnWidth - fullName.Length + 2);
            var leftPadding = new string(' ', nameColumnWidth + 2);

            var words = definition.Description.Split(' ');
            var descriptionLines = new List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(currentLine))
                {
                    currentLine = word;
                }
                else if ((currentLine + " " + word).Length <= descriptionWidth)
                {
                    currentLine += " " + word;
                }
                else
                {
                    descriptionLines.Add(currentLine);
                    currentLine = word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                descriptionLines.Add(currentLine);
            }

            result.Add($"{prefix}{fullName}{padding}{descriptionLines[0]}");

            for (var i = 1; i < descriptionLines.Count; i++)
            {
                result.Add($"{prefix}{leftPadding}{descriptionLines[i]}");
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    public bool HasMissingValues()
    {
        foreach (var argumentDefinition in _argumentDefinitions.Values.Where(item => item.IsRequired))
        {
            var found = false;

            foreach (string key in _arguments.Keys)
            {
                if (argumentDefinition.IsSatisfiedBy(key))
                {
                    found = true;
                }
            }

            if (!found)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> SplitCommandLine(string commandLine)
    {
        var inQuotes = false;
        var isEscaped = false;
        var arg = new StringBuilder();

        for (var i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if (isEscaped)
            {
                arg.Append(c);
                isEscaped = false;
            }
            else
                switch (c)
                {
                    case '\\' when i + 1 < commandLine.Length && commandLine[i + 1] == '"':
                    {
                        isEscaped = true;
                        break;
                    }
                    case '"':
                    {
                        inQuotes = !inQuotes;
                        break;
                    }
                    case ' ' when !inQuotes:
                    {
                        {
                            if (arg.Length > 0)
                            {
                                yield return arg.ToString();
                                arg.Clear();
                            }

                            break;
                        }
                    }
                    default:
                    {
                        arg.Append(c);
                        break;
                    }
                }
        }

        if (arg.Length > 0)
        {
            yield return arg.ToString();
        }
    }
}
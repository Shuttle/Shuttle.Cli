# Shuttle.Cli

Provides the `Arguments` class that gives you access to command-line interface arguments:

## Installation

```bash
dotnet add package Shuttle.Cli
```

### Constructor

```cs
public Arguments(params string[] commandLine)
```

The `commandLine` is parsed as arguments starting with `-`, `--` or `/` followed by the argument name then either `=`, `:`, or a space, and then the argument value.

The following are valid arguments:

```batch
-name=value
--name=value
/name=value
-name:value
--name:value
/name:value
-name value
--name value
/name value
```

The argument name and value may be *quoted* with either a single quote (`'`) or double quote (`"`).

### Boolean Flags

You may also pass boolean flags without a value to automatically assign them the value `"true"`:

```batch
-flag
--flag
/flag
```

### Static Factory Method

```cs
public static Arguments FromCommandLine()
```

Creates an `Arguments` instance from the current process command line, automatically skipping the executable name:

```cs
var args = Arguments.FromCommandLine();
```

### Properties

```cs
public string[] CommandLine { get; }
public IEnumerable<ArgumentDefinition> Definitions { get; }
public string? this[string name] { get; }
```

- `CommandLine`: Returns the original command line arguments
- `Definitions`: Returns read-only collection of argument definitions
- `this[string name]`: Indexer to get argument value by name

### Checking for values

```cs
public bool Contains(string name)
```

Returns `true` if the given argument `name` is found; else `false`.

### Getting values

```cs
public T Get<T>(string name)
public T Get<T>(string name, T @default)
```

Returns the value of the given argument `name` as type `T`.  If the argument `name` cannot be found the value given as `@default` will be returned.  If not `@default` is specified an `InvalidOperationException` is thrown.

### Adding arguments programmatically

```cs
public Arguments Add(string key)
public Arguments Add(string key, string value)
```

Add arguments programmatically to the collection:

```cs
args.Add("verbose");
args.Add("output", "results.txt");
```

### Argument definitions

You can add `ArgumentDefinition` entries to an `Arguments` instance by using the following method:

```cs
public Arguments Add(ArgumentDefinition definition)
```

#### ArgumentDefinition constructor

```cs
public ArgumentDefinition(string name, params string[] aliases)
```

Creates a new argument definition with optional aliases:

```cs
var definition = new ArgumentDefinition("input", "i", "in");
```

#### ArgumentDefinition fluent methods

```cs
public ArgumentDefinition WithDescription(string description)
public ArgumentDefinition AsRequired()
```

Configure argument definitions using fluent interface:

```cs
var definition = new ArgumentDefinition("input", "i")
    .WithDescription("Input file path")
    .AsRequired();
```

Argument definitions must have unique keys and if aliases are used these too have to be unique across definitions.  Duplicate aliases within the same argument definition will be ignored.

### Help text generation

```cs
public string GetDefinitionText(int consoleWidth = 80, bool required = false, string prefix = "  ")
```

Generates formatted help text for defined arguments:

```cs
var helpText = args.GetDefinitionText();
Console.WriteLine(helpText);
```

To show only required arguments:

```cs
var requiredHelp = args.GetDefinitionText(required: true);
```

### Validation

```cs
public bool HasMissingValues()
```

Returns `true` if there are any required arguments that have not been specified using either the proper name or an alias:

```cs
if (args.HasMissingValues())
{
    Console.WriteLine("Missing required arguments:");
    Console.WriteLine(args.GetDefinitionText(required: true));
}
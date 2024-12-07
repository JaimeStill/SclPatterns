# System.CommandLine Helpful Patterns

## App and Commands

<details open>

<summary><h4><code>Cli/ICliCommand.cs</code></h4></summary>

```cs

```

</details>

<details open>
<summary><h4><code>Cli/CliCommand.cs</code></h4></summary>

```cs

```

</details>

<details open>
<summary><h4><code>Cli/CliApp.cs</code></h4></summary>

```cs
using System.CommandLine;

namespace SclPatterns.Cli;
public class CliApp
{
    readonly RootCommand root;

    public CliApp(
        string description,
        List<ICliCommand> commands,
        List<Option>? globals = null
    )
    {
        root = new(description);

        if (globals?.Count > 0)
            globals.ForEach(root.AddGlobalOption);

        commands
            .Select(x => x.Build())
            .ToList()
            .ForEach(root.AddCommand);
    }

    public Task InvokeAsync(params string[] args) =>
        root.InvokeAsync(args);
}
```

</details>

<details open>
<summary><h4><code>Program.cs</code></h4>></summary>

```cs

```

</details>

## Runners

## Configuration

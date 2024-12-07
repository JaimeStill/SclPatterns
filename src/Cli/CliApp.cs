using System.CommandLine;

namespace SclPatterns.Cli;
public class CliApp
{
    readonly RootCommand root;
    readonly CliConfig config = new();

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
            .Select(x => x.Build(config))
            .ToList()
            .ForEach(root.AddCommand);
    }

    public Task InvokeAsync(params string[] args) =>
        root.InvokeAsync(args);
}
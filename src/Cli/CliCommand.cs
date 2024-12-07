using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace SclPatterns.Cli;
public abstract class CliCommand(
    string name,
    string description,
    Delegate? @delegate = null,
    List<Option>? options = null,
    List<Option>? globals = null,
    List<ICliCommand>? commands = null
)
: ICliCommand
{
    protected virtual Action<CliConfig>? BuildConfigOptions { get; set; }
    protected readonly string name = name;
    protected readonly string description = description;
    protected readonly Delegate? @delegate = @delegate;
    protected readonly List<ICliCommand>? commands = commands;
    protected List<Option>? options = options;
    protected List<Option>? globals = globals;

    public Command Build(CliConfig config)
    {
        Command command = new(name, description);

        if (@delegate is not null)
            command.Handler = CommandHandler.Create(@delegate);

        if (BuildConfigOptions is not null)
            BuildConfigOptions(config);

        options?.ForEach(command.AddOption);

        globals?.ForEach(command.AddGlobalOption);

        if (commands?.Count > 0)
            commands
                .Select(c => c.Build(config))
                .ToList()
                .ForEach(command.AddCommand);

        return command;
    }

    protected List<Option> AddOptions(ICollection<Option> updates) =>
        AddOptions(updates, ref options);

    protected List<Option> AddGlobalOptions(ICollection<Option> updates) =>
        AddOptions(updates, ref globals);

    protected static List<Option> AddOptions(ICollection<Option> updates, ref List<Option>? options) =>
        options = options is null
            ? [.. updates]
            : [.. options, .. updates];
}
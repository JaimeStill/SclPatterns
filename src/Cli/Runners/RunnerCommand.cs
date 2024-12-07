using System.CommandLine;

namespace SclPatterns.Cli.Runners;
public abstract class RunnerCommand<I>(
    string name,
    string description,
    List<Option>? options = null,
    List<Option>? globals = null,
    List<ICliCommand>? commands = null
)
: CliCommand(
    name,
    description,
    RunnerDelegate<I>.Call,
    options,
    globals,
    commands
)
where I : IRunner
{ }
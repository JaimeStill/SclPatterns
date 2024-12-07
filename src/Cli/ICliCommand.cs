using System.CommandLine;

namespace SclPatterns.Cli;
public interface ICliCommand
{
    Command Build(CliConfig config);
}
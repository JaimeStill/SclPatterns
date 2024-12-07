using System.CommandLine;
using SclPatterns.Cli;
using SclPatterns.Cli.Runners;
using SclPatterns.Runners;

namespace SclPatterns.Commands;
public class EncryptCommand()
: RunnerCommand<EncryptRunner>(
    "encrypt",
    "Encrypt a file and output the result to the specified directory",
    [
        new Option<FileInfo>(
            aliases: ["--source", "-s"],
            description: "File to encrypt"
        )
        { IsRequired = true },
        new Option<DirectoryInfo>(
            aliases: ["--target", "-t"],
            description: "Output directory",
            getDefaultValue: () => CliDefaults.AppPath
        )
    ]
)
{ }
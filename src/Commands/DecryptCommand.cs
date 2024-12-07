using System.CommandLine;
using SclPatterns.Cli;
using SclPatterns.Cli.Runners;
using SclPatterns.Runners;

namespace SclPatterns.Commands;
public class DecryptCommand()
: RunnerCommand<DecryptRunner>(
    "decrypt",
    "Decrypt an encrypted file and output the result to the specified directory",
    [
        new Option<FileInfo>(
            aliases: ["--source", "-s"],
            description: "File to decrypt"
        ) { IsRequired = true },
        new Option<DirectoryInfo>(
            aliases: ["--target", "-t"],
            description: "Output directory",
            getDefaultValue: () => CliDefaults.AppPath
        )
    ]
)
{ }
using System.CommandLine;
using SclPatterns.Cli;

namespace SclPatterns.Commands;
public class FileCommand()
: CliCommand(
    "file",
    "Commands for interfacing with system files",
    commands:
    [
        new DecryptCommand(),
        new EncryptCommand()
    ]
)
{
    protected override Action<CliConfig>? BuildConfigOptions =>
        (CliConfig config) =>
            AddGlobalOptions([
                new Option<Guid>(
                    aliases: ["--key", "-k"],
                    description: "Encryption key. Configurable with SclPatterns:CipherKey.",
                    getDefaultValue: () => config.CipherKey
                )
            ]);
}
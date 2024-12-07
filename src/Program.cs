using SclPatterns.Cli;
using SclPatterns.Commands;

await new CliApp(
    "Demonstrate helpful patterns working with System.CommandLine.",
    [
        new FileCommand()
    ]
)
.InvokeAsync(args);
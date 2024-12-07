using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace SclPatterns.Cli;
public class CliConfig
{
    private readonly SclPatternsOptions Options;
    public Guid CipherKey => Options.CipherKey;

    public CliConfig()
    {
        Options = SclPatternsOptions.FromConfig(
            InitializeConfiguration(
                InitializeEnvironment()
            )
        );
    }

    static string InitializeEnvironment() =>
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

    static IConfiguration InitializeConfiguration(string environment)
    {
        Assembly assembly = Assembly.GetEntryAssembly()
            ?? Assembly.GetExecutingAssembly();

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        /*
            generate a collection of objects that compose a
            pipeline of potential JSON files to read from:
            * Defaults.Configuration - ~/.scl-patterns/appsettings.json
            * Defaults.Configuration - ~/.scl-patterns/appsettings.{environment}.json
            * Directory.GetCurrentDirectory() - appsettings.json co-located with the execution path
            * Directory.GetCurrentDirectory() - appsettings.{environment}.json co-located with the execution path
        */

        foreach (var (path, optional, reloadOnChange) in MapConfigurations(environment))
            builder.AddJsonFile(path, optional, reloadOnChange);

        return builder
            .AddEnvironmentVariables()
            .AddUserSecrets(assembly)
            .Build();
    }

    static List<(string path, bool optional, bool reloadOnChange)> MapConfigurations(string environment) => [
        (Path.Join(CliDefaults.AppPath.FullName, "appsettings.json"), true, true),
        (Path.Join(CliDefaults.AppPath.FullName, $"appsettings.{environment}.json"), true, true),
        ("appsettings.json", true, true),
        ($"appsettings.{environment}.json", true, true)
    ];
}
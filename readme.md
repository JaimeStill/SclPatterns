# System.CommandLine Helpful Patterns

This document, and the corresponding [**repository**](https://github.com/JaimeStill/SclPatterns), illustrate patterns that I've found to be useful when working with the [**`System.CommandLine`**](https://github.com/dotnet/command-line-api) library. It is my hope that they can be of value to anyone who stumbles across it.

The CLI app built out through this demonstration perfoms simple symmetric encryption / decryption of a specified file using a specified **`Guid`** as the private key.

Each section illustrates the implementation of a specific concept, and each subsequent section enhances the capability or development experience of the overall app structure.

The topics introduced are:

- [**App and Commands**](#app-and-commands) - Create infrastructure that simplifies the initialization of a CLI app and the creation of commands / sub-commands. This section also scaffolds the initial project that the rest of the sections will build on.

- [**Runners**](#runners) - Bind the initial state of the command from provided options to the properties of a class that corresponds to a standard delegate definition. This greatly simplifies the process of defining the delegate executed by a command.

* [**Configuration**](#configuration) - Create a configuration pipeline that is fed through the root command to its sub-commands for binding default values based on configuration. This makes it possible to bind default options and globals to an internal `IConfiguration` instance.

> [!NOTE]
> The specific capability demonstrated is trivial and purely intended to illustrate patterns for working with **`System.CommandLine`**.

## App and Commands

This section will form the basis for the rest of the patterns that will be shown. It provides what, in my experience, has been an excellent starting point for building a robust CLI app with numerous sub-commands just by standardizing and simplifying the process of initializing the app and creating the commands.

Initial setup:

1. Create the **`SclPatterns.sln`** file:

   ```bash
   dotnet new sln -n SclPatterns
   ```

2. Initialize the console app:

   ```bash
   dotnet new console -n SclPatterns -o src
   ```

3. Add initial dependencies:

   ```bash
   dotnet add package System.CommandLine --prerelease
   dotnet add package System.CommandLine.NamingConventionBinder --prerelease
   ```

> [!TIP]
> Code snippet headers will indicate the full file path of the file relative to the project root.
>
> If the project is hosted at **`/home/SclPatterns/src`** and a file is located at **`/home/SclPatterns/src/Cli/ICliCommand.cs`**, the header will be **`Cli/ICliCommand.cs`**.

### Core Infrastructure

The files that follow provide the basic primitives for standardizing and simplifying the CLI app.

<details open>

<summary><h4><code>Cli/ICliCommand.cs</code></h4></summary>

All `CliCommand` instances will define a `Build` function that returns a `Command`.

```cs
using System.CommandLine;

namespace SclPatterns.Cli;
public interface ICliCommand
{
    Command Build();
}
```

</details>

<details open>
<summary><h4><code>Cli/CliCommand.cs</code></h4></summary>

A `CliCommand` is initialized with all of the state needed to generate a `Command` through the `Build` method.

`options` defines all of the options purely needed for this command.

`globals` defines all of the options that should be available from this command down to its deepest sub-commands.

Sub-commands are added to the returned `Command` by selecting the result of their own `Build` function.

```cs
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
    protected readonly string name = name;
    protected readonly string description = description;
    protected readonly Delegate? @delegate = @delegate;
    protected readonly List<ICliCommand>? commands = commands;
    protected List<Option>? options = options;
    protected List<Option>? globals = globals;

    public Command Build()
    {
        Command command = new(name, description);

        if (@delegate is not null)
            command.Handler = CommandHandler.Create(@delegate);

        options?.ForEach(command.AddOption);

        globals?.ForEach(command.AddGlobalOption);

        if (commands?.Count > 0)
            commands
                .Select(c => c.Build())
                .ToList()
                .ForEach(command.AddCommand);

        return command;
    }
}
```

</details>

<details open>
<summary><h4><code>Cli/CliApp.cs</code></h4></summary>

The `CliApp` class adds all of its direct commands by selecting the result of their `Build` method and adding it to the `RootCommand`.

`globals` is used to define any options that shuold be globally available from this level down to the deepest sub-command.

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
<summary><h4><code>CliDefaults.cs</code></h4></summary>

The **`CliDefaults`** static class provides a helpful point of reference fo default values that should not change.

```cs
namespace SclPatterns.Cli;
public static class CliDefaults
{
    public static DirectoryInfo AppPath =>
        new(Path.Join(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile
            ),
            ".scl-patterns"
        ));
}
```

</details>

### Example Setup

The files that follow define the starting functionality for the CLI app, which facilitates local file encryption / decryption with a global **`Guid key`** option.

<details open>
<summary><h4><code>Models/EncryptedFile.cs</code></h4></summary>

This class serves as the model for storing metadata and encrypted file data. It also provides methods needed to serialize / deserialize the model instance to and from JSON on the local file system.

```cs
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SclPatterns.Models;
public class EncryptedFile
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Extension { get; set; }
    public required long Size { get; set; }

    public string FullName => $"{Name}{Extension}";
    public string FileName => $"{Name}.encrypted.json";

    public required byte[] Vector { get; set; }
    public required byte[] Data { get; set; }

    public EncryptedFile() { }

    [SetsRequiredMembers]
    public EncryptedFile(
        FileInfo file,
        byte[] vector,
        byte[] data
    )
    {
        Id = Guid.CreateVersion7();
        Name = Path.GetFileNameWithoutExtension(file.Name);
        Extension = file.Extension;
        Size = file.Length;
        Vector = vector;
        Data = data;
    }

    public static EncryptedFile Deserialize(string path)
    {
        string json = File.ReadAllText(path);
        return FromJson(json);
    }

    public FileInfo Serialize(DirectoryInfo target)
    {
        FileInfo result = new(Path.Join(
            target.FullName,
            FileName
        ));

        File.WriteAllText(
            result.FullName,
            ToJson()
        );

        return result;
    }

    static EncryptedFile FromJson(string json) =>
        JsonSerializer.Deserialize<EncryptedFile>(
            json,
            JsonSerializerOptions.Web
        )
        ?? throw new ArgumentException("Value does not deserialize to EncryptedFile");

    string ToJson() =>
        JsonSerializer.Serialize(
            this,
            JsonSerializerOptions.Web
        );
}
```

</details>

<details open>
<summary><h4><code>Commands/EncryptCommand.cs</code></h4></summary>

This command takes a **`FileInfo source`** (required) and a **`DirectoryInfo target`** (default: `CliDefaults.AppPath`), as well as the global **`Guid key`** option, and compresses + encrypts the `source` to the `target` destination.

```cs
using System.CommandLine;
using System.IO.Compression;
using System.Security.Cryptography;
using SclPatterns.Cli;
using SclPatterns.Models;

namespace SclPatterns.Commands;
public class EncryptCommand()
: CliCommand(
    "encrypt",
    "Encrypt a file and output the result to the specified directory",
    new Func<Guid, FileInfo, DirectoryInfo, Task>(Call),
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
{
    static async Task Call(
        Guid key,
        FileInfo source,
        DirectoryInfo target
    )
    {
        if (!target.Exists)
            target.Create();

        using FileStream input = new(source.FullName, FileMode.Open);
        byte[] data = new byte[input.Length];
        await input.ReadExactlyAsync(data.AsMemory(0, data.Length));

        using MemoryStream output = new();
        byte[] iv;

        using (DeflateStream zip = new(output, CompressionLevel.Optimal))
        {
            using Aes aes = Aes.Create();
            aes.Key = key.ToByteArray();
            aes.GenerateIV();
            iv = aes.IV;

            using CryptoStream crypto = new(
                zip,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write
            );

            await output.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length));
            await crypto.WriteAsync(data.AsMemory(0, data.Length));
        }

        EncryptedFile file = new(
            source,
            iv,
            output.ToArray()
        );

        FileInfo result = file.Serialize(target);

        Console.WriteLine($"{source.Name} encrypted to {result.FullName}");
    }
}
```

</details>

<details open>
<summary><h4><code>Commands/DecryptCommand.cs</code></h4></summary>

This command takes a **`FileInfo source`** (representing the path to a serialized `EncryptedFile` object) and a **`DirectoryInfo target`** (defaults to **`CliDefaults.AppPath`**), as well as the global **`Guid key`** option, and decrypts + decompresses the `source.data` and outputs it to the `target` directory.

```cs
using System.CommandLine;
using System.IO.Compression;
using System.Security.Cryptography;
using SclPatterns.Cli;
using SclPatterns.Models;

namespace SclPatterns.Commands;
public class DecryptCommand()
: CliCommand(
    "decrypt",
    "Decrypt an encrypted file and output the result to the specified directory",
    new Func<Guid, FileInfo, DirectoryInfo, Task>(Call),
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
{
    static async Task Call(
        Guid key,
        FileInfo source,
        DirectoryInfo target
    )
    {
        if (!target.Exists)
            target.Create();

        EncryptedFile file = EncryptedFile.Deserialize(source.FullName);


        FileInfo result = new(Path.Join(
            target.FullName,
            file.FullName
        ));

        using FileStream output = new(result.FullName, FileMode.Create);

        using MemoryStream input = new(file.Data);

        byte[] iv = new byte[file.Vector.Length];
        await input.ReadExactlyAsync(
            iv.AsMemory(0, iv.Length)
        );

        byte[] data = new byte[input.Length - iv.Length];
        await input.ReadExactlyAsync(
            data.AsMemory(0, data.Length)
        );

        using DeflateStream zip = new(
            new MemoryStream(data),
            CompressionMode.Decompress
        );

        using Aes aes = Aes.Create();
        aes.Key = key.ToByteArray();
        aes.IV = file.Vector;

        using CryptoStream crypto = new(
            zip,
            aes.CreateDecryptor(),
            CryptoStreamMode.Read
        );

        await crypto.CopyToAsync(output);

        Console.WriteLine($"{file.FileName} decrypted to {result.FullName}");
    }
}
```

</details>

<details open>
<summary><h4><code>Commands/FileCommand.cs</code></h4></summary>

This command serves as the base for the `encrypt` and `decrypt` sub-commands, which both have access to the defined **`Guid key`** global option.

```cs
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
    ],
    globals:
    [
        new Option<Guid>(
            aliases: ["--key", "-k"],
            description: "Encryption key.",
            getDefaultValue: Guid.CreateVersion7
        )
    ]
)
{ }
```

</details>

<details open>
<summary><h4><code>Program.cs</code></h4></summary>

With this infrastructure in place, the **`Program`** file initialization is really clean.

```cs
using SclPatterns.Cli;
using SclPatterns.Commands;

await new CliApp(
    "Demonstrate helpful patterns working with System.CommandLine.",
    [
        new FileCommand()
    ]
)
.InvokeAsync(args);
```

</details>

At this point, this is a functional CLI app.

> [!TIP]
> You can use the included [**`github.css`**](./github.css) file to follow along with the command execution that follows.

<details open>
<summary><h4>Running Encrypt Command</h4></summary>

```sh
# execution
host@computer:~/SclPatterns/src$ dotnet run -- file encrypt -s ~/github.css -k 4c6b7053-b1f1-4016-8a34-02e4c2760712

#output
github.css encrypted to /home/host/.scl-patterns/github.encrypted.json
```

**JSON File**

> [!NOTE]
> The **`data`** property has been redacted for brevity.

```json
{
  "id": "0193a2af-e0b0-75ff-a3ec-307e52782456",
  "name": "github",
  "extension": ".css",
  "size": 18279,
  "fullName": "github.css",
  "fileName": "github.encrypted.json",
  "vector": "iNuQm9TZf5ZXqwiAxWhZ/w==",
  "data": "iNuQm9TZf5ZXqwiAxWhZ/wAPQPC/..."
}
```

</details>

<details open>
<summary><h4>Running Decrypt Command</h4></summary>

```sh
# execution
host@computer:~/SclPatterns/src$ dotnet run -- file decrypt -s ~/.scl-patterns/github.encrypted.json -k 4c6b7053-b1f1-4016-8a34-02e4c2760712
# output
github.encrypted.json decrypted to /home/host/.scl-patterns/github.css
```

The [**`github.css`**](./github.css) file should be rendered at `~/.scl-patterns` directory.

</details>

## Runners

Having to specify a delegate `Func<>`, with all of the options individually specified in the generic signature, is a bit cumbersome. The command and its state + functionality can be decoupled by defining command runner infrastructure.

### Runner Infrastructure

<details open>
<summary><h4><code>Cli/Runners/IRunner.cs</code></h4></summary>

The interface specifies that all implementations will define a simple `Task Execute()` method.

```cs
namespace SclPatterns.Cli.Runners;
public interface IRunner
{
    Task Execute();
}
```

</details>

<details open>
<summary><h4><code>Cli/Runners/RunnerDelegate.cs</code></h4></summary>

This delegate signature is passed to the `@delegate` for any command executing an `IRunner`.

```cs
namespace SclPatterns.Cli.Runners;
public static class RunnerDelegate<I>
where I : IRunner
{
    public static async Task Call(I runner)
    {
        await runner.Execute();
    }
}
```

</details>

<details open>
<summary><h4><code>Cli/Runners/RunnerCommand.cs</code></h4></summary>

This class provides a sub-class of `CliCommand` that passes the `RunnerDelegate<I>.Call` delegate as the base constructor `@delegate` argument and specifies that the `I` generic type implements the `IRunner` interface.

```cs
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
```

</details>

### Runner Implementation

<details open>
<summary><h4><code>Runners/EncryptRunner.cs</code></h4></summary>

The arguments passed into the constructor of `EncryptRunner` are provided by the model-boudn `Option` values defined by the `Command` hierarchy that will execute the `IRunner` instance through the `RunnerDelegate`.

```cs
using System.IO.Compression;
using System.Security.Cryptography;
using SclPatterns.Cli.Runners;
using SclPatterns.Models;

namespace SclPatterns.Runners;
public record EncryptRunner(
    Guid Key,
    FileInfo Source,
    DirectoryInfo Target
)
: IRunner
{
    public async Task Execute()
    {
        if (!Target.Exists)
            Target.Create();

        using FileStream input = new(Source.FullName, FileMode.Open);
        byte[] data = new byte[input.Length];
        await input.ReadExactlyAsync(data.AsMemory(0, data.Length));

        using MemoryStream output = new();
        byte[] iv;

        using (DeflateStream zip = new(output, CompressionLevel.Optimal))
        {
            using Aes aes = Aes.Create();
            aes.Key = Key.ToByteArray();
            aes.GenerateIV();
            iv = aes.IV;

            using CryptoStream crypto = new(
                zip,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write
            );

            await output.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length));
            await crypto.WriteAsync(data.AsMemory(0, data.Length));
        }

        EncryptedFile file = new(
            Source,
            iv,
            output.ToArray()
        );

        FileInfo result = file.Serialize(Target);

        Console.WriteLine($"{Source.Name} encrypted to {result.FullName}");
    }
}
```

</details>

<details open>
<summary><h4><code>Commands/EncryptCommand.cs</code></h4></summary>

The runner infrastructure allows the `EncryptCommand` to be simplified as follows:

```cs
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
```

</details>

<details open>
<summary><h4><code>Runners/DecryptRunner.cs</code></h4></summary>

The `decrypt` functionality can be moved into a runner as well:

```cs
using System.IO.Compression;
using System.Security.Cryptography;
using SclPatterns.Cli.Runners;
using SclPatterns.Models;

namespace SclPatterns.Runners;
public record DecryptRunner(
    Guid Key,
    FileInfo Source,
    DirectoryInfo Target
)
: IRunner
{
    public async Task Execute()
    {
        if (!Target.Exists)
            Target.Create();

        EncryptedFile file = EncryptedFile.Deserialize(Source.FullName);


        FileInfo result = new(Path.Join(
            Target.FullName,
            file.FullName
        ));

        using FileStream output = new(result.FullName, FileMode.Create);

        using MemoryStream input = new(file.Data);

        byte[] iv = new byte[file.Vector.Length];
        await input.ReadExactlyAsync(
            iv.AsMemory(0, iv.Length)
        );

        byte[] data = new byte[input.Length - iv.Length];
        await input.ReadExactlyAsync(
            data.AsMemory(0, data.Length)
        );

        using DeflateStream zip = new(
            new MemoryStream(data),
            CompressionMode.Decompress
        );

        using Aes aes = Aes.Create();
        aes.Key = Key.ToByteArray();
        aes.IV = file.Vector;

        using CryptoStream crypto = new(
            zip,
            aes.CreateDecryptor(),
            CryptoStreamMode.Read
        );

        await crypto.CopyToAsync(output);

        Console.WriteLine($"{file.FileName} decrypted to {result.FullName}");
    }
}
```

</details>

<details open>
<summary><h4><code>Commands/DecryptCommand.cs</code></h4></summary>

```cs
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
```

</details>

## Configuration

Having to pass the encryption key to the commands each time is tedious. Additionally, the **`getDefaultValue`** factory function has to be a compile-time constant (currently `getDefaultValue: Guid.CreateVersion7`), so internally defined command state cannot be used to initialize a value read from configuration.

Defining configuration initialization state on commands from which default values are derived is also not recommnded as each command is built during CLI app initialization regardless of whether it is called or not. This generates a lot of overhead and drastically slows down CLI app startup time.

To solve this, a single configuration pipeline instance can be initialized in the `CliApp` class and fed down to the `Build()` method of each `CliCommand`. Then, an optional `BuildConfigOptions` delegate action can be defined on `CliCommand` to provide the opportunity to specify default values from configuration if the delegate is defined.

The configuration pipeline that will be setup here will load, in order of least to most precedence, as follows:

- **`~/.scl-patterns/appsettings.json`**.
- **`~/.scl-patterns/appsettings.{environment}.json`**.
- **`appsettings.json`** co-located at the execution path.
- **`appsettings.{environment}.json`** co-located at the execution path.
- Environment variables
- User secrets

### Configuration Infrastructure

Install the following NuGet packages:

```sh
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Binder
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add package Microsoft.Extensions.Configuration.FileExtensions
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Configuration.UserSecrets
```

<details open>
<summary><h4><code>SclPatternsOptions.cs</code></h4></summary>

Define the values that can be extracted from configuration, as well as a helper method for retrieving the configuration object.

```cs
using Microsoft.Extensions.Configuration;

namespace SclPatterns;
public record SclPatternsOptions
{
    public Guid CipherKey { get; set; } = Guid.CreateVersion7();

    public static SclPatternsOptions FromConfig(IConfiguration config) =>
        config
            .GetSection("SclPatterns")
            .Get<SclPatternsOptions>()
        ?? new();
}
```

</details>

<details open>
<summary><h4><code>Cli/CliConfig.cs</code></h4></summary>

This class serves as the configuration pipeline that will be initialized in the `CliApp` class.

```cs
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
```

</details>

### Configuration Implementation

The snippets that follow illustrate changes to the existing files that are needed to implement the configuration pipeline.

<details open>
<summary><h4><code>Cli/ICliCommand.cs</code></h4></summary>

The `Build` method signature needs to be modified to receive a `CliConfig config` argument.

```cs
using System.CommandLine;

namespace SclPatterns.Cli;
public interface ICliCommand
{
    Command Build(CliConfig config);
}
```

</details>

> [!IMPORTANT]
> In the code block that follows, existing code has been redacted for brevity and to highlight the changes. See comments for details.

<details open>
<summary><h4><code>Cli/CliCommand.cs</code></h4></summary>

To facilitate the configuration of configuration-based options, the `Action<CliConfig>? BuildConfigOptions` delegate is defined as a virtual property that can be overridden in sub-classes of `CliCommand`.

The `CliConfig` instance is fed into the `Build` method, and passed to the call to `BuildConfigOptions` if it is not null. This instance is also passed to the `Build` command when intializing sub-commands.

```cs
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace SclPatterns.Cli;
public abstract class CliCommand(
    // arguments
)
: ICliCommand
{
    protected virtual Action<CliConfig>? BuildConfigOptions { get; set; }

    // additional properties

    // CliConfig is passed to Build
    public Command Build(CliConfig config)
    {
        Command command = new(name, description);

        if (@delegate is not null)
            command.Handler = CommandHandler.Create(@delegate);

        /*
            If the optional BuildConfigOptions delegate
            is not null, execute it passing config.
        */
        if (BuildConfigOptions is not null)
            BuildConfigOptions(config);

        options?.ForEach(command.AddOption);

        globals?.ForEach(command.AddGlobalOption);

        if (commands?.Count > 0)
            commands
                // pass config to the sub-commands
                .Select(c => c.Build(config))
                .ToList()
                .ForEach(command.AddCommand);

        return command;
    }

    /*
        Helper methods designed to simplify the
        implementations of the BuildConfigOptions
        delegate in sub-classes of CliCommand.
    */
    protected List<Option> AddOptions(ICollection<Option> updates) =>
        AddOptions(updates, ref options);

    protected List<Option> AddGlobalOptions(ICollection<Option> updates) =>
        AddOptions(updates, ref globals);

    protected static List<Option> AddOptions(ICollection<Option> updates, ref List<Option>? options) =>
        options = options is null
            ? [.. updates]
            : [.. options, .. updates];
}
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
    /*
        Initialize the global CliConfig instance
    */
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

        /*
            Pass the CliConfig instance to
            CliApp command initialization.
        */
        commands
            .Select(x => x.Build(config))
            .ToList()
            .ForEach(root.AddCommand);
    }

    public Task InvokeAsync(params string[] args) =>
        root.InvokeAsync(args);
}
```

</details>

<details open>
<summary><h4><code>Commands/FileCommand.cs</code></h4></summary>

By defining the `BuildConfigOptions` delegate, the `getDefaultValue` factory for the key option can now leverage configuration values through the provided `CliConfig` instance.

```cs
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
```

</details>

The [`dotnet user-secrets`](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) tool can be used to initialize and set the `SclPatterns:CipherKey` configuration value:

1. Initialize user secrets for the project:

   ```sh
   dotnet user-secrets init
   ```

2. Set the configuration value:

   **bash**

   ```sh
   dotnet user-secrets set "SclPatterns:CipherKey" $(uuidgen)
   ```

   **PowerShell**

   ```pwsh
   dotnet user-secrets set "SclPatterns:CipherKey" [guid]::NewGuid().ToString()
   ```

3. Verify secret:

   ```sh
   dotnet user-secrets list

   # output
   SclPatterns:CipherKey = 513a7b7a-4421-4dc9-b00b-11e6868c6f99
   ```

4. Execute the help command to verify the default key value:

   ```sh
   dotnet run -- file -h

   # output
   Description:
   Commands for interfacing with system files

   Usage:
   SclPatterns file [command] [options]

   Options:
   -k, --key <key>  Encryption key. Configurable with SclPatterns:CipherKey. [default: 513a7b7a-4421-4dc9-b00b-11e6868c6f99]
   -?, -h, --help   Show help and usage information

   Commands:
   decrypt  Decrypt an encrypted file and output the result to the specified directory
   encrypt  Encrypt a file and output the result to the specified directory
   ```

In addition to user-secrets, you can also specify the configuration values:

- As a `SclPatterns:CipherKey` environment variable.
- In an `appsettings.json` or `appsettings.{environment}.json` file located at either:
  - `~/.scl-patterns/`
  - the path from which the command is executed.

<details>
<summary><h4>Sample <code>appsettings.json</code></h4></summary>

```json
{
  "SclPatterns": {
    "CipherKey": "019363c4-8f8f-710a-9405-889b63791e28"
  }
}
```

</details>

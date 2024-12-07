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
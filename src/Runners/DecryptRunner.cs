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
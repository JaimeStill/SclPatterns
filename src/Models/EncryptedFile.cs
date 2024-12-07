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
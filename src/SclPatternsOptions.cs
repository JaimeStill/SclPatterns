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
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
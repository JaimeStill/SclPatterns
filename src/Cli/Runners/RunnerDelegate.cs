namespace SclPatterns.Cli.Runners;
public static class RunnerDelegate<I>
where I : IRunner
{
    public static async Task Call(I runner)
    {
        await runner.Execute();
    }
}
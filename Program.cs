namespace TheAdventure;

public static class Program
{
    public static async Task Main()
    {
        using var engine = new Engine();

        await engine.RunAsync();
    }
}
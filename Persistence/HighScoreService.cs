using System.Text.Json;

namespace TheAdventure;

public sealed class HighScoreEntry
{
    public int Score { get; set; }
    public DateTimeOffset PlayedAt { get; set; }
}

public sealed class HighScoreService
{
    private const string FileName = "highscores.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<int> GetBestScoreAsync()
    {
        var scores = await LoadScoresAsync();

        if (scores.Count == 0)
        {
            return 0;
        }

        return scores.Max(score => score.Score);
    }

    public async Task SaveScoreAsync(int score)
    {
        var scores = await LoadScoresAsync();

        scores.Add(new HighScoreEntry
        {
            Score = score,
            PlayedAt = DateTimeOffset.Now
        });

        var bestScores = scores
            .OrderByDescending(entry => entry.Score)
            .Take(5)
            .ToList();

        await using var stream = File.Create(FileName);
        await JsonSerializer.SerializeAsync(stream, bestScores, JsonOptions);
    }

    private static async Task<List<HighScoreEntry>> LoadScoresAsync()
    {
        if (!File.Exists(FileName))
        {
            return new List<HighScoreEntry>();
        }

        await using var stream = File.OpenRead(FileName);
        var scores = await JsonSerializer.DeserializeAsync<List<HighScoreEntry>>(stream, JsonOptions);

        return scores ?? new List<HighScoreEntry>();
    }
}
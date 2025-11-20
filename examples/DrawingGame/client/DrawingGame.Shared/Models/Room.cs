using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

public class Room
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("hostUsername")]
    public string HostUsername { get; set; } = string.Empty;

    [JsonPropertyName("maxPlayers")]
    public double MaxPlayers { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "waiting";

    [JsonPropertyName("currentRound")]
    public double CurrentRound { get; set; }

    [JsonPropertyName("totalRounds")]
    public double TotalRounds { get; set; }

    [JsonPropertyName("currentDrawer")]
    public string? CurrentDrawer { get; set; }

    [JsonPropertyName("currentWord")]
    public string? CurrentWord { get; set; }

    [JsonPropertyName("wordOptions")]
    public List<string>? WordOptions { get; set; }

    [JsonPropertyName("roundStartTime")]
    public long? RoundStartTime { get; set; }

    [JsonPropertyName("roundDuration")]
    public double RoundDuration { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "mixed";

    [JsonPropertyName("allowHints")]
    public bool AllowHints { get; set; }

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("finishedAt")]
    public long? FinishedAt { get; set; }

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new();
}

using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

public class CreateRoomResult
{
    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

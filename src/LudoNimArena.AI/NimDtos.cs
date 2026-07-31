using System.Text.Json.Serialization;

namespace LudoNimArena.AI;

/// <summary>Compact game state sent to NIM for a move decision.</summary>
public class NimGameStateDto
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = "";

    [JsonPropertyName("turnId")]
    public string TurnId { get; set; } = "";

    [JsonPropertyName("rollId")]
    public string RollId { get; set; } = "";

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";

    [JsonPropertyName("playerId")]
    public string PlayerColor { get; set; } = "";

    [JsonPropertyName("strategyHint")]
    public string StrategyHint { get; set; } = "";

    [JsonPropertyName("dieResult")]
    public int DieResult { get; set; }

    [JsonPropertyName("consecutiveSixCount")]
    public int ConsecutiveSixCount { get; set; }

    [JsonPropertyName("isBonusRoll")]
    public bool IsBonusRoll { get; set; }

    [JsonPropertyName("tokenPositions")]
    public Dictionary<string, string> TokenPositions { get; set; } = new();

    [JsonPropertyName("safeSquares")]
    public List<int> SafeSquares { get; set; } = new();

    [JsonPropertyName("blockades")]
    public List<BlockadeInfo> Blockades { get; set; } = new();

    [JsonPropertyName("recentEvents")]
    public List<string> RecentEvents { get; set; } = new();

    [JsonPropertyName("legalMoves")]
    public List<NimMoveDto> LegalMoves { get; set; } = new();
}

public class BlockadeInfo
{
    [JsonPropertyName("sharedIndex")]
    public int SharedIndex { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "";
}

public class NimMoveDto
{
    [JsonPropertyName("moveId")]
    public string MoveId { get; set; } = "";

    [JsonPropertyName("tokenId")]
    public string TokenId { get; set; } = "";

    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";

    [JsonPropertyName("entersBoard")]
    public bool EntersBoard { get; set; }

    [JsonPropertyName("captures")]
    public List<string> Captures { get; set; } = new();

    [JsonPropertyName("landsSafe")]
    public bool LandsSafe { get; set; }

    [JsonPropertyName("finishes")]
    public bool Finishes { get; set; }

    [JsonPropertyName("formsBlockade")]
    public bool FormsBlockade { get; set; }
}

/// <summary>Expected NIM response contract.</summary>
public class NimResponseDto
{
    [JsonPropertyName("moveId")]
    public string MoveId { get; set; } = "";

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

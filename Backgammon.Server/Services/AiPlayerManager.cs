using System.Collections.Concurrent;

namespace Backgammon.Server.Services;

/// <summary>
/// Manages AI players for matches, ensuring the same AI player ID is used across all games in a match.
/// Eliminates duplicate AI player logic and prevents AI player ID inconsistencies during match continuation.
/// </summary>
public class AiPlayerManager : IAiPlayerManager
{
    private readonly IAiMoveService _aiMoveService;
    private readonly ConcurrentDictionary<string, (string PlayerId, string AiType)> _matchAiPlayers = new();

    public AiPlayerManager(IAiMoveService aiMoveService)
    {
        _aiMoveService = aiMoveService;
    }

    /// <inheritdoc />
    public string GetOrCreateAiForMatch(string matchId, string aiType = "Greedy")
    {
        var (playerId, _) = _matchAiPlayers.GetOrAdd(matchId, _ =>
            (_aiMoveService.GenerateAiPlayerId(aiType), aiType));
        return playerId;
    }

    /// <inheritdoc />
    public string GetAiNameForMatch(string matchId, string aiType)
    {
        // Use stored AI type if available, otherwise use provided parameter
        if (_matchAiPlayers.TryGetValue(matchId, out var aiInfo))
        {
            aiType = aiInfo.AiType;
        }

        return aiType switch
        {
            "Random" => "Random Bot",
            "random" => "Random Bot",
            "Greedy" => "Greedy Bot",
            "greedy" => "Greedy Bot",
            _ => "Computer"
        };
    }

    /// <inheritdoc />
    public void RemoveMatch(string matchId)
    {
        _matchAiPlayers.TryRemove(matchId, out _);
    }
}

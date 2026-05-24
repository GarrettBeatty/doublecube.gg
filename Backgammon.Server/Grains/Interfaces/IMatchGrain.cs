using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Per-match aggregate grain: owns Orleans-persisted match state (scores, Crawford,
/// correspondence turn tracking) and the in-memory connection map. The grain is the
/// authoritative source for mutating operations; queries that scan many matches still
/// go through <see cref="Services.IMatchService"/> against the Postgres index tables,
/// kept fresh by the grain's dual-write.
///
/// Key = matchId (string).
/// </summary>
public interface IMatchGrain : IGrainWithStringKey
{
    // ==================== Lifecycle ====================

    /// <summary>
    /// Initialize a new regular (real-time) match. Creates the first game record,
    /// persists match state, and returns the data needed to assemble client events.
    /// The grain key must equal the matchId chosen by the caller.
    /// </summary>
    Task<MatchCreationResult> CreateMatchAsync(MatchCreationRequest request);

    /// <summary>
    /// Initialize a new correspondence (async) match. Same contract as
    /// <see cref="CreateMatchAsync"/> with correspondence-specific fields.
    /// </summary>
    Task<MatchCreationResult> CreateCorrespondenceMatchAsync(CorrespondenceMatchCreationRequest request);

    /// <summary>
    /// Join an existing match as player 2 (OpenLobby or Friend). For correspondence
    /// matches, also initializes turn tracking.
    /// </summary>
    Task<MatchJoinResult> JoinAsync(string player2Id, string? player2DisplayName);

    /// <summary>
    /// Create the next game in this match after the previous one completed.
    /// Returns the new game ID.
    /// </summary>
    Task<string> StartNextGameAsync();

    /// <summary>
    /// Update match scores after a game completes. Applies Core.Match business
    /// logic (Crawford, match-complete detection) and returns the post-update state.
    /// </summary>
    Task<MatchCompletionResult> CompleteGameAsync(string gameId, GameCompletionInfo result);

    /// <summary>
    /// Abandon the match. The non-abandoning player wins by default.
    /// </summary>
    Task AbandonAsync(string abandoningPlayerId);

    // ==================== Correspondence ====================

    /// <summary>
    /// Handle turn completion in a correspondence match — updates the current turn
    /// player and deadline.
    /// </summary>
    Task HandleTurnCompletedAsync(string nextPlayerId);

    /// <summary>
    /// Handle a correspondence timeout — forfeits the match against the player whose
    /// deadline expired.
    /// </summary>
    Task HandleTimeoutAsync();

    /// <summary>
    /// Get correspondence-specific state for the match. Returned values are zero/false
    /// for non-correspondence matches.
    /// </summary>
    Task<MatchCorrespondenceInfo> GetCorrespondenceInfoAsync();

    // ==================== Connection tracking (in-memory) ====================

    /// <summary>Get the current game ID for this match.</summary>
    Task<string?> GetCurrentGameIdAsync();

    /// <summary>Set the current active game ID.</summary>
    Task SetCurrentGameIdAsync(string gameId);

    /// <summary>Get all player connection IDs for a player in this match.</summary>
    Task<List<string>> GetPlayerConnectionsAsync(string playerId);

    /// <summary>Update connection tracking for a player in this match.</summary>
    Task TrackPlayerConnectionAsync(string playerId, string connectionId);

    /// <summary>Remove a player connection from tracking.</summary>
    Task RemovePlayerConnectionAsync(string playerId, string connectionId);
}

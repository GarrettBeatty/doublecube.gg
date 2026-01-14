using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Tapper;
using TypedSignalR.Client;

namespace Backgammon.Server.Hubs.Interfaces;

/// <summary>
/// Defines methods that clients can invoke on the server hub.
/// This interface is used by TypedSignalR to generate TypeScript client code.
/// </summary>
[Hub]
public interface IGameHub
{
    // ==================== Game Actions ====================

    /// <summary>
    /// Join an existing game by ID
    /// </summary>
    Task JoinGame(string? gameId);

    /// <summary>
    /// Create an analysis/practice game (single player controls both sides)
    /// </summary>
    Task CreateAnalysisGame();

    /// <summary>
    /// Create a new game against an AI opponent
    /// </summary>
    Task CreateAiGame();

    /// <summary>
    /// Roll dice to start turn
    /// </summary>
    Task RollDice();

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    Task MakeMove(int from, int to);

    /// <summary>
    /// End current turn and switch to opponent
    /// </summary>
    Task EndTurn();

    /// <summary>
    /// Undo the last move made during current turn
    /// </summary>
    Task UndoLastMove();

    /// <summary>
    /// Abandon the current game (opponent wins)
    /// </summary>
    Task AbandonGame();

    /// <summary>
    /// Leave the current game
    /// </summary>
    Task LeaveGame();

    /// <summary>
    /// Request current game state
    /// </summary>
    Task GetGameState();

    // ==================== Doubling Actions ====================

    /// <summary>
    /// Offer to double the stakes
    /// </summary>
    Task OfferDouble();

    /// <summary>
    /// Accept a double offer
    /// </summary>
    Task AcceptDouble();

    /// <summary>
    /// Decline a double offer (opponent wins at current stakes)
    /// </summary>
    Task DeclineDouble();

    // ==================== Move Validation ====================

    /// <summary>
    /// Get list of points with moveable checkers
    /// </summary>
    Task<List<int>> GetValidSources();

    /// <summary>
    /// Get valid destination moves from a source point
    /// </summary>
    Task<List<MoveDto>> GetValidDestinations(int fromPoint);

    // ==================== Match Actions ====================

    /// <summary>
    /// Create a new match with configuration
    /// </summary>
    Task CreateMatch(MatchConfig config);

    /// <summary>
    /// Join an existing match as player 2
    /// </summary>
    Task JoinMatch(string matchId);

    /// <summary>
    /// Continue to the next game in a match
    /// </summary>
    Task ContinueMatch(string matchId);

    /// <summary>
    /// Get match status
    /// </summary>
    Task GetMatchStatus(string matchId);

    /// <summary>
    /// Get match state with score and Crawford info
    /// </summary>
    Task<MatchStateDto?> GetMatchState(string matchId);

    /// <summary>
    /// Get complete match results including all games
    /// </summary>
    Task<MatchResultsDto?> GetMatchResults(string matchId);

    /// <summary>
    /// Get player's matches, optionally filtered by status
    /// </summary>
    Task GetMyMatches(string? status);

    /// <summary>
    /// Get available match lobbies
    /// </summary>
    Task<List<MatchLobbyDto>> GetMatchLobbies(string? lobbyType);

    /// <summary>
    /// Get player's active (in-progress) games
    /// </summary>
    Task<List<ActiveGameDto>> GetActiveGames(int limit);

    /// <summary>
    /// Get player's active (in-progress) matches
    /// </summary>
    Task<List<ActiveMatchDto>> GetActiveMatches(int limit);

    /// <summary>
    /// Get all games for a specific match
    /// </summary>
    Task<List<MatchGameDto>> GetMatchGames(string matchId);

    /// <summary>
    /// Get player's recent completed games
    /// </summary>
    Task<List<RecentGameDto>> GetRecentGames(int limit);

    /// <summary>
    /// Get recent opponents with head-to-head records
    /// </summary>
    Task<List<RecentOpponentDto>> GetRecentOpponents(int limit, bool includeAi);

    // ==================== Correspondence Actions ====================

    /// <summary>
    /// Get all correspondence games for the current user
    /// </summary>
    Task<CorrespondenceGamesResponse> GetCorrespondenceGames();

    /// <summary>
    /// Create a new correspondence match
    /// </summary>
    Task CreateCorrespondenceMatch(MatchConfig config);

    /// <summary>
    /// Notify that a turn has been completed in a correspondence game
    /// </summary>
    Task NotifyCorrespondenceTurnComplete(string matchId, string nextPlayerId);

    // ==================== Analysis Mode Actions ====================

    /// <summary>
    /// Set dice values manually (analysis mode only)
    /// </summary>
    Task SetDice(int die1, int die2);

    /// <summary>
    /// Set the current player (analysis mode only)
    /// </summary>
    Task SetCurrentPlayer(CheckerColorDto color);

    /// <summary>
    /// Move a checker directly bypassing game rules (analysis mode only)
    /// </summary>
    Task MoveCheckerDirectly(int from, int to);

    /// <summary>
    /// Export current position as base64-encoded SGF
    /// </summary>
    Task<string> ExportPosition();

    /// <summary>
    /// Import a position from SGF or base64
    /// </summary>
    Task ImportPosition(string positionData);

    // ==================== Analysis Actions ====================

    /// <summary>
    /// Analyze the current position
    /// </summary>
    Task<PositionEvaluationDto> AnalyzePosition(string gameId, string? evaluatorType);

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    Task<BestMovesAnalysisDto> FindBestMoves(string gameId, string? evaluatorType);

    /// <summary>
    /// Get turn-by-turn history for a completed game for analysis board replay
    /// </summary>
    Task<GameHistoryDto?> GetGameHistory(string gameId);

    // ==================== Chat ====================

    /// <summary>
    /// Send a chat message to all players in the game
    /// </summary>
    Task SendChatMessage(string message);

    // ==================== Friends ====================

    /// <summary>
    /// Get the current user's friends list
    /// </summary>
    Task<List<FriendDto>> GetFriends();

    /// <summary>
    /// Get pending friend requests
    /// </summary>
    Task<List<FriendDto>> GetFriendRequests();

    /// <summary>
    /// Search for players by username
    /// </summary>
    Task<List<PlayerSearchResultDto>> SearchPlayers(string query);

    /// <summary>
    /// Send a friend request
    /// </summary>
    Task<bool> SendFriendRequest(string toUserId);

    /// <summary>
    /// Accept a friend request
    /// </summary>
    Task<bool> AcceptFriendRequest(string friendUserId);

    /// <summary>
    /// Decline a friend request
    /// </summary>
    Task<bool> DeclineFriendRequest(string friendUserId);

    /// <summary>
    /// Remove a friend
    /// </summary>
    Task<bool> RemoveFriend(string friendUserId);

    // ==================== Profile ====================

    /// <summary>
    /// Get player profile data
    /// </summary>
    Task<PlayerProfileDto?> GetPlayerProfile(string username);

    // ==================== Daily Puzzle ====================

    /// <summary>
    /// Get today's daily puzzle
    /// </summary>
    Task<DailyPuzzleDto?> GetDailyPuzzle();

    /// <summary>
    /// Submit an answer to today's puzzle
    /// </summary>
    Task<PuzzleResultDto> SubmitPuzzleAnswer(List<MoveDto> moves);

    /// <summary>
    /// Give up on today's puzzle and reveal the answer
    /// </summary>
    Task<PuzzleResultDto> GiveUpPuzzle();

    /// <summary>
    /// Get user's puzzle streak information
    /// </summary>
    Task<PuzzleStreakInfo> GetPuzzleStreak();

    /// <summary>
    /// Get a historical puzzle by date
    /// </summary>
    Task<DailyPuzzleDto?> GetHistoricalPuzzle(string date);

    /// <summary>
    /// Get valid moves for a puzzle position with pending moves applied.
    /// </summary>
    Task<List<MoveDto>> GetPuzzleValidMoves(PuzzleValidMovesRequest request);

    // ==================== Leaderboard & Players ====================

    /// <summary>
    /// Get the leaderboard with top rated players
    /// </summary>
    Task<List<LeaderboardEntryDto>> GetLeaderboard(int limit);

    /// <summary>
    /// Get online players
    /// </summary>
    Task<List<OnlinePlayerDto>> GetOnlinePlayers();

    /// <summary>
    /// Get rating distribution statistics
    /// </summary>
    Task<RatingDistributionDto> GetRatingDistribution();

    /// <summary>
    /// Get available bots to play against
    /// </summary>
    Task<List<BotInfoDto>> GetAvailableBots();
}

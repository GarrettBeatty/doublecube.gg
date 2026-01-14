using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Factory for creating and configuring GameSession instances.
/// Centralizes session creation logic to eliminate duplication.
/// </summary>
public class GameSessionFactory : IGameSessionFactory
{
    private readonly IGameSessionManager _sessionManager;
    private readonly ILogger<GameSessionFactory> _logger;

    public GameSessionFactory(
        IGameSessionManager sessionManager,
        ILogger<GameSessionFactory> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new game session for a match game.
    /// </summary>
    public GameSession CreateMatchGameSession(Match match, string gameId)
    {
        _logger.LogDebug(
            "Creating match game session: GameId={GameId}, MatchId={MatchId}",
            gameId,
            match.MatchId);

        var session = _sessionManager.CreateGame(gameId);

        // Configure player IDs from match (Player1 = White, Player2 = Red)
        // This ensures correct color assignment even when AI joins first
        session.SetWhitePlayer(match.Player1Id);
        if (!string.IsNullOrEmpty(match.Player2Id))
        {
            session.SetRedPlayer(match.Player2Id);
        }

        // Configure match context
        session.WhitePlayerName = match.Player1Name;
        session.RedPlayerName = match.Player2Name ?? "Waiting...";
        session.MatchId = match.MatchId;

        // Configure match scores
        session.TargetScore = match.TargetScore;
        session.Player1Score = match.Player1Score;
        session.Player2Score = match.Player2Score;
        session.IsCrawfordGame = match.IsCrawfordGame;

        // Configure Crawford rule if applicable
        if (match.IsCrawfordGame)
        {
            session.Engine.IsCrawfordGame = true;
            session.Engine.MatchId = match.MatchId;

            _logger.LogDebug(
                "Configured Crawford rule for game {GameId}",
                gameId);
        }

        // Configure time controls if enabled
        if (match.TimeControl != null && match.TimeControl.Type != TimeControlType.None)
        {
            ConfigureTimeControl(session, match);
        }

        // Set rated/unrated flag (AI matches are always unrated)
        session.IsRated = match.OpponentType == "AI" ? false : match.IsRated;

        _logger.LogInformation(
            "Created match game session {GameId} for match {MatchId} (Crawford: {IsCrawford}, Rated: {IsRated})",
            gameId,
            match.MatchId,
            match.IsCrawfordGame,
            session.IsRated);

        return session;
    }

    /// <summary>
    /// Creates a new game session for analysis mode.
    /// </summary>
    public GameSession CreateAnalysisSession(string gameId)
    {
        _logger.LogDebug("Creating analysis session: GameId={GameId}", gameId);

        var session = _sessionManager.CreateGame(gameId);
        session.IsRated = false;
        session.WhitePlayerName = "Analysis";
        session.RedPlayerName = "Analysis";

        _logger.LogInformation("Created analysis session {GameId}", gameId);

        return session;
    }

    /// <summary>
    /// Initializes a newly created session (rolls dice, etc.)
    /// </summary>
    public void InitializeSession(GameSession session)
    {
        _logger.LogDebug("Initializing session {GameId}", session.Id);

        session.Engine.StartNewGame();
        session.Engine.RollDice();

        _logger.LogDebug(
            "Session {GameId} initialized with dice: [{Die1}, {Die2}]",
            session.Id,
            session.Engine.Dice.Die1,
            session.Engine.Dice.Die2);
    }

    private void ConfigureTimeControl(GameSession session, Match match)
    {
        if (match.TimeControl == null)
        {
            return;
        }

        session.TimeControl = match.TimeControl;

        // Calculate reserve times based on current match score
        var whiteReserve = match.TimeControl.CalculateReserveTime(
            match.TargetScore,
            match.Player1Score,
            match.Player2Score);
        var redReserve = whiteReserve; // Same for both players

        session.Engine.InitializeTimeControl(match.TimeControl, whiteReserve, redReserve);

        _logger.LogInformation(
            "Initialized time control for game {GameId}: Type={Type}, Reserve={Reserve}min",
            session.Id,
            match.TimeControl.Type,
            whiteReserve.TotalMinutes);
    }
}

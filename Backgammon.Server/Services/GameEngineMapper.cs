using System.Reflection;
using Backgammon.Core;
using Backgammon.Server.Models;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Services;

/// <summary>
/// Maps between GameSession/GameEngine and Game (database) model.
/// Handles serialization for persistent storage and deserialization for server restart.
/// </summary>
public static class GameEngineMapper
{
    /// <summary>
    /// Convert GameSession to Game model for database storage.
    /// Captures complete game state including board, players, dice, and move history.
    /// </summary>
    /// <summary>
    /// Convert GameSession to Game model for database storage.
    /// Captures complete game state including board, players, dice, and move history.
    /// </summary>
    public static ServerGame ToGame(GameSession session)
    {
        var engine = session.Engine;

        var whiteUserId = IsRegisteredUserId(session.WhitePlayerId) ? session.WhitePlayerId : null;
        var redUserId = IsRegisteredUserId(session.RedPlayerId) ? session.RedPlayerId : null;

        var game = new ServerGame
        {
            GameId = session.Id,
            WhitePlayerId = session.WhitePlayerId,
            RedPlayerId = session.RedPlayerId,
            WhiteUserId = whiteUserId,
            RedUserId = redUserId,
            WhitePlayerName = session.WhitePlayerName,
            RedPlayerName = session.RedPlayerName,
            Status = engine.Winner != null ? "Completed" : "InProgress",
            GameStarted = engine.GameStarted,
            BoardState = SerializeBoardState(engine.Board),
            WhiteCheckersOnBar = engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = engine.WhitePlayer.CheckersBornOff,
            RedBornOff = engine.RedPlayer.CheckersBornOff,
            CurrentPlayer = engine.CurrentPlayer?.Color.ToString() ?? "White",
            Die1 = engine.Dice.Die1,
            Die2 = engine.Dice.Die2,
            RemainingMoves = new List<int>(engine.RemainingMoves),
            DoublingCubeValue = engine.DoublingCube.Value,
            DoublingCubeOwner = engine.DoublingCube.Owner?.ToString(),
            Moves = engine.MoveHistory.Select(m => m.ToNotation()).ToList(),
            MoveCount = engine.MoveHistory.Count,
            GameSgf = engine.GameSgf,
            CreatedAt = session.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow
        };

        // Check if this is an AI game
        game.IsAiOpponent = IsAiPlayer(session.WhitePlayerId) || IsAiPlayer(session.RedPlayerId);

        // Set rated/unrated flag (AI games are always unrated regardless of session setting)
        game.IsRated = game.IsAiOpponent ? false : session.IsRated;

        // Copy match properties from session
        game.MatchId = session.MatchId;
        game.IsCrawfordGame = session.IsCrawfordGame ?? false;

        // Validate: if session has match context (TargetScore set) but no MatchId, fail fast
        // This indicates a programming error - MatchId should always be set for match games
        if (session.TargetScore.HasValue && session.TargetScore > 0 && string.IsNullOrEmpty(session.MatchId))
        {
            throw new InvalidOperationException(
                $"Game {session.Id} has TargetScore={session.TargetScore} but MatchId is null. " +
                "MatchId must be set on the session before saving match games.");
        }

        // If game is completed, add completion data
        if (engine.Winner != null)
        {
            game.Winner = engine.Winner.Color.ToString();
            game.WinType = engine.DetermineWinType().ToString();
            game.Stakes = engine.GetGameResult();
            game.CompletedAt = DateTime.UtcNow;
            game.DurationSeconds = (int)(DateTime.UtcNow - session.CreatedAt).TotalSeconds;
        }

        return game;
    }

    /// <summary>
    /// Restore GameSession from Game model (for server restart).
    /// Reconstructs the complete game state including board position and turn state.
    /// </summary>
    public static GameSession FromGame(ServerGame game)
    {
        var session = new GameSession(game.GameId);

        var createdAtField = typeof(GameSession)
            .GetField("<CreatedAt>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (createdAtField != null)
        {
            createdAtField.SetValue(session, game.CreatedAt);
        }

        if (!string.IsNullOrEmpty(game.WhitePlayerId))
        {
            session.AddPlayer(game.WhitePlayerId, string.Empty);
            session.WhitePlayerName = game.WhitePlayerName;
        }

        if (!string.IsNullOrEmpty(game.RedPlayerId))
        {
            session.AddPlayer(game.RedPlayerId, string.Empty);
            session.RedPlayerName = game.RedPlayerName;
        }

        var engine = session.Engine;

        RestoreBoardState(engine.Board, game.BoardState);

        engine.WhitePlayer.CheckersOnBar = game.WhiteCheckersOnBar;
        engine.RedPlayer.CheckersOnBar = game.RedCheckersOnBar;
        engine.WhitePlayer.CheckersBornOff = game.WhiteBornOff;
        engine.RedPlayer.CheckersBornOff = game.RedBornOff;

        engine.Dice.SetDice(game.Die1, game.Die2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(game.RemainingMoves);

        if (game.DoublingCubeValue > 1)
        {
            var cubeValueField = engine.DoublingCube.GetType()
                .GetField("<Value>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (cubeValueField != null)
            {
                cubeValueField.SetValue(engine.DoublingCube, game.DoublingCubeValue);
            }

            if (game.DoublingCubeOwner != null)
            {
                var owner = Enum.Parse<CheckerColor>(game.DoublingCubeOwner);
                var cubeOwnerField = engine.DoublingCube.GetType()
                    .GetField("<Owner>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (cubeOwnerField != null)
                {
                    cubeOwnerField.SetValue(engine.DoublingCube, owner);
                }
            }
        }

        var currentPlayer = game.CurrentPlayer == "White"
            ? engine.WhitePlayer
            : engine.RedPlayer;

        var currentPlayerField = engine.GetType()
            .GetField("<CurrentPlayer>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (currentPlayerField != null)
        {
            currentPlayerField.SetValue(engine, currentPlayer);
        }

        if (game.GameStarted)
        {
            var gameStartedField = engine.GetType()
                .GetField("<GameStarted>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (gameStartedField != null)
            {
                gameStartedField.SetValue(engine, true);
            }
        }

        // Restore winner if game is completed/abandoned
        if (!string.IsNullOrEmpty(game.Winner))
        {
            var winnerColor = Enum.Parse<CheckerColor>(game.Winner);
            engine.Winner = winnerColor == CheckerColor.White
                ? engine.WhitePlayer
                : engine.RedPlayer;
        }

        ValidateRestoredState(engine);

        return session;
    }

    /// <summary>
    /// Check if a player ID represents an AI opponent
    /// </summary>
    public static bool IsAiPlayer(string? playerId)
    {
        return playerId?.StartsWith("ai_") == true;
    }

    /// <summary>
    /// Serialize board state to list of point DTOs
    /// </summary>
    private static List<PointStateDto> SerializeBoardState(Board board)
    {
        var states = new List<PointStateDto>();

        for (int i = 1; i <= 24; i++)
        {
            var point = board.GetPoint(i);
            states.Add(new PointStateDto
            {
                Position = i,
                Color = point.Color?.ToString(),
                Count = point.Count
            });
        }

        return states;
    }

    /// <summary>
    /// Restore board state from list of point DTOs
    /// </summary>
    private static void RestoreBoardState(Board board, List<PointStateDto> states)
    {
        // Clear all points first
        for (int i = 1; i <= 24; i++)
        {
            board.GetPoint(i).Checkers.Clear();
        }

        // Restore each point
        foreach (var state in states)
        {
            if (state.Color != null && state.Count > 0)
            {
                var color = Enum.Parse<CheckerColor>(state.Color);
                var point = board.GetPoint(state.Position);

                for (int i = 0; i < state.Count; i++)
                {
                    point.AddChecker(color);
                }
            }
        }
    }

    /// <summary>
    /// Validate that restored game state is consistent
    /// </summary>
    private static void ValidateRestoredState(GameEngine engine)
    {
        // Check total white checkers = 15
        var totalWhite = CountCheckersOnBoard(engine.Board, CheckerColor.White) +
                         engine.WhitePlayer.CheckersOnBar +
                         engine.WhitePlayer.CheckersBornOff;

        if (totalWhite != 15)
        {
            throw new InvalidOperationException(
                $"Invalid restored state: White has {totalWhite} checkers (expected 15)");
        }

        // Check total red checkers = 15
        var totalRed = CountCheckersOnBoard(engine.Board, CheckerColor.Red) +
                       engine.RedPlayer.CheckersOnBar +
                       engine.RedPlayer.CheckersBornOff;

        if (totalRed != 15)
        {
            throw new InvalidOperationException(
                $"Invalid restored state: Red has {totalRed} checkers (expected 15)");
        }
    }

    /// <summary>
    /// Count checkers of a specific color on the board
    /// </summary>
    private static int CountCheckersOnBoard(Board board, CheckerColor color)
    {
        int count = 0;
        for (int i = 1; i <= 24; i++)
        {
            var point = board.GetPoint(i);
            if (point.Color == color)
            {
                count += point.Count;
            }
        }

        return count;
    }

    /// <summary>
    /// Check if a player ID looks like a registered user ID (GUID format)
    /// </summary>
    private static bool IsRegisteredUserId(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return false;
        }

        return Guid.TryParse(playerId, out _);
    }
}

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
    public static ServerGame ToGame(GameSession session)
    {
        var engine = session.Engine;

        // Determine if players are authenticated users (GUID format vs anonymous)
        var whiteUserId = IsRegisteredUserId(session.WhitePlayerId) ? session.WhitePlayerId : null;
        var redUserId = IsRegisteredUserId(session.RedPlayerId) ? session.RedPlayerId : null;

        var game = new ServerGame
        {
            GameId = session.Id,

            // Player information
            WhitePlayerId = session.WhitePlayerId,
            RedPlayerId = session.RedPlayerId,
            WhiteUserId = whiteUserId,
            RedUserId = redUserId,
            WhitePlayerName = session.WhitePlayerName,
            RedPlayerName = session.RedPlayerName,

            // Game state
            Status = engine.Winner != null ? "Completed" : "InProgress",
            GameStarted = engine.GameStarted,

            // Board state (serialize all 24 points)
            BoardState = SerializeBoardState(engine.Board),

            // Player states
            WhiteCheckersOnBar = engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = engine.WhitePlayer.CheckersBornOff,
            RedBornOff = engine.RedPlayer.CheckersBornOff,

            // Current turn state
            CurrentPlayer = engine.CurrentPlayer?.Color.ToString() ?? "White",

            // Dice state
            Die1 = engine.Dice.Die1,
            Die2 = engine.Dice.Die2,
            RemainingMoves = new List<int>(engine.RemainingMoves),

            // Doubling cube
            DoublingCubeValue = engine.DoublingCube.Value,
            DoublingCubeOwner = engine.DoublingCube.Owner?.ToString(),

            // Move history (convert to string notation)
            Moves = engine.MoveHistory
                .Select(m => m.IsBearOff ? $"{m.From}/off" :
                             m.From == 0 ? $"bar/{m.To}" :
                             $"{m.From}/{m.To}")
                .ToList(),

            // Move count
            MoveCount = engine.MoveHistory.Count,

            // Timestamps
            CreatedAt = session.CreatedAt,
            LastUpdatedAt = DateTime.UtcNow
        };

        // Check if this is an AI game
        game.IsAiOpponent = IsAiPlayer(session.WhitePlayerId) || IsAiPlayer(session.RedPlayerId);

        // If game is completed, add completion data
        if (engine.Winner != null)
        {
            game.Winner = engine.Winner.Color.ToString();
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

        // Restore timestamps using reflection
        var createdAtField = typeof(GameSession)
            .GetField("<CreatedAt>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (createdAtField != null)
        {
            createdAtField.SetValue(session, game.CreatedAt);
        }

        // Restore player assignments (connections will be empty until players reconnect)
        if (!string.IsNullOrEmpty(game.WhitePlayerId))
        {
            session.AddPlayer(game.WhitePlayerId, ""); // Empty connection ID
            session.WhitePlayerName = game.WhitePlayerName;
        }

        if (!string.IsNullOrEmpty(game.RedPlayerId))
        {
            session.AddPlayer(game.RedPlayerId, ""); // Empty connection ID
            session.RedPlayerName = game.RedPlayerName;
        }

        var engine = session.Engine;

        // Restore board state
        RestoreBoardState(engine.Board, game.BoardState);

        // Restore player states
        engine.WhitePlayer.CheckersOnBar = game.WhiteCheckersOnBar;
        engine.RedPlayer.CheckersOnBar = game.RedCheckersOnBar;
        engine.WhitePlayer.CheckersBornOff = game.WhiteBornOff;
        engine.RedPlayer.CheckersBornOff = game.RedBornOff;

        // Restore dice state
        engine.Dice.SetDice(game.Die1, game.Die2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(game.RemainingMoves);

        // Restore doubling cube
        if (game.DoublingCubeValue > 1)
        {
            // Set cube value using reflection (DoublingCube doesn't expose public setters)
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

        // Restore current player using reflection (CurrentPlayer is read-only)
        var currentPlayer = game.CurrentPlayer == "White"
            ? engine.WhitePlayer
            : engine.RedPlayer;

        var currentPlayerField = engine.GetType()
            .GetField("<CurrentPlayer>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (currentPlayerField != null)
        {
            currentPlayerField.SetValue(engine, currentPlayer);
        }

        // Restore GameStarted flag using reflection
        if (game.GameStarted)
        {
            var gameStartedField = engine.GetType()
                .GetField("<GameStarted>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (gameStartedField != null)
            {
                gameStartedField.SetValue(engine, true);
            }
        }

        // Validate restored state
        ValidateRestoredState(engine);

        return session;
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
            return false;

        return Guid.TryParse(playerId, out _);
    }

    /// <summary>
    /// Check if a player ID represents an AI opponent
    /// </summary>
    public static bool IsAiPlayer(string? playerId)
    {
        return playerId?.StartsWith("ai_") == true;
    }
}

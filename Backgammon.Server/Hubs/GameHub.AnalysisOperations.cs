using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Analysis Session Operations
/// Handles dedicated analysis sessions (separate from game sessions)
/// </summary>
public partial class GameHub
{
    /// <summary>
    /// Create a new analysis session.
    /// Returns the session ID for sharing/reconnecting.
    /// </summary>
    public async Task<string> CreateAnalysisSession()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var userId = GetAuthenticatedUserId() ?? connectionId;

            var session = _analysisSessionManager.CreateSession(userId, connectionId);

            // Add to SignalR group for broadcasting
            await Groups.AddToGroupAsync(connectionId, $"analysis-{session.Id}");

            _logger.LogInformation(
                "Created analysis session {SessionId} for user {UserId}",
                session.Id,
                userId);

            // Send initial state to client
            var state = session.GetState(connectionId);
            await Clients.Caller.GameStart(state);

            return session.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis session");
            throw new HubException("Failed to create analysis session");
        }
    }

    /// <summary>
    /// Join an existing analysis session (for multi-tab support).
    /// </summary>
    public async Task JoinAnalysisSession(string sessionId)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var userId = GetAuthenticatedUserId() ?? connectionId;

            var session = _analysisSessionManager.JoinSession(sessionId, userId, connectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Analysis session not found or you don't have access");
                return;
            }

            // Add to SignalR group
            await Groups.AddToGroupAsync(connectionId, $"analysis-{session.Id}");

            _logger.LogInformation(
                "Connection {ConnectionId} joined analysis session {SessionId}",
                connectionId,
                sessionId);

            // Send current state to the new connection
            var state = session.GetState(connectionId);
            await Clients.Caller.GameUpdate(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining analysis session {SessionId}", sessionId);
            await Clients.Caller.Error("Failed to join analysis session");
        }
    }

    /// <summary>
    /// Leave the current analysis session.
    /// </summary>
    public async Task LeaveAnalysisSession()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var session = _analysisSessionManager.GetSessionByConnection(connectionId);

            if (session == null)
            {
                // Not in an analysis session, nothing to do
                return;
            }

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(connectionId, $"analysis-{session.Id}");

            // Remove connection from session
            _analysisSessionManager.RemoveConnection(connectionId);

            _logger.LogInformation(
                "Connection {ConnectionId} left analysis session {SessionId}",
                connectionId,
                session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving analysis session");
        }
    }

    /// <summary>
    /// Broadcast game state update to all connections in an analysis session.
    /// </summary>
    private async Task BroadcastAnalysisSessionUpdate(AnalysisSession session)
    {
        foreach (var connectionId in session.Connections)
        {
            var state = session.GetState(connectionId);
            await Clients.Client(connectionId).GameUpdate(state);
        }
    }

    // ==================== Game Action Helpers for Analysis Sessions ====================

    /// <summary>
    /// Roll dice in an analysis session.
    /// </summary>
    private async Task RollDiceForAnalysisSession(AnalysisSession session)
    {
        await session.GameActionLock.WaitAsync();
        try
        {
            // Don't allow rolling if moves are remaining
            if (session.Engine.RemainingMoves.Count > 0)
            {
                await Clients.Caller.Error("Complete or undo your moves before rolling again");
                return;
            }

            session.Engine.RollDice();
            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await BroadcastAnalysisSessionUpdate(session);
    }

    /// <summary>
    /// Make a move in an analysis session.
    /// </summary>
    private async Task MakeMoveForAnalysisSession(AnalysisSession session, int from, int to)
    {
        await session.GameActionLock.WaitAsync();
        try
        {
            // Get valid moves
            var validMoves = session.Engine.GetValidMoves();
            var move = validMoves.FirstOrDefault(m => m.From == from && m.To == to);

            if (move == null)
            {
                await Clients.Caller.Error("Invalid move");
                return;
            }

            session.Engine.ExecuteMove(move);
            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await BroadcastAnalysisSessionUpdate(session);
    }

    /// <summary>
    /// Make a combined move in an analysis session.
    /// </summary>
    private async Task MakeCombinedMoveForAnalysisSession(
        AnalysisSession session,
        int from,
        int to,
        int[] intermediatePoints)
    {
        await session.GameActionLock.WaitAsync();
        try
        {
            // Build the sequence of moves
            var sequence = new List<(int From, int To)>();
            var currentFrom = from;

            foreach (var intermediate in intermediatePoints)
            {
                sequence.Add((currentFrom, intermediate));
                currentFrom = intermediate;
            }

            sequence.Add((currentFrom, to));

            // Validate and execute each move in sequence
            foreach (var (moveFrom, moveTo) in sequence)
            {
                var validMoves = session.Engine.GetValidMoves();
                var move = validMoves.FirstOrDefault(m => m.From == moveFrom && m.To == moveTo);

                if (move == null)
                {
                    await Clients.Caller.Error($"Invalid move from {moveFrom} to {moveTo}");
                    return;
                }

                session.Engine.ExecuteMove(move);
            }

            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await BroadcastAnalysisSessionUpdate(session);
    }

    /// <summary>
    /// End turn in an analysis session.
    /// </summary>
    private async Task EndTurnForAnalysisSession(AnalysisSession session)
    {
        await session.GameActionLock.WaitAsync();
        try
        {
            session.Engine.EndTurn();
            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await BroadcastAnalysisSessionUpdate(session);
    }

    /// <summary>
    /// Undo last move in an analysis session.
    /// </summary>
    private async Task UndoLastMoveForAnalysisSession(AnalysisSession session)
    {
        await session.GameActionLock.WaitAsync();
        try
        {
            if (!session.Engine.UndoLastMove())
            {
                await Clients.Caller.Error("Nothing to undo");
                return;
            }

            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await BroadcastAnalysisSessionUpdate(session);
    }
}

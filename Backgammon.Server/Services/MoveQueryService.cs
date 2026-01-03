using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles queries for valid moves and destinations
/// </summary>
public class MoveQueryService : IMoveQueryService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly ILogger<MoveQueryService> _logger;

    public MoveQueryService(
        IGameSessionManager sessionManager,
        ILogger<MoveQueryService> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public List<int> GetValidSources(string connectionId)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session == null)
            {
                return new List<int>();
            }

            if (!session.IsPlayerTurn(connectionId))
            {
                return new List<int>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                return new List<int>();
            }

            var validMoves = session.Engine.GetValidMoves();
            var sources = validMoves.Select(m => m.From).Distinct().ToList();
            return sources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid sources");
            return new List<int>();
        }
    }

    public List<MoveDto> GetValidDestinations(string connectionId, int fromPoint)
    {
        try
        {
            _logger.LogInformation("GetValidDestinations called for point {FromPoint}", fromPoint);
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session == null)
            {
                _logger.LogWarning("No session found for player");
                return new List<MoveDto>();
            }

            if (!session.IsPlayerTurn(connectionId))
            {
                _logger.LogWarning("Not player's turn");
                return new List<MoveDto>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                _logger.LogWarning("No remaining moves");
                return new List<MoveDto>();
            }

            var allValidMoves = session.Engine.GetValidMoves();
            _logger.LogInformation(
                "Total valid moves: {Count}",
                allValidMoves.Count);
            foreach (var m in allValidMoves)
            {
                _logger.LogInformation(
                    "  Valid move: {From} -> {To} (die: {Die})",
                    m.From,
                    m.To,
                    m.DieValue);
            }

            var validMoves = allValidMoves
                .Where(m => m.From == fromPoint)
                .Select(m => new MoveDto
                {
                    From = m.From,
                    To = m.To,
                    DieValue = m.DieValue,
                    IsHit = WillHit(session.Engine, m)
                })
                .ToList();

            _logger.LogInformation(
                "Filtered moves from point {FromPoint}: {Count}",
                fromPoint,
                validMoves.Count);
            return validMoves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid destinations");
            return new List<MoveDto>();
        }
    }

    private bool WillHit(GameEngine engine, Move move)
    {
        // Bear-off moves (To = 0 or 25) cannot hit
        if (move.IsBearOff)
        {
            return false;
        }

        var targetPoint = engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
        {
            return false;
        }

        return targetPoint.Color != engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }
}

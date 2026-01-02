using System;
using System.Collections.Generic;

namespace Backgammon.Core;

public class Match
{
    public Match()
    {
        CreatedAt = DateTime.UtcNow;
        Status = MatchStatus.InProgress;
    }

    public Match(string matchId, string player1Id, string player2Id, int targetScore)
        : this()
    {
        MatchId = matchId;
        Player1Id = player1Id;
        Player2Id = player2Id;
        TargetScore = targetScore;
    }

    public string MatchId { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string Player1Id { get; set; } = string.Empty;

    public string Player2Id { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public bool IsCrawfordGame { get; set; }

    public bool HasCrawfordGameBeenPlayed { get; set; }

    public List<Game> Games { get; set; } = new();

    public Game? CurrentGame { get; set; }

    public MatchStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int TotalGamesPlayed => Games.Count;

    public bool IsMatchComplete()
    {
        return Player1Score >= TargetScore || Player2Score >= TargetScore;
    }

    public string? GetWinnerId()
    {
        if (Player1Score >= TargetScore)
        {
            return Player1Id;
        }

        if (Player2Score >= TargetScore)
        {
            return Player2Id;
        }

        return null;
    }

    public void AddGame(Game game)
    {
        Games.Add(game);
        CurrentGame = game;
    }

    public void UpdateScores(string winnerId, int pointsWon)
    {
        if (winnerId == Player1Id)
        {
            Player1Score += pointsWon;
        }
        else if (winnerId == Player2Id)
        {
            Player2Score += pointsWon;
        }

        CheckCrawfordRule();

        if (IsMatchComplete())
        {
            Status = MatchStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
    }

    private void CheckCrawfordRule()
    {
        if (!HasCrawfordGameBeenPlayed && !IsCrawfordGame)
        {
            var maxScore = Math.Max(Player1Score, Player2Score);
            if (maxScore == TargetScore - 1)
            {
                IsCrawfordGame = true;
            }
        }
        else if (IsCrawfordGame)
        {
            IsCrawfordGame = false;
            HasCrawfordGameBeenPlayed = true;
        }
    }
}

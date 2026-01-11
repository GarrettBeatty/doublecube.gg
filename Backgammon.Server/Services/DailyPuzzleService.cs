using Backgammon.Analysis;
using Backgammon.Analysis.Models;
using Backgammon.Core;
using Backgammon.Server.Configuration;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for daily puzzle operations including generation, retrieval, and answer validation.
/// </summary>
public class DailyPuzzleService : IDailyPuzzleService
{
    private readonly IPuzzleRepository _puzzleRepository;
    private readonly RandomPositionGenerator _positionGenerator;
    private readonly PositionEvaluatorFactory _evaluatorFactory;
    private readonly PuzzleSettings _settings;
    private readonly ILogger<DailyPuzzleService> _logger;

    public DailyPuzzleService(
        IPuzzleRepository puzzleRepository,
        RandomPositionGenerator positionGenerator,
        PositionEvaluatorFactory evaluatorFactory,
        IOptions<PuzzleSettings> settings,
        ILogger<DailyPuzzleService> logger)
    {
        _puzzleRepository = puzzleRepository;
        _positionGenerator = positionGenerator;
        _evaluatorFactory = evaluatorFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DailyPuzzleDto?> GetTodaysPuzzleAsync(string? userId)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return await GetPuzzleByDateAsync(today, userId);
    }

    /// <inheritdoc/>
    public async Task<DailyPuzzleDto?> GetPuzzleByDateAsync(string date, string? userId)
    {
        var puzzle = await _puzzleRepository.GetPuzzleByDateAsync(date);
        if (puzzle == null)
        {
            _logger.LogWarning("No puzzle found for date {Date}", date);
            return null;
        }

        // Check if user has already solved this puzzle
        PuzzleAttempt? attempt = null;
        if (!string.IsNullOrEmpty(userId))
        {
            attempt = await _puzzleRepository.GetAttemptAsync(userId, date);
        }

        return MapToDto(puzzle, attempt);
    }

    /// <inheritdoc/>
    public async Task<PuzzleResultDto> SubmitAnswerAsync(string userId, string puzzleDate, List<MoveDto> moves)
    {
        var puzzle = await _puzzleRepository.GetPuzzleByDateAsync(puzzleDate);
        if (puzzle == null)
        {
            throw new InvalidOperationException($"Puzzle not found for date {puzzleDate}");
        }

        // Get or create attempt record
        var attempt = await _puzzleRepository.GetAttemptAsync(userId, puzzleDate)
            ?? new PuzzleAttempt
            {
                UserId = userId,
                PuzzleId = puzzle.PuzzleId,
                PuzzleDate = puzzleDate,
                CreatedAt = DateTime.UtcNow
            };

        attempt.AttemptCount++;
        attempt.SubmittedMoves = moves;
        attempt.SubmittedNotation = FormatMovesNotation(moves);

        // Check if the submitted moves match the best moves or an alternative
        var (isCorrect, equityLoss) = EvaluateAnswer(puzzle, moves);
        attempt.IsCorrect = isCorrect;
        attempt.EquityLoss = equityLoss;

        // Track if this is the first time the user solved this puzzle
        var isFirstSolve = isCorrect && !attempt.SolvedAt.HasValue;

        if (isFirstSolve)
        {
            attempt.SolvedAt = DateTime.UtcNow;
        }

        // Update attempt and puzzle stats
        await _puzzleRepository.SaveAttemptAsync(attempt);
        await _puzzleRepository.IncrementAttemptCountAsync(puzzleDate);

        if (isFirstSolve)
        {
            // Increment solved count when user solves for the first time (regardless of attempt count)
            await _puzzleRepository.IncrementSolvedCountAsync(puzzleDate);
        }

        // Update streak
        var streakInfo = await UpdateStreakAsync(userId, puzzleDate, isCorrect);

        return new PuzzleResultDto
        {
            IsCorrect = isCorrect,
            EquityLoss = equityLoss,
            Feedback = GetFeedback(isCorrect, equityLoss, attempt.AttemptCount),
            BestMoves = isCorrect ? puzzle.BestMoves : null,
            BestMovesNotation = isCorrect ? puzzle.BestMovesNotation : null,
            CurrentStreak = streakInfo.CurrentStreak,
            StreakBroken = false, // Streak only breaks if you miss a day, not wrong answer
            AttemptCount = attempt.AttemptCount
        };
    }

    /// <inheritdoc/>
    public async Task<PuzzleResultDto> GiveUpPuzzleAsync(string userId, string puzzleDate)
    {
        var puzzle = await _puzzleRepository.GetPuzzleByDateAsync(puzzleDate);
        if (puzzle == null)
        {
            throw new InvalidOperationException($"Puzzle not found for date {puzzleDate}");
        }

        // Get or create attempt record
        var attempt = await _puzzleRepository.GetAttemptAsync(userId, puzzleDate)
            ?? new PuzzleAttempt
            {
                UserId = userId,
                PuzzleId = puzzle.PuzzleId,
                PuzzleDate = puzzleDate,
                CreatedAt = DateTime.UtcNow
            };

        // Mark as given up (counts as an incorrect attempt)
        attempt.AttemptCount++;
        attempt.IsCorrect = false;
        attempt.GaveUp = true;
        attempt.SubmittedMoves = new List<MoveDto>();
        attempt.SubmittedNotation = "(gave up)";

        await _puzzleRepository.SaveAttemptAsync(attempt);
        await _puzzleRepository.IncrementAttemptCountAsync(puzzleDate);

        // Reset streak since user gave up
        var streakInfo = await _puzzleRepository.GetStreakInfoAsync(userId)
            ?? new PuzzleStreakInfo { UserId = userId };
        streakInfo.CurrentStreak = 0;
        await _puzzleRepository.SaveStreakInfoAsync(streakInfo);

        return new PuzzleResultDto
        {
            IsCorrect = false,
            EquityLoss = 1.0, // Max equity loss for giving up
            Feedback = "Here's the best move for this position.",
            BestMoves = puzzle.BestMoves,
            BestMovesNotation = puzzle.BestMovesNotation,
            CurrentStreak = 0,
            StreakBroken = true,
            AttemptCount = attempt.AttemptCount
        };
    }

    /// <inheritdoc/>
    public async Task<PuzzleStreakInfo> GetStreakInfoAsync(string userId)
    {
        var streakInfo = await _puzzleRepository.GetStreakInfoAsync(userId);
        return streakInfo ?? new PuzzleStreakInfo { UserId = userId };
    }

    /// <inheritdoc/>
    public async Task<bool> PuzzleExistsAsync(string date)
    {
        return await _puzzleRepository.PuzzleExistsAsync(date);
    }

    /// <inheritdoc/>
    public async Task<DailyPuzzle> GeneratePuzzleForDateAsync(string date)
    {
        _logger.LogInformation(
            "Generating puzzle for date {Date} using {EvaluatorType} evaluator",
            date,
            _settings.EvaluatorType);

        // Generate a random position
        var engine = _positionGenerator.GenerateRandomPosition();

        // Get best moves using the configured evaluator
        var evaluator = _evaluatorFactory.GetEvaluator(_settings.EvaluatorType);
        var analysis = evaluator.FindBestMoves(engine);

        if (analysis.TopMoves.Count == 0)
        {
            throw new InvalidOperationException("Failed to find best moves for generated position");
        }

        var bestMove = analysis.TopMoves[0];

        // Create puzzle entity
        var puzzle = new DailyPuzzle
        {
            PuzzleId = Guid.NewGuid().ToString(),
            PuzzleDate = date,
            PositionSgf = SgfSerializer.ExportPosition(engine),
            CurrentPlayer = engine.CurrentPlayer.Color.ToString(),
            Dice = new[] { engine.Dice.Die1, engine.Dice.Die2 },
            BoardState = CreateBoardState(engine),
            WhiteCheckersOnBar = engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = engine.WhitePlayer.CheckersBornOff,
            RedBornOff = engine.RedPlayer.CheckersBornOff,
            BestMoves = bestMove.Moves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = m.DieValue,
                IsHit = m.IsHit
            }).ToList(),
            BestMovesNotation = bestMove.Notation,
            BestMoveEquity = bestMove.FinalEvaluation.Equity,
            AlternativeMoves = GetAlternativeMoves(analysis, _settings.EquityTolerance),
            EvaluatorType = _settings.EvaluatorType,
            CreatedAt = DateTime.UtcNow
        };

        // Save to repository
        await _puzzleRepository.SavePuzzleAsync(puzzle);

        _logger.LogInformation(
            "Created puzzle {PuzzleId} for date {Date} with {AlternativeCount} alternative moves",
            puzzle.PuzzleId,
            date,
            puzzle.AlternativeMoves.Count);

        return puzzle;
    }

    private (bool IsCorrect, double EquityLoss) EvaluateAnswer(DailyPuzzle puzzle, List<MoveDto> submittedMoves)
    {
        // Normalize the submitted moves for comparison
        var submittedNotation = NormalizeMovesNotation(submittedMoves);
        var bestNotation = NormalizeMovesNotation(puzzle.BestMoves);

        // Check if matches best move exactly
        if (submittedNotation == bestNotation)
        {
            return (true, 0);
        }

        // Check if matches any alternative move
        foreach (var alt in puzzle.AlternativeMoves)
        {
            var altNotation = NormalizeMovesNotation(alt.Moves);
            if (submittedNotation == altNotation)
            {
                return (true, alt.EquityLoss);
            }
        }

        // Not correct - calculate equity loss
        // For now, return a generic "wrong" equity loss
        // In a more sophisticated implementation, we'd evaluate the submitted position
        return (false, _settings.EquityTolerance + 0.01);
    }

    private string NormalizeMovesNotation(List<MoveDto> moves)
    {
        // Sort moves to handle equivalent move orders
        var sorted = moves
            .OrderByDescending(m => m.From)
            .ThenByDescending(m => m.To)
            .Select(m => $"{m.From}/{m.To}")
            .ToList();

        return string.Join(" ", sorted);
    }

    private string FormatMovesNotation(List<MoveDto> moves)
    {
        return string.Join(" ", moves.Select(m =>
        {
            var from = m.From == 0 ? "bar" : m.From.ToString();
            var to = m.To == 25 || m.To == 0 ? "off" : m.To.ToString();
            return $"{from}/{to}";
        }));
    }

    private string GetFeedback(bool isCorrect, double equityLoss, int attemptCount)
    {
        if (!isCorrect)
        {
            return attemptCount < 3
                ? "Not quite. Try again!"
                : "Keep trying! Think about blots and primes.";
        }

        return Math.Abs(equityLoss) < 1e-6
            ? (attemptCount == 1
                ? "Perfect! You found the best move!"
                : "Correct! That's the best move.")
            : $"Good move! Within {equityLoss:F3} equity of optimal.";
    }

    private async Task<PuzzleStreakInfo> UpdateStreakAsync(string userId, string puzzleDate, bool isCorrect)
    {
        var streakInfo = await _puzzleRepository.GetStreakInfoAsync(userId)
            ?? new PuzzleStreakInfo { UserId = userId };

        // Only count attempts from the submission path (this method is only called after submission)
        if (isCorrect)
        {
            streakInfo.TotalAttempts++;
        }

        if (isCorrect)
        {
            // Only count a solve and update streaks the first time this puzzle is solved
            var isFirstSolveForThisPuzzle =
                string.IsNullOrEmpty(streakInfo.LastSolvedDate) ||
                !string.Equals(streakInfo.LastSolvedDate, puzzleDate, StringComparison.Ordinal);

            if (isFirstSolveForThisPuzzle)
            {
                streakInfo.TotalSolved++;

                // Check if this continues or starts a streak
                if (string.IsNullOrEmpty(streakInfo.LastSolvedDate))
                {
                    // First puzzle ever solved
                    streakInfo.CurrentStreak = 1;
                }
                else
                {
                    var lastSolved = DateTime.Parse(streakInfo.LastSolvedDate);
                    var currentPuzzle = DateTime.Parse(puzzleDate);
                    var daysDiff = (currentPuzzle - lastSolved).Days;

                    if (daysDiff == 1)
                    {
                        // Consecutive day - continue streak
                        streakInfo.CurrentStreak++;
                    }
                    else if (daysDiff > 1)
                    {
                        // Missed day(s) - reset streak
                        streakInfo.CurrentStreak = 1;
                    }

                    // daysDiff == 0 means same day, don't change streak
                }

                // Update best streak
                if (streakInfo.CurrentStreak > streakInfo.BestStreak)
                {
                    streakInfo.BestStreak = streakInfo.CurrentStreak;
                }

                streakInfo.LastSolvedDate = puzzleDate;
            }
        }

        await _puzzleRepository.SaveStreakInfoAsync(streakInfo);
        return streakInfo;
    }

    private List<AlternativeMove> GetAlternativeMoves(BestMovesAnalysis analysis, double tolerance)
    {
        var alternatives = new List<AlternativeMove>();

        if (analysis.TopMoves.Count <= 1)
        {
            return alternatives;
        }

        var bestEquity = analysis.TopMoves[0].FinalEvaluation.Equity;

        // Skip the first (best) move, include others within tolerance
        foreach (var move in analysis.TopMoves.Skip(1))
        {
            var equityLoss = Math.Abs(bestEquity - move.FinalEvaluation.Equity);

            if (equityLoss <= tolerance)
            {
                alternatives.Add(new AlternativeMove
                {
                    Moves = move.Moves.Select(m => new MoveDto
                    {
                        From = m.From,
                        To = m.To,
                        DieValue = m.DieValue,
                        IsHit = m.IsHit
                    }).ToList(),
                    Notation = move.Notation,
                    Equity = move.FinalEvaluation.Equity,
                    EquityLoss = equityLoss
                });
            }
        }

        return alternatives;
    }

    private List<PointStateDto> CreateBoardState(GameEngine engine)
    {
        var boardState = new List<PointStateDto>();

        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            boardState.Add(new PointStateDto
            {
                Position = i,
                Color = point.Color?.ToString(),
                Count = point.Count
            });
        }

        return boardState;
    }

    private DailyPuzzleDto MapToDto(DailyPuzzle puzzle, PuzzleAttempt? attempt)
    {
        var alreadySolved = attempt?.IsCorrect == true;

        return new DailyPuzzleDto
        {
            PuzzleId = puzzle.PuzzleId,
            PuzzleDate = puzzle.PuzzleDate,
            CurrentPlayer = puzzle.CurrentPlayer,
            Dice = puzzle.Dice,
            BoardState = puzzle.BoardState,
            WhiteCheckersOnBar = puzzle.WhiteCheckersOnBar,
            RedCheckersOnBar = puzzle.RedCheckersOnBar,
            WhiteBornOff = puzzle.WhiteBornOff,
            RedBornOff = puzzle.RedBornOff,
            AlreadySolved = alreadySolved,
            AttemptsToday = attempt?.AttemptCount ?? 0,
            // Only reveal best moves if already solved
            BestMoves = alreadySolved ? puzzle.BestMoves : null,
            BestMovesNotation = alreadySolved ? puzzle.BestMovesNotation : null
        };
    }
}

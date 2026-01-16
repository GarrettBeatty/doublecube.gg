using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Gnubg;
using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Analysis.Evaluators;

/// <summary>
/// Position evaluator using GNU Backgammon via HTTP service.
/// </summary>
public class HttpGnubgEvaluator : IPositionEvaluator, IPliesConfigurable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly GnubgSettings _settings;
    private readonly ILogger<HttpGnubgEvaluator>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpGnubgEvaluator"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making requests.</param>
    /// <param name="settings">The gnubg settings.</param>
    /// <param name="logger">Optional logger.</param>
    public HttpGnubgEvaluator(
        HttpClient httpClient,
        IOptions<GnubgSettings> settings,
        ILogger<HttpGnubgEvaluator>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    /// <inheritdoc/>
    public string EvaluatorId => "gnubg-http";

    /// <inheritdoc/>
    public string DisplayName => "GNU Backgammon (HTTP)";

    /// <inheritdoc/>
    public bool RequiresExternalResources => true;

    /// <summary>
    /// Optional override for evaluation plies. When set, this takes precedence over settings.
    /// Used to support different difficulty levels (e.g., 0-ply for easy, 3-ply for expert).
    /// </summary>
    public int? PliesOverride { get; set; }

    /// <inheritdoc/>
    public async Task<PositionEvaluation> EvaluateAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            // Use Position ID format to correctly handle bar checkers
            var positionId = GnubgPositionConverter.ToPositionId(engine);
            var player = GnubgPositionConverter.GetPlayerString(engine);

            _logger?.LogDebug(
                "Evaluating position via HTTP. PositionID: {PositionId}, Player: {Player}",
                positionId,
                player);

            var request = new NativeEvalRequest
            {
                Position = positionId,
                Dice = new[] { engine.Dice.Die1, engine.Dice.Die2 },
                Player = player,
                Plies = PliesOverride ?? _settings.EvaluationPlies
            };

            var response = await _httpClient.PostAsJsonAsync("/eval-native", request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EvaluateResponse>(JsonOptions, ct)
                ?? throw new Exception("Null response from gnubg service");

            _logger?.LogDebug("HTTP evaluation complete. Equity: {Equity}", result.Equity);

            return new PositionEvaluation
            {
                Equity = result.Equity,
                WinProbability = result.WinProb,
                GammonProbability = result.GammonProb,
                BackgammonProbability = result.BgProb,
                Features = new PositionFeatures()
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to evaluate position via HTTP");
            throw new Exception("HTTP gnubg evaluation failed. See inner exception for details.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<BestMovesAnalysis> FindBestMovesAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            // Use Position ID format to correctly handle bar checkers
            var positionId = GnubgPositionConverter.ToPositionId(engine);
            var player = GnubgPositionConverter.GetPlayerString(engine);

            _logger?.LogDebug(
                "Finding best moves via HTTP. PositionID: {PositionId}, Player: {Player}, Dice: [{Dice}]",
                positionId,
                player,
                GnubgPositionConverter.GetDiceString(engine));

            // Get initial evaluation
            var initialEvaluation = await EvaluateAsync(engine, ct);

            // Get move hints using native endpoint
            var request = new NativeHintRequest
            {
                Position = positionId,
                Dice = new[] { engine.Dice.Die1, engine.Dice.Die2 },
                Player = player,
                Plies = PliesOverride ?? _settings.EvaluationPlies
            };

            var response = await _httpClient.PostAsJsonAsync("/hint-native", request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HintResponse>(JsonOptions, ct)
                ?? throw new Exception("Null response from gnubg service");

            _logger?.LogDebug("HTTP hint returned {Count} moves", result.Moves?.Count ?? 0);

            // Convert to BestMovesAnalysis format
            var topMoves = new List<MoveSequenceEvaluation>();

            if (result.Moves != null)
            {
                // Get original dice for parsing abbreviated notation
                var availableDice = engine.Dice.GetMoves();
                _logger?.LogInformation(
                    "Parsing gnubg moves. Player: {Player}, Dice: [{Dice}], RemainingMoves: [{Remaining}]",
                    engine.CurrentPlayer.Color,
                    string.Join(", ", availableDice),
                    string.Join(", ", engine.RemainingMoves));

                foreach (var moveHint in result.Moves.Take(5))
                {
                    _logger?.LogInformation(
                        "Gnubg hint #{Rank}: notation='{Notation}', equity={Equity}",
                        moveHint.Rank,
                        moveHint.Notation,
                        moveHint.Equity);

                    // Parse move notation into Move objects with alternatives
                    try
                    {
                        var alternatives = GnubgOutputParser.ParseMoveNotationWithAlternatives(
                            moveHint.Notation,
                            engine.CurrentPlayer.Color,
                            availableDice);

                        var moves = alternatives.FirstOrDefault() ?? new List<Move>();

                        _logger?.LogInformation(
                            "Parsed {Count} moves from notation '{Notation}': [{Moves}], alternatives: {AltCount}",
                            moves.Count,
                            moveHint.Notation,
                            string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})")),
                            alternatives.Count);

                        var moveEvaluation = new PositionEvaluation
                        {
                            Equity = moveHint.Equity,
                            Features = new PositionFeatures()
                        };

                        var sequence = new MoveSequenceEvaluation
                        {
                            Moves = moves,
                            Alternatives = alternatives,
                            FinalEvaluation = moveEvaluation,
                            EquityGain = moveHint.Equity - initialEvaluation.Equity
                        };

                        topMoves.Add(sequence);
                    }
                    catch (Exception parseEx)
                    {
                        _logger?.LogWarning(
                            parseEx,
                            "Failed to parse gnubg notation '{Notation}' for {Player}",
                            moveHint.Notation,
                            engine.CurrentPlayer.Color);
                    }
                }
            }

            _logger?.LogDebug("HTTP found {Count} best moves", topMoves.Count);

            return new BestMovesAnalysis
            {
                InitialEvaluation = initialEvaluation,
                TopMoves = topMoves,
                TotalSequencesExplored = result.Moves?.Count ?? 0
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to find best moves via HTTP");
            throw new Exception("HTTP gnubg move analysis failed. See inner exception for details.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine, MatchContext matchContext, CancellationToken ct = default)
    {
        try
        {
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug(
                "Analyzing cube decision via HTTP. SGF: {Sgf}, Match: {Target}-point, Score: {P1}-{P2}, Crawford: {Crawford}",
                sgf,
                matchContext.TargetScore,
                matchContext.Player1Score,
                matchContext.Player2Score,
                matchContext.IsCrawfordGame);

            var request = new CubeRequest
            {
                Sgf = sgf,
                Plies = PliesOverride ?? _settings.EvaluationPlies,
                MatchLength = matchContext.TargetScore,
                Player1Score = matchContext.Player1Score,
                Player2Score = matchContext.Player2Score,
                IsCrawford = matchContext.IsCrawfordGame
            };

            var response = await _httpClient.PostAsJsonAsync("/cube", request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CubeResponse>(JsonOptions, ct)
                ?? throw new Exception("Null response from gnubg service");

            _logger?.LogDebug("HTTP cube decision: {Recommendation}", result.Recommendation);

            return new CubeDecision
            {
                NoDoubleEquity = result.NoDoubleEq,
                DoubleTakeEquity = result.DoubleTakeEq,
                DoublePassEquity = result.DoublePassEq,
                Recommendation = result.Recommendation ?? "NoDouble"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to analyze cube decision via HTTP");
            throw new Exception("HTTP gnubg cube analysis failed. See inner exception for details.", ex);
        }
    }

    private sealed class EvaluateRequest
    {
        public string Sgf { get; set; } = string.Empty;

        public int Plies { get; set; } = 2;
    }

    private sealed class EvaluateResponse
    {
        public double Equity { get; set; }

        public double WinProb { get; set; }

        public double GammonProb { get; set; }

        public double BgProb { get; set; }
    }

    private sealed class HintRequest
    {
        public string Sgf { get; set; } = string.Empty;

        public int Plies { get; set; } = 2;
    }

    private sealed class HintResponse
    {
        public List<MoveHint>? Moves { get; set; }
    }

    private sealed class MoveHint
    {
        public int Rank { get; set; }

        public string Notation { get; set; } = string.Empty;

        public double Equity { get; set; }
    }

    private sealed class CubeRequest
    {
        public string Sgf { get; set; } = string.Empty;

        public int Plies { get; set; } = 2;

        public int MatchLength { get; set; }

        public int Player1Score { get; set; }

        public int Player2Score { get; set; }

        public bool IsCrawford { get; set; }
    }

    private sealed class CubeResponse
    {
        public double NoDoubleEq { get; set; }

        public double DoubleTakeEq { get; set; }

        public double DoublePassEq { get; set; }

        public string? Recommendation { get; set; }
    }

    private sealed class NativeEvalRequest
    {
        public string Position { get; set; } = string.Empty;

        public int[] Dice { get; set; } = Array.Empty<int>();

        public string Player { get; set; } = "O";

        public int Plies { get; set; } = 2;
    }

    private sealed class NativeHintRequest
    {
        public string Position { get; set; } = string.Empty;

        public int[] Dice { get; set; } = Array.Empty<int>();

        public string Player { get; set; } = "O";

        public int Plies { get; set; } = 2;
    }
}

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
public class HttpGnubgEvaluator : IPositionEvaluator
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

    /// <inheritdoc/>
    public async Task<PositionEvaluation> EvaluateAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug("Evaluating position via HTTP. SGF: {Sgf}", sgf);

            var request = new EvaluateRequest
            {
                Sgf = sgf,
                Plies = _settings.EvaluationPlies
            };

            var response = await _httpClient.PostAsJsonAsync("/evaluate", request, JsonOptions, ct);
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
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug("Finding best moves via HTTP. SGF: {Sgf}", sgf);

            // Get initial evaluation
            var initialEvaluation = await EvaluateAsync(engine, ct);

            // Get move hints
            var request = new HintRequest
            {
                Sgf = sgf,
                Plies = _settings.EvaluationPlies
            };

            var response = await _httpClient.PostAsJsonAsync("/hint", request, JsonOptions, ct);
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

                foreach (var moveHint in result.Moves.Take(5))
                {
                    // Parse move notation into Move objects
                    var moves = GnubgOutputParser.ParseMoveNotation(
                        moveHint.Notation,
                        engine.CurrentPlayer.Color,
                        availableDice);

                    var moveEvaluation = new PositionEvaluation
                    {
                        Equity = moveHint.Equity,
                        Features = new PositionFeatures()
                    };

                    var sequence = new MoveSequenceEvaluation
                    {
                        Moves = moves,
                        FinalEvaluation = moveEvaluation,
                        EquityGain = moveHint.Equity - initialEvaluation.Equity
                    };

                    topMoves.Add(sequence);
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
    public async Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug("Analyzing cube decision via HTTP. SGF: {Sgf}", sgf);

            var request = new CubeRequest
            {
                Sgf = sgf,
                Plies = _settings.EvaluationPlies
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
    }

    private sealed class CubeResponse
    {
        public double NoDoubleEq { get; set; }

        public double DoubleTakeEq { get; set; }

        public double DoublePassEq { get; set; }

        public string? Recommendation { get; set; }
    }
}

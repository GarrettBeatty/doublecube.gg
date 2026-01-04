using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Implementation of ELO rating calculation service
/// </summary>
public class EloRatingService : IEloRatingService
{
    private readonly int _startingRating;
    private readonly int _kFactorNew;
    private readonly int _kFactorEstablished;
    private readonly int _gamesForEstablished;
    private readonly ILogger<EloRatingService> _logger;

    public EloRatingService(IConfiguration configuration, ILogger<EloRatingService> logger)
    {
        _logger = logger;

        // Load configuration with defaults
        _startingRating = configuration.GetValue<int>("EloRating:StartingRating", 1500);
        _kFactorNew = configuration.GetValue<int>("EloRating:KFactorNew", 32);
        _kFactorEstablished = configuration.GetValue<int>("EloRating:KFactorEstablished", 24);
        _gamesForEstablished = configuration.GetValue<int>("EloRating:GamesForEstablished", 30);

        _logger.LogInformation(
            "EloRatingService initialized: StartingRating={StartingRating}, KFactorNew={KFactorNew}, " +
            "KFactorEstablished={KFactorEstablished}, GamesForEstablished={GamesForEstablished}",
            _startingRating,
            _kFactorNew,
            _kFactorEstablished,
            _gamesForEstablished);
    }

    public (int WhiteNewRating, int RedNewRating) CalculateNewRatings(
        int whiteRating,
        int redRating,
        int whiteRatedGames,
        int redRatedGames,
        bool whiteWon)
    {
        // Calculate expected scores
        var whiteExpected = CalculateExpectedScore(whiteRating, redRating);
        var redExpected = CalculateExpectedScore(redRating, whiteRating);

        // Actual scores (1 for win, 0 for loss)
        var whiteActual = whiteWon ? 1.0 : 0.0;
        var redActual = whiteWon ? 0.0 : 1.0;

        // Get K-factors based on games played
        var whiteK = GetKFactor(whiteRatedGames);
        var redK = GetKFactor(redRatedGames);

        // Calculate rating changes
        var whiteChange = (int)Math.Round(whiteK * (whiteActual - whiteExpected));
        var redChange = (int)Math.Round(redK * (redActual - redExpected));

        var whiteNewRating = whiteRating + whiteChange;
        var redNewRating = redRating + redChange;

        _logger.LogDebug(
            "ELO calculation: White {WhiteOld}→{WhiteNew} ({WhiteChange:+#;-#;0}), " +
            "Red {RedOld}→{RedNew} ({RedChange:+#;-#;0}), " +
            "Expected: W={WhiteExp:F2} R={RedExp:F2}, K-factors: W={WhiteK} R={RedK}",
            whiteRating,
            whiteNewRating,
            whiteChange,
            redRating,
            redNewRating,
            redChange,
            whiteExpected,
            redExpected,
            whiteK,
            redK);

        return (whiteNewRating, redNewRating);
    }

    public double CalculateExpectedScore(int playerRating, int opponentRating)
    {
        // ELO expected score formula: 1 / (1 + 10^((OpponentRating - PlayerRating) / 400))
        return 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - playerRating) / 400.0));
    }

    public int GetKFactor(int gamesPlayed)
    {
        // Higher K-factor for new players (more volatile ratings)
        // Lower K-factor for established players (more stable ratings)
        return gamesPlayed < _gamesForEstablished ? _kFactorNew : _kFactorEstablished;
    }
}

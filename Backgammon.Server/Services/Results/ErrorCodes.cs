namespace Backgammon.Server.Services.Results;

/// <summary>
/// Common error codes used across the application
/// </summary>
public static class ErrorCodes
{
    // Game errors
    public const string GameNotFound = "GAME_NOT_FOUND";
    public const string GameFull = "GAME_FULL";
    public const string GameNotStarted = "GAME_NOT_STARTED";
    public const string GameAlreadyOver = "GAME_ALREADY_OVER";

    // Turn errors
    public const string NotYourTurn = "NOT_YOUR_TURN";
    public const string MustRollFirst = "MUST_ROLL_FIRST";
    public const string AlreadyRolled = "ALREADY_ROLLED";
    public const string NoMovesRemaining = "NO_MOVES_REMAINING";

    // Move errors
    public const string InvalidMove = "INVALID_MOVE";
    public const string MoveNotAllowed = "MOVE_NOT_ALLOWED";

    // Match errors
    public const string MatchNotFound = "MATCH_NOT_FOUND";
    public const string MatchFull = "MATCH_FULL";
    public const string MatchAlreadyStarted = "MATCH_ALREADY_STARTED";
    public const string MatchAlreadyComplete = "MATCH_ALREADY_COMPLETE";
    public const string NotMatchParticipant = "NOT_MATCH_PARTICIPANT";

    // Doubling errors
    public const string CannotDouble = "CANNOT_DOUBLE";
    public const string NoDoubleOffered = "NO_DOUBLE_OFFERED";

    // Player errors
    public const string PlayerNotFound = "PLAYER_NOT_FOUND";
    public const string NotAuthorized = "NOT_AUTHORIZED";

    // Session errors
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string InvalidSessionState = "INVALID_SESSION_STATE";
}

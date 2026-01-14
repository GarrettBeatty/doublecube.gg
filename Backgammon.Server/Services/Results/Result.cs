namespace Backgammon.Server.Services.Results;

/// <summary>
/// Represents the result of an operation with no return value.
/// </summary>
public record Result
{
    private Result()
    {
    }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static Result Ok() => new() { Success = true };

    /// <summary>
    /// Create a failed result with error details
    /// </summary>
    public static Result Failure(string errorCode, string message) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message
    };

    /// <summary>
    /// Deconstruct for pattern matching
    /// </summary>
    public void Deconstruct(out bool success, out string? errorCode, out string? message)
    {
        success = Success;
        errorCode = ErrorCode;
        message = Message;
    }
}

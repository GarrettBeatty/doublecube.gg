namespace Backgammon.Server.Services.Results;

/// <summary>
/// Represents the result of an operation that may succeed or fail.
/// Provides a functional alternative to throwing exceptions for expected failure cases.
/// </summary>
/// <typeparam name="T">The type of the value on success</typeparam>
public record Result<T>
{
    private Result()
    {
    }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The value on success (default if failed)
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Error code for programmatic handling (e.g., "GAME_NOT_FOUND", "NOT_YOUR_TURN")
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Implicitly convert a value to a successful result
    /// </summary>
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>
    /// Create a successful result with a value
    /// </summary>
    public static Result<T> Ok(T value) => new()
    {
        Success = true,
        Value = value
    };

    /// <summary>
    /// Create a failed result with error details
    /// </summary>
    public static Result<T> Failure(string errorCode, string message) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        Message = message
    };

    /// <summary>
    /// Deconstruct for pattern matching
    /// </summary>
    public void Deconstruct(out bool success, out T? value, out string? errorCode, out string? message)
    {
        success = Success;
        value = Value;
        errorCode = ErrorCode;
        message = Message;
    }
}

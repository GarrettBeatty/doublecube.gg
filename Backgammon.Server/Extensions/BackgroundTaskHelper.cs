using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Extensions;

/// <summary>
/// Helper methods for background task execution
/// </summary>
public static class BackgroundTaskHelper
{
    /// <summary>
    /// Execute a task in the background without awaiting, with automatic error logging.
    /// This is the "fire-and-forget" pattern used for non-critical background operations.
    /// </summary>
    /// <param name="task">The task to execute</param>
    /// <param name="logger">Logger for error reporting</param>
    /// <param name="operationName">Descriptive name for logging purposes</param>
    public static void FireAndForget(
        this Task task,
        ILogger logger,
        string operationName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fire-and-forget task failed: {Operation}", operationName);
            }
        });
    }

    /// <summary>
    /// Execute an async function in the background without awaiting, with automatic error logging.
    /// This is the "fire-and-forget" pattern used for non-critical background operations.
    /// </summary>
    /// <param name="taskFactory">Function that returns the task to execute</param>
    /// <param name="logger">Logger for error reporting</param>
    /// <param name="operationName">Descriptive name for logging purposes</param>
    public static void FireAndForget(
        Func<Task> taskFactory,
        ILogger logger,
        string operationName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await taskFactory();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fire-and-forget task failed: {Operation}", operationName);
            }
        });
    }
}

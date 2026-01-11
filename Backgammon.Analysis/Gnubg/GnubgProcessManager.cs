using System.Diagnostics;
using System.Text;
using Backgammon.Analysis.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Manages GNU Backgammon (gnubg) process lifecycle and communication
/// </summary>
public class GnubgProcessManager : IDisposable
{
    private readonly GnubgSettings _settings;
    private readonly ILogger<GnubgProcessManager>? _logger;
    private readonly SemaphoreSlim _processLock;
    private bool _disposed;

    public GnubgProcessManager(IOptions<GnubgSettings> settings, ILogger<GnubgProcessManager>? logger = null)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
        _processLock = new SemaphoreSlim(1, 1); // Only one process at a time
    }

    /// <summary>
    /// Execute a gnubg command and return the output
    /// </summary>
    /// <param name="commands">List of gnubg commands to execute</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Output from gnubg</returns>
    public async Task<string> ExecuteCommandAsync(List<string> commands, CancellationToken ct = default)
    {
        await _processLock.WaitAsync(ct);
        try
        {
            var fullCommand = string.Join(Environment.NewLine, commands) + Environment.NewLine + "quit" + Environment.NewLine;

            if (_settings.VerboseLogging)
            {
                _logger?.LogDebug("Executing gnubg commands:\n{Commands}", fullCommand);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_settings.TimeoutMs);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _settings.ExecutablePath,
                Arguments = "-t", // Text mode (no GUI)
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processStartInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write commands to stdin
            await process.StandardInput.WriteAsync(fullCommand);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            // Wait for process to complete
            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Gnubg process timed out after {TimeoutMs}ms, killing process", _settings.TimeoutMs);
                try
                {
                    process.Kill(true);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to kill gnubg process");
                }

                throw new TimeoutException($"Gnubg process timed out after {_settings.TimeoutMs}ms");
            }

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            if (_settings.VerboseLogging)
            {
                _logger?.LogDebug("Gnubg output:\n{Output}", output);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _logger?.LogDebug("Gnubg error output:\n{Error}", error);
                }
            }

            if (process.ExitCode != 0)
            {
                _logger?.LogWarning("Gnubg exited with code {ExitCode}. Error: {Error}", process.ExitCode, error);
            }

            if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrWhiteSpace(error))
            {
                throw new Exception($"Gnubg execution failed: {error}");
            }

            return output;
        }
        finally
        {
            _processLock.Release();
        }
    }

    /// <summary>
    /// Check if gnubg is available and working
    /// </summary>
    /// <returns>True if gnubg is available and functional</returns>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var commands = new List<string> { "show version" };
            var output = await ExecuteCommandAsync(commands, CancellationToken.None);

            // Check if output contains "GNU Backgammon"
            return output.Contains("GNU Backgammon", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Gnubg availability check failed");
            return false;
        }
    }

    /// <summary>
    /// Execute gnubg commands with an SGF position loaded from a temporary file
    /// </summary>
    /// <param name="sgfContent">SGF position content to load</param>
    /// <param name="commands">List of gnubg commands to execute after loading position</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Output from gnubg</returns>
    public async Task<string> ExecuteWithSgfFileAsync(string sgfContent, List<string> commands, CancellationToken ct = default)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"gnubg_{Guid.NewGuid()}.sgf");
        try
        {
            // Write SGF content to temporary file
            await File.WriteAllTextAsync(tempFile, sgfContent, ct);

            if (_settings.VerboseLogging)
            {
                _logger?.LogDebug("Wrote SGF to temp file: {TempFile}", tempFile);
            }

            // Prepend load position command
            var allCommands = new List<string> { $"load position {tempFile}" };
            allCommands.AddRange(commands);

            // Execute commands
            return await ExecuteCommandAsync(allCommands, ct);
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to delete temp file {TempFile}", tempFile);
                }
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _processLock.Dispose();
            _disposed = true;
        }
    }
}

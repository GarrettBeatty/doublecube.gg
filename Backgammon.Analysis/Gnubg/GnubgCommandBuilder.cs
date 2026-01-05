using Backgammon.Analysis.Configuration;

namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Builds gnubg command sequences for various operations
/// </summary>
public static class GnubgCommandBuilder
{
    /// <summary>
    /// Build gnubg commands for position evaluation
    /// </summary>
    /// <param name="sgf">SGF representation of the position</param>
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands</returns>
    public static List<string> BuildEvaluationCommand(string sgf, GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Import the position
        commands.Add($"import sgf {sgf}");

        // Configure evaluation settings
        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation cubeful off"); // Faster for position evaluation
        }

        commands.Add($"set evaluation plies {settings.EvaluationPlies}");

        // Evaluate the position
        commands.Add("eval");

        return commands;
    }

    /// <summary>
    /// Build gnubg commands for finding best moves (hint)
    /// </summary>
    /// <param name="sgf">SGF representation of the position</param>
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands</returns>
    public static List<string> BuildHintCommand(string sgf, GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Import the position
        commands.Add($"import sgf {sgf}");

        // Configure evaluation settings
        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation cubeful off");
        }

        commands.Add($"set evaluation plies {settings.EvaluationPlies}");

        // Get move hints
        commands.Add("hint");

        return commands;
    }

    /// <summary>
    /// Build gnubg commands for cube decision analysis
    /// </summary>
    /// <param name="sgf">SGF representation of the position</param>
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands</returns>
    public static List<string> BuildCubeCommand(string sgf, GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Import the position
        commands.Add($"import sgf {sgf}");

        // Configure evaluation settings with cubeful analysis
        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation cubeful on"); // Important for cube decisions
        }

        commands.Add($"set evaluation plies {settings.EvaluationPlies}");

        // Evaluate cube decision
        commands.Add("hint cube");

        return commands;
    }
}

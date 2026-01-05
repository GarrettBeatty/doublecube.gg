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
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands (position should be loaded separately)</returns>
    public static List<string> BuildEvaluationCommand(GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Configure evaluation settings (position will be loaded by ProcessManager)
        commands.Add($"set evaluation chequerplay evaluation plies {settings.EvaluationPlies}");
        commands.Add($"set evaluation cubedecision evaluation plies {settings.EvaluationPlies}");

        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation chequerplay evaluation cubeful off"); // Faster for position evaluation
        }

        // Evaluate the position
        commands.Add("eval");

        return commands;
    }

    /// <summary>
    /// Build gnubg commands for finding best moves (hint)
    /// </summary>
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands (position should be loaded separately)</returns>
    public static List<string> BuildHintCommand(GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Configure evaluation settings (position will be loaded by ProcessManager)
        commands.Add($"set evaluation chequerplay evaluation plies {settings.EvaluationPlies}");
        commands.Add($"set evaluation cubedecision evaluation plies {settings.EvaluationPlies}");

        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation chequerplay evaluation cubeful off");
        }

        // Get move hints
        commands.Add("hint");

        return commands;
    }

    /// <summary>
    /// Build gnubg commands for cube decision analysis
    /// </summary>
    /// <param name="settings">Gnubg settings</param>
    /// <returns>List of gnubg commands (position should be loaded separately)</returns>
    public static List<string> BuildCubeCommand(GnubgSettings settings)
    {
        var commands = new List<string>();

        // Disable automatic play
        commands.Add("set automatic game off");
        commands.Add("set automatic roll off");

        // Configure evaluation settings with cubeful analysis (position will be loaded by ProcessManager)
        commands.Add($"set evaluation chequerplay evaluation plies {settings.EvaluationPlies}");
        commands.Add($"set evaluation cubedecision evaluation plies {settings.EvaluationPlies}");

        if (settings.UseNeuralNet)
        {
            commands.Add("set evaluation chequerplay evaluation cubeful on"); // Important for cube decisions
            commands.Add("set evaluation cubedecision evaluation cubeful on");
        }

        // Evaluate cube decision
        commands.Add("hint cube");

        return commands;
    }
}

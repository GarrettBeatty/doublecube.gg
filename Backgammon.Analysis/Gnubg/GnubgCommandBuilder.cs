using Backgammon.Analysis.Configuration;
using Backgammon.Plugins.Models;

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

    /// <summary>
    /// Build gnubg commands for setting match context (score, target, Crawford).
    /// These commands should be issued before cube analysis for accurate match equity calculations.
    /// </summary>
    /// <param name="context">Match context with score and Crawford information</param>
    /// <returns>List of gnubg commands to set match context</returns>
    public static List<string> BuildMatchContextCommands(MatchContext context)
    {
        var commands = new List<string>();

        if (!context.IsMatchGame)
        {
            // Money game - no match context needed
            commands.Add("set match 0");
            return commands;
        }

        // Set match length (target score)
        commands.Add($"set match {context.TargetScore}");

        // Set current scores (gnubg uses "set score <player-on-roll> <opponent>")
        commands.Add($"set score {context.Player1Score} {context.Player2Score}");

        // Set Crawford state
        commands.Add(context.IsCrawfordGame ? "set crawford on" : "set crawford off");

        return commands;
    }
}

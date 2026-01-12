using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// All customizable colors for a board theme.
/// </summary>
public class ThemeColors
{
    // Board structure
    [JsonPropertyName("boardBackground")]
    public string BoardBackground { get; set; } = "hsl(0 0% 14%)";

    [JsonPropertyName("boardBorder")]
    public string BoardBorder { get; set; } = "hsl(0 0% 22%)";

    [JsonPropertyName("bar")]
    public string Bar { get; set; } = "hsl(0 0% 11%)";

    [JsonPropertyName("bearoff")]
    public string Bearoff { get; set; } = "hsl(0 0% 11%)";

    // Triangles/Points
    [JsonPropertyName("pointLight")]
    public string PointLight { get; set; } = "hsl(0 0% 32%)";

    [JsonPropertyName("pointDark")]
    public string PointDark { get; set; } = "hsl(0 0% 20%)";

    // Checkers
    [JsonPropertyName("checkerWhite")]
    public string CheckerWhite { get; set; } = "hsl(0 0% 98%)";

    [JsonPropertyName("checkerWhiteStroke")]
    public string CheckerWhiteStroke { get; set; } = "hsl(0 0% 72%)";

    [JsonPropertyName("checkerRed")]
    public string CheckerRed { get; set; } = "hsl(0 84.2% 60.2%)";

    [JsonPropertyName("checkerRedStroke")]
    public string CheckerRedStroke { get; set; } = "hsl(0 72.2% 50.6%)";

    // Dice
    [JsonPropertyName("diceBackground")]
    public string DiceBackground { get; set; } = "white";

    [JsonPropertyName("diceDots")]
    public string DiceDots { get; set; } = "hsl(0 0% 9%)";

    // Doubling cube
    [JsonPropertyName("doublingCubeBackground")]
    public string DoublingCubeBackground { get; set; } = "#fbbf24";

    [JsonPropertyName("doublingCubeStroke")]
    public string DoublingCubeStroke { get; set; } = "#f59e0b";

    [JsonPropertyName("doublingCubeText")]
    public string DoublingCubeText { get; set; } = "#111827";

    // Highlights
    [JsonPropertyName("highlightSource")]
    public string HighlightSource { get; set; } = "hsla(47.9 95.8% 53.1% / 0.6)";

    [JsonPropertyName("highlightSelected")]
    public string HighlightSelected { get; set; } = "hsla(142.1 76.2% 36.3% / 0.7)";

    [JsonPropertyName("highlightDest")]
    public string HighlightDest { get; set; } = "hsla(221.2 83.2% 53.3% / 0.6)";

    [JsonPropertyName("highlightCapture")]
    public string HighlightCapture { get; set; } = "hsla(0 84.2% 60.2% / 0.6)";

    [JsonPropertyName("highlightAnalysis")]
    public string HighlightAnalysis { get; set; } = "hsla(142.1 76.2% 36.3% / 0.5)";

    // Text
    [JsonPropertyName("textLight")]
    public string TextLight { get; set; } = "hsla(0 0% 98% / 0.5)";

    [JsonPropertyName("textDark")]
    public string TextDark { get; set; } = "hsla(0 0% 9% / 0.7)";
}

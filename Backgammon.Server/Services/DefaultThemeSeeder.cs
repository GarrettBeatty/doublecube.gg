using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Provides default themes to seed the database.
/// </summary>
public static class DefaultThemeSeeder
{
    /// <summary>
    /// System author ID for default themes.
    /// </summary>
    private const string SystemAuthorId = "system";
    private const string SystemAuthorUsername = "Backgammon";

    /// <summary>
    /// Get all default themes to seed.
    /// </summary>
    public static List<BoardTheme> GetDefaultThemes()
    {
        return new List<BoardTheme>
        {
            CreateClassicTheme(),
            CreateWoodTheme(),
            CreateOceanTheme(),
            CreateForestTheme(),
            CreateHighContrastTheme(),
        };
    }

    /// <summary>
    /// Seed default themes to the database.
    /// </summary>
    public static async Task SeedDefaultThemesAsync(IThemeRepository themeRepository)
    {
        var existingDefaults = await themeRepository.GetDefaultThemesAsync();

        foreach (var theme in GetDefaultThemes())
        {
            // Only seed if theme doesn't exist
            if (!existingDefaults.Any(t => t.ThemeId == theme.ThemeId))
            {
                try
                {
                    await themeRepository.CreateThemeAsync(theme);
                    Console.WriteLine($"Seeded default theme: {theme.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to seed theme {theme.Name}: {ex.Message}");
                }
            }
        }
    }

    private static BoardTheme CreateClassicTheme()
    {
        return new BoardTheme
        {
            ThemeId = "default-classic",
            Name = "Classic",
            Description = "The default modern dark theme",
            AuthorId = SystemAuthorId,
            AuthorUsername = SystemAuthorUsername,
            Visibility = ThemeVisibility.Public,
            IsDefault = true,
            Colors = new ThemeColors
            {
                BoardBackground = "hsl(0 0% 14%)",
                BoardBorder = "hsl(0 0% 22%)",
                Bar = "hsl(0 0% 11%)",
                Bearoff = "hsl(0 0% 11%)",
                PointLight = "hsl(0 0% 32%)",
                PointDark = "hsl(0 0% 20%)",
                CheckerWhite = "hsl(0 0% 98%)",
                CheckerWhiteStroke = "hsl(0 0% 72%)",
                CheckerRed = "hsl(0 84.2% 60.2%)",
                CheckerRedStroke = "hsl(0 72.2% 50.6%)",
                DiceBackground = "white",
                DiceDots = "hsl(0 0% 9%)",
                DoublingCubeBackground = "#fbbf24",
                DoublingCubeStroke = "#f59e0b",
                DoublingCubeText = "#111827",
                HighlightSource = "hsla(47.9 95.8% 53.1% / 0.6)",
                HighlightSelected = "hsla(142.1 76.2% 36.3% / 0.7)",
                HighlightDest = "hsla(221.2 83.2% 53.3% / 0.6)",
                HighlightCapture = "hsla(0 84.2% 60.2% / 0.6)",
                HighlightAnalysis = "hsla(142.1 76.2% 36.3% / 0.5)",
                TextLight = "hsla(0 0% 98% / 0.5)",
                TextDark = "hsla(0 0% 9% / 0.7)",
            }
        };
    }

    private static BoardTheme CreateWoodTheme()
    {
        return new BoardTheme
        {
            ThemeId = "default-wood",
            Name = "Wood",
            Description = "Warm wooden board with natural tones",
            AuthorId = SystemAuthorId,
            AuthorUsername = SystemAuthorUsername,
            Visibility = ThemeVisibility.Public,
            IsDefault = true,
            Colors = new ThemeColors
            {
                BoardBackground = "hsl(30 30% 25%)",
                BoardBorder = "hsl(30 25% 35%)",
                Bar = "hsl(30 35% 18%)",
                Bearoff = "hsl(30 35% 18%)",
                PointLight = "hsl(35 40% 55%)",
                PointDark = "hsl(25 35% 35%)",
                CheckerWhite = "hsl(40 30% 90%)",
                CheckerWhiteStroke = "hsl(35 25% 70%)",
                CheckerRed = "hsl(15 70% 40%)",
                CheckerRedStroke = "hsl(15 60% 30%)",
                DiceBackground = "hsl(40 30% 92%)",
                DiceDots = "hsl(30 40% 20%)",
                DoublingCubeBackground = "hsl(35 60% 50%)",
                DoublingCubeStroke = "hsl(30 50% 40%)",
                DoublingCubeText = "hsl(30 40% 15%)",
                HighlightSource = "hsla(45 90% 50% / 0.6)",
                HighlightSelected = "hsla(120 60% 40% / 0.7)",
                HighlightDest = "hsla(200 70% 50% / 0.6)",
                HighlightCapture = "hsla(0 70% 50% / 0.6)",
                HighlightAnalysis = "hsla(120 60% 40% / 0.5)",
                TextLight = "hsla(40 30% 95% / 0.6)",
                TextDark = "hsla(30 40% 15% / 0.7)",
            }
        };
    }

    private static BoardTheme CreateOceanTheme()
    {
        return new BoardTheme
        {
            ThemeId = "default-ocean",
            Name = "Ocean",
            Description = "Cool blue ocean-inspired theme",
            AuthorId = SystemAuthorId,
            AuthorUsername = SystemAuthorUsername,
            Visibility = ThemeVisibility.Public,
            IsDefault = true,
            Colors = new ThemeColors
            {
                BoardBackground = "hsl(210 40% 18%)",
                BoardBorder = "hsl(210 35% 28%)",
                Bar = "hsl(210 45% 12%)",
                Bearoff = "hsl(210 45% 12%)",
                PointLight = "hsl(200 50% 45%)",
                PointDark = "hsl(220 40% 30%)",
                CheckerWhite = "hsl(200 30% 95%)",
                CheckerWhiteStroke = "hsl(200 25% 75%)",
                CheckerRed = "hsl(15 80% 55%)",
                CheckerRedStroke = "hsl(15 70% 45%)",
                DiceBackground = "hsl(200 30% 95%)",
                DiceDots = "hsl(210 40% 15%)",
                DoublingCubeBackground = "hsl(45 90% 55%)",
                DoublingCubeStroke = "hsl(40 80% 45%)",
                DoublingCubeText = "hsl(210 40% 15%)",
                HighlightSource = "hsla(50 95% 55% / 0.6)",
                HighlightSelected = "hsla(160 70% 45% / 0.7)",
                HighlightDest = "hsla(190 80% 55% / 0.6)",
                HighlightCapture = "hsla(0 80% 55% / 0.6)",
                HighlightAnalysis = "hsla(160 70% 45% / 0.5)",
                TextLight = "hsla(200 30% 95% / 0.6)",
                TextDark = "hsla(210 40% 15% / 0.7)",
            }
        };
    }

    private static BoardTheme CreateForestTheme()
    {
        return new BoardTheme
        {
            ThemeId = "default-forest",
            Name = "Forest",
            Description = "Deep green forest theme",
            AuthorId = SystemAuthorId,
            AuthorUsername = SystemAuthorUsername,
            Visibility = ThemeVisibility.Public,
            IsDefault = true,
            Colors = new ThemeColors
            {
                BoardBackground = "hsl(150 30% 15%)",
                BoardBorder = "hsl(150 25% 25%)",
                Bar = "hsl(150 35% 10%)",
                Bearoff = "hsl(150 35% 10%)",
                PointLight = "hsl(140 35% 40%)",
                PointDark = "hsl(160 30% 25%)",
                CheckerWhite = "hsl(80 25% 92%)",
                CheckerWhiteStroke = "hsl(80 20% 72%)",
                CheckerRed = "hsl(25 75% 45%)",
                CheckerRedStroke = "hsl(25 65% 35%)",
                DiceBackground = "hsl(80 25% 94%)",
                DiceDots = "hsl(150 35% 12%)",
                DoublingCubeBackground = "hsl(50 85% 50%)",
                DoublingCubeStroke = "hsl(45 75% 40%)",
                DoublingCubeText = "hsl(150 35% 12%)",
                HighlightSource = "hsla(55 90% 50% / 0.6)",
                HighlightSelected = "hsla(100 65% 45% / 0.7)",
                HighlightDest = "hsla(180 70% 50% / 0.6)",
                HighlightCapture = "hsla(0 75% 50% / 0.6)",
                HighlightAnalysis = "hsla(100 65% 45% / 0.5)",
                TextLight = "hsla(80 25% 95% / 0.6)",
                TextDark = "hsla(150 35% 12% / 0.7)",
            }
        };
    }

    private static BoardTheme CreateHighContrastTheme()
    {
        return new BoardTheme
        {
            ThemeId = "default-high-contrast",
            Name = "High Contrast",
            Description = "High contrast theme for better visibility",
            AuthorId = SystemAuthorId,
            AuthorUsername = SystemAuthorUsername,
            Visibility = ThemeVisibility.Public,
            IsDefault = true,
            Colors = new ThemeColors
            {
                BoardBackground = "hsl(0 0% 5%)",
                BoardBorder = "hsl(0 0% 40%)",
                Bar = "hsl(0 0% 8%)",
                Bearoff = "hsl(0 0% 8%)",
                PointLight = "hsl(0 0% 60%)",
                PointDark = "hsl(0 0% 25%)",
                CheckerWhite = "hsl(0 0% 100%)",
                CheckerWhiteStroke = "hsl(0 0% 70%)",
                CheckerRed = "hsl(0 100% 50%)",
                CheckerRedStroke = "hsl(0 100% 35%)",
                DiceBackground = "hsl(0 0% 100%)",
                DiceDots = "hsl(0 0% 0%)",
                DoublingCubeBackground = "hsl(60 100% 50%)",
                DoublingCubeStroke = "hsl(55 100% 40%)",
                DoublingCubeText = "hsl(0 0% 0%)",
                HighlightSource = "hsla(60 100% 50% / 0.8)",
                HighlightSelected = "hsla(120 100% 40% / 0.8)",
                HighlightDest = "hsla(200 100% 50% / 0.8)",
                HighlightCapture = "hsla(0 100% 50% / 0.8)",
                HighlightAnalysis = "hsla(120 100% 40% / 0.6)",
                TextLight = "hsla(0 0% 100% / 0.9)",
                TextDark = "hsla(0 0% 0% / 0.9)",
            }
        };
    }
}

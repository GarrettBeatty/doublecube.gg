using Backgammon.Server.Models;
using Xunit;

namespace Backgammon.Tests.Services;

/// <summary>
/// Tests to verify MatchConfig default values and validation
/// </summary>
public class MatchConfigValidationTests
{
    [Fact]
    public void MatchConfig_DefaultTimeControlType_IsChicagoPoint()
    {
        // Arrange & Act
        var config = new MatchConfig();

        // Assert
        Assert.Equal("ChicagoPoint", config.TimeControlType);
    }

    [Fact]
    public void MatchConfig_DefaultTargetScore_Is7()
    {
        // Arrange & Act
        var config = new MatchConfig();

        // Assert
        Assert.Equal(7, config.TargetScore);
    }

    [Fact]
    public void MatchConfig_DefaultIsRated_IsTrue()
    {
        // Arrange & Act
        var config = new MatchConfig();

        // Assert
        Assert.True(config.IsRated);
    }

    [Fact]
    public void MatchConfig_DefaultAiType_IsGreedy()
    {
        // Arrange & Act
        var config = new MatchConfig();

        // Assert
        Assert.Equal("greedy", config.AiType);
    }

    [Fact]
    public void MatchConfig_DefaultTimePerMoveDays_Is3()
    {
        // Arrange & Act
        var config = new MatchConfig();

        // Assert
        Assert.Equal(3, config.TimePerMoveDays);
    }

    [Fact]
    public void MatchConfig_ExplicitChicagoPoint_IsAccepted()
    {
        // Arrange & Act
        var config = new MatchConfig
        {
            TimeControlType = "ChicagoPoint"
        };

        // Assert
        Assert.Equal("ChicagoPoint", config.TimeControlType);
    }

    [Theory]
    [InlineData("OpenLobby")]
    [InlineData("AI")]
    [InlineData("Friend")]
    public void MatchConfig_DifferentOpponentTypes_DefaultToChicagoPoint(string opponentType)
    {
        // Arrange & Act
        var config = new MatchConfig
        {
            OpponentType = opponentType
        };

        // Assert
        Assert.Equal("ChicagoPoint", config.TimeControlType);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(11)]
    public void MatchConfig_DifferentTargetScores_DefaultToChicagoPoint(int targetScore)
    {
        // Arrange & Act
        var config = new MatchConfig
        {
            TargetScore = targetScore
        };

        // Assert
        Assert.Equal("ChicagoPoint", config.TimeControlType);
    }
}

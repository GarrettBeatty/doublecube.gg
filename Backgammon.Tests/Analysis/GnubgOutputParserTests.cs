using Backgammon.Analysis.Gnubg;
using Backgammon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Backgammon.Tests.Analysis;

public class GnubgOutputParserTests
{
    private readonly ITestOutputHelper _output;

    public GnubgOutputParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ===================
    // Bar Entry Tests
    // ===================

    [Fact]
    public void ParseMoveNotation_RedBarEntry_ParsesCorrectly()
    {
        // Arrange
        // gnubg returns "bar/24" for Red entering at our point 1 with die 1
        var notation = "bar/24 13/8";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 1, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: bar/24 = bar to our point 1 with die 1
        Assert.Equal(0, moves[0].From);  // bar
        Assert.Equal(1, moves[0].To);    // our point 1
        Assert.Equal(1, moves[0].DieValue);

        // Second move: 13/8 = our point 12 to 17 with die 5 (Red moves ascending)
        Assert.Equal(12, moves[1].From);
        Assert.Equal(17, moves[1].To);
        Assert.Equal(5, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_RedBarEntryWithFive_ParsesCorrectly()
    {
        // Arrange
        // gnubg returns "bar/20" for Red entering at our point 5 with die 5
        var notation = "bar/20";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Single(moves);
        Assert.Equal(0, moves[0].From);  // bar
        Assert.Equal(5, moves[0].To);    // our point 5
        Assert.Equal(5, moves[0].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_WhiteBarEntry_ParsesCorrectly()
    {
        // Arrange
        // gnubg returns "bar/20" for White entering at our point 20 with die 5
        var notation = "bar/20";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Single(moves);
        Assert.Equal(0, moves[0].From);  // bar
        Assert.Equal(20, moves[0].To);   // our point 20
        Assert.Equal(5, moves[0].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_RedBarEntryWithDoubles_ParsesAllFour()
    {
        // Arrange
        // GNUBG returns "bar/20(4)" for Red entering 4 times with double 5s
        // gnubg point 20 for Red = our point 25-20 = 5
        var notation = "bar/20(4)";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5, 5, 5, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(4, moves.Count);
        foreach (var move in moves)
        {
            Assert.Equal(0, move.From);   // bar
            Assert.Equal(5, move.To);     // our point 5
            Assert.Equal(5, move.DieValue);
        }
    }

    [Fact]
    public void ParseMoveNotation_WhiteBarEntryWithDoubles_ParsesAllFour()
    {
        // Arrange
        // GNUBG returns "bar/20(4)" for White entering 4 times with double 5s
        // For White, gnubg point 20 = our point 20
        var notation = "bar/20(4)";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 5, 5, 5, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(4, moves.Count);
        foreach (var move in moves)
        {
            Assert.Equal(0, move.From);   // bar
            Assert.Equal(20, move.To);    // our point 20
            Assert.Equal(5, move.DieValue);
        }
    }

    [Fact]
    public void ParseMoveNotation_RedBarEntryWithDoublesPartial_ParsesTwo()
    {
        // Arrange
        // GNUBG returns "bar/20(2)" for Red entering 2 times with double 5s
        // (e.g., only 2 checkers on bar)
        var notation = "bar/20(2)";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5, 5, 5, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);
        foreach (var move in moves)
        {
            Assert.Equal(0, move.From);   // bar
            Assert.Equal(5, move.To);     // our point 5
            Assert.Equal(5, move.DieValue);
        }
    }

    // ===================
    // Hit Notation Tests (asterisk *)
    // ===================

    [Fact]
    public void ParseMoveNotation_HitNotation_StripsAsterisk()
    {
        // Arrange
        // gnubg returns "8/5* 6/5" - hit on 5, then another move to 5
        // For Red with dice [3, 1]: 8/5* uses die 3 (gnubg 8->5 = our 17->20)
        var notation = "8/5* 6/5";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 3, 1 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: 8/5 = our 17->20 with die 3
        Assert.Equal(17, moves[0].From);
        Assert.Equal(20, moves[0].To);
        Assert.Equal(3, moves[0].DieValue);

        // Second move: 6/5 = our 19->20 with die 1
        Assert.Equal(19, moves[1].From);
        Assert.Equal(20, moves[1].To);
        Assert.Equal(1, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_HitNotationSingleMove_ParsesCorrectly()
    {
        // Arrange
        // gnubg returns "8/5*" - just hit on 5
        var notation = "8/5*";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 3 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Single(moves);
        Assert.Equal(17, moves[0].From);  // gnubg 8 = our 17
        Assert.Equal(20, moves[0].To);    // gnubg 5 = our 20
        Assert.Equal(3, moves[0].DieValue);
    }

    // ===================
    // Compound Move Tests (multiple slashes)
    // ===================

    [Fact]
    public void ParseMoveNotation_CompoundMove_ParsesTwoMoves()
    {
        // Arrange
        // gnubg returns "6/5/2" - move from 6 to 5 (die 1), then 5 to 2 (die 3)
        // For Red: gnubg 6->5->2 = our 19->20->23
        var notation = "6/5/2";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 3, 1 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: 6/5 = our 19->20 with die 1
        Assert.Equal(19, moves[0].From);
        Assert.Equal(20, moves[0].To);
        Assert.Equal(1, moves[0].DieValue);

        // Second move: 5/2 = our 20->23 with die 3
        Assert.Equal(20, moves[1].From);
        Assert.Equal(23, moves[1].To);
        Assert.Equal(3, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_CompoundMoveWithHit_ParsesCorrectly()
    {
        // Arrange
        // gnubg returns "6/5*/2" - move from 6 to 5 (hitting), then 5 to 2
        // The * is stripped, same as 6/5/2
        var notation = "6/5*/2";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 3, 1 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: 6/5 = our 19->20 with die 1
        Assert.Equal(19, moves[0].From);
        Assert.Equal(20, moves[0].To);
        Assert.Equal(1, moves[0].DieValue);

        // Second move: 5/2 = our 20->23 with die 3
        Assert.Equal(20, moves[1].From);
        Assert.Equal(23, moves[1].To);
        Assert.Equal(3, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_CompoundMoveWhite_ParsesCorrectly()
    {
        // Arrange
        // For White: gnubg "13/10/7" = our 13->10->7 (no transformation)
        var notation = "13/10/7";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 3, 3 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: 13/10 with die 3
        Assert.Equal(13, moves[0].From);
        Assert.Equal(10, moves[0].To);
        Assert.Equal(3, moves[0].DieValue);

        // Second move: 10/7 with die 3
        Assert.Equal(10, moves[1].From);
        Assert.Equal(7, moves[1].To);
        Assert.Equal(3, moves[1].DieValue);
    }

    // ===================
    // Abbreviated Bar Entry Tests
    // ===================

    [Fact]
    public void ParseMoveNotation_AbbreviatedBarEntry_ExpandsTwoMoves()
    {
        // Arrange
        // gnubg returns "bar/17" for Red with dice [3,5]
        // gnubg point 17 for Red = our point 25-17 = 8
        // Either: enter at 3 (die 3) then move to 8 (die 5)
        // Or: enter at 5 (die 5) then move to 8 (die 3)
        var notation = "bar/17";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 3, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move must be from bar
        Assert.Equal(0, moves[0].From);

        // Final destination must be point 8
        Assert.Equal(8, moves[1].To);

        // Dice used must be 3 and 5 (in either order)
        var diceUsed = new[] { moves[0].DieValue, moves[1].DieValue }.OrderBy(d => d).ToList();
        Assert.Equal(new[] { 3, 5 }, diceUsed);
    }

    [Fact]
    public void ParseMoveNotation_AbbreviatedBarEntryReverseDice_ExpandsCorrectly()
    {
        // Arrange
        // Same as above but with dice order where smaller die is used for entry
        // gnubg point 17 for Red = our point 8 (total distance 8 = 3+5)
        // Could enter at 3 then move 5, or enter at 5 then move 3
        var notation = "bar/17";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5, 3 }; // Different order

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);
        // Either order is valid as long as we reach point 8
        Assert.Equal(0, moves[0].From);
        Assert.Equal(8, moves[1].To);
    }

    [Fact]
    public void ParseMoveNotation_AbbreviatedMoveRed_LargerDieFirst()
    {
        // Arrange
        // gnubg returns "24/13" for Red with dice [5,6] - total distance 11
        // This should expand to use larger die (6) first: 1->7->12
        // because intermediate point 6 might be blocked
        var notation = "24/13";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5, 6 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // Should use larger die (6) first: 1 -> 7
        Assert.Equal(1, moves[0].From);
        Assert.Equal(7, moves[0].To);
        Assert.Equal(6, moves[0].DieValue);

        // Then smaller die (5): 7 -> 12
        Assert.Equal(7, moves[1].From);
        Assert.Equal(12, moves[1].To);
        Assert.Equal(5, moves[1].DieValue);
    }

    // ===================
    // Regular Move Tests
    // ===================

    [Fact]
    public void ParseMoveNotation_SimpleMoves_ParsesCorrectly()
    {
        // Arrange
        var notation = "24/20 13/9";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 4, 4 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);
        Assert.Equal(24, moves[0].From);
        Assert.Equal(20, moves[0].To);
        Assert.Equal(4, moves[0].DieValue);

        Assert.Equal(13, moves[1].From);
        Assert.Equal(9, moves[1].To);
        Assert.Equal(4, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_RepetitionNotation_ExpandsCorrectly()
    {
        // Arrange
        // "6/5(3)" means move 6->5 three times (doubles)
        var notation = "6/5(3)";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 1, 1, 1, 1 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(3, moves.Count);
        foreach (var move in moves)
        {
            Assert.Equal(6, move.From);
            Assert.Equal(5, move.To);
            Assert.Equal(1, move.DieValue);
        }
    }

    // ===================
    // Bear Off Tests
    // ===================

    [Fact]
    public void ParseMoveNotation_BearOff_ParsesCorrectly()
    {
        // Arrange
        var notation = "6/off 5/off";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 6, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // White bears off to point 0
        Assert.Equal(6, moves[0].From);
        Assert.Equal(0, moves[0].To);
        Assert.Equal(6, moves[0].DieValue);

        Assert.Equal(5, moves[1].From);
        Assert.Equal(0, moves[1].To);
        Assert.Equal(5, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithRepetition_ParsesCorrectly()
    {
        // Arrange
        // "3/off(2)" means bear off from point 3 twice (using both dice)
        // For Red with dice [4, 6]: gnubg point 3 = our point 22, bear off to 25
        var notation = "3/off(2)";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 4, 6 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // Red bears off to point 25
        // Both moves are from our point 22 (gnubg point 3)
        Assert.Equal(22, moves[0].From);
        Assert.Equal(25, moves[0].To);

        Assert.Equal(22, moves[1].From);
        Assert.Equal(25, moves[1].To);

        // Both dice (4 and 6) should be used since they're >= 3
        var diceUsed = new[] { moves[0].DieValue, moves[1].DieValue }.OrderBy(d => d).ToList();
        Assert.Equal(new[] { 4, 6 }, diceUsed);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithCombinedDice_ParsesCorrectly()
    {
        // Arrange
        // "6/off" with dice [2, 4] - no single die equals or exceeds 6
        // Need to combine: 2+4=6
        // For White: gnubg point 6 = our point 6, bear off to 0
        // Should expand to: 6->2 (die 4), then 2->0 (die 2, bear off)
        var notation = "6/off";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 2, 4 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: intermediate within home board
        Assert.Equal(6, moves[0].From);

        // Second move: bear off to point 0
        Assert.Equal(0, moves[1].To);

        // Both dice should be used
        var diceUsed = new[] { moves[0].DieValue, moves[1].DieValue }.OrderBy(d => d).ToList();
        Assert.Equal(new[] { 2, 4 }, diceUsed);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithCombinedDice_Red_ParsesCorrectly()
    {
        // Arrange
        // "6/off" with dice [2, 4] for Red
        // For Red: gnubg point 6 = our point 19 (25-6=19), bear off to 25
        // Should expand to: 19->23 (die 4), then 23->25 (die 2, bear off)
        // Or: 19->21 (die 2), then 21->25 (die 4, bear off)
        var notation = "6/off";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 2, 4 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move starts from point 19
        Assert.Equal(19, moves[0].From);

        // Second move bears off to point 25
        Assert.Equal(25, moves[1].To);

        // Both dice should be used
        var diceUsed = new[] { moves[0].DieValue, moves[1].DieValue }.OrderBy(d => d).ToList();
        Assert.Equal(new[] { 2, 4 }, diceUsed);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithCombinedDice_Doubles_ParsesCorrectly()
    {
        // Arrange
        // "4/off" with dice [2, 2] - using same die twice
        // For White: gnubg point 4 = our point 4, bear off to 0
        // Should expand to: 4->2 (die 2), then 2->0 (die 2, bear off)
        var notation = "4/off";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 2, 2 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        Assert.Equal(2, moves.Count);

        // First move: 4->2
        Assert.Equal(4, moves[0].From);
        Assert.Equal(2, moves[0].To);
        Assert.Equal(2, moves[0].DieValue);

        // Second move: 2->0 (bear off)
        Assert.Equal(2, moves[1].From);
        Assert.Equal(0, moves[1].To);
        Assert.Equal(2, moves[1].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithCombinedDice_FiveThree_ParsesCorrectly()
    {
        // Arrange
        // "5/off" with dice [3, 5] for White
        // The 5 is available, so it should use exact die, not combine
        var notation = "5/off";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 3, 5 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        // Should use exact die (5), not combine
        Assert.Single(moves);
        Assert.Equal(5, moves[0].From);
        Assert.Equal(0, moves[0].To);
        Assert.Equal(5, moves[0].DieValue);
    }

    [Fact]
    public void ParseMoveNotation_BearOffWithHigherDie_UsesHigherDie()
    {
        // Arrange
        // "3/off" with dice [5, 6] for White
        // Higher die (5 or 6) should be used for overshoot bearing off
        var notation = "3/off";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 5, 6 };

        // Act
        var moves = GnubgOutputParser.ParseMoveNotation(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Parsed moves: {string.Join(", ", moves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}");

        // Should use higher die for overshoot
        Assert.Single(moves);
        Assert.Equal(3, moves[0].From);
        Assert.Equal(0, moves[0].To);
        Assert.True(moves[0].DieValue >= 3); // Either 5 or 6
    }

    // ===================
    // Alternatives Tests
    // ===================

    [Fact]
    public void ParseMoveNotationWithAlternatives_AbbreviatedMove_ReturnsBothOrderings()
    {
        // Arrange
        // "24/15" with dice [5, 4] = total distance 9
        // Two possible orderings:
        // 1. 1->6 (die 5) then 6->10 (die 4)
        // 2. 1->5 (die 4) then 5->10 (die 5)
        var notation = "24/15";
        var color = CheckerColor.Red;
        var availableDice = new List<int> { 5, 4 };

        // Act
        var alternatives = GnubgOutputParser.ParseMoveNotationWithAlternatives(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Alternatives count: {alternatives.Count}");
        foreach (var alt in alternatives)
        {
            _output.WriteLine($"  [{string.Join(", ", alt.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})"))}]");
        }

        Assert.Equal(2, alternatives.Count);

        // Both alternatives should end at point 10 (gnubg 15 = our 10 for Red)
        Assert.Equal(10, alternatives[0].Last().To);
        Assert.Equal(10, alternatives[1].Last().To);

        // First alternative uses larger die first (5)
        Assert.Equal(5, alternatives[0][0].DieValue);
        Assert.Equal(4, alternatives[0][1].DieValue);

        // Second alternative uses smaller die first (4)
        Assert.Equal(4, alternatives[1][0].DieValue);
        Assert.Equal(5, alternatives[1][1].DieValue);
    }

    [Fact]
    public void ParseMoveNotationWithAlternatives_NonAbbreviated_ReturnsSingleOption()
    {
        // Arrange
        // Regular move that doesn't need expansion
        var notation = "24/20 13/9";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 4, 4 };

        // Act
        var alternatives = GnubgOutputParser.ParseMoveNotationWithAlternatives(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Alternatives count: {alternatives.Count}");

        // Non-abbreviated moves should have only one alternative
        Assert.Single(alternatives);
        Assert.Equal(2, alternatives[0].Count);
    }

    [Fact]
    public void ParseMoveNotationWithAlternatives_DoublesAbbreviated_ReturnsSingleOption()
    {
        // Arrange
        // "6/2" with dice [2, 2] = using same die twice, no ordering ambiguity
        var notation = "6/2";
        var color = CheckerColor.White;
        var availableDice = new List<int> { 2, 2 };

        // Act
        var alternatives = GnubgOutputParser.ParseMoveNotationWithAlternatives(notation, color, availableDice);

        // Assert
        _output.WriteLine($"Alternatives count: {alternatives.Count}");

        // Doubles with same die value have no ordering ambiguity
        Assert.Single(alternatives);
        Assert.Equal(2, alternatives[0].Count);
        Assert.Equal(2, alternatives[0][0].DieValue);
        Assert.Equal(2, alternatives[0][1].DieValue);
    }
}

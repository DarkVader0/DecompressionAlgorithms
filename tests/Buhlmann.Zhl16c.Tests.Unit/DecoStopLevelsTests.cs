using Buhlmann.Zhl16c.Constants;

namespace Buhlmann.Zhl16c.Tests.Unit;

public sealed class DecoStopLevelsTests
{
    [Fact]
    public void GetNextStopLevel_ShouldReturn0_WhenDepthIs0()
    {
        // Arrange
        var depthMm = 0u;

        // Act
        var result = DecoStopLevels.GetNextStopLevel(depthMm);

        // Assert
        Assert.Equal(0u, result);
    }

    [Fact]
    public void GetNextStopLevel_ShouldReturn0_WhenDepthIsAtFirstLevel()
    {
        // Arrange
        var depthMm = 3000u;

        // Act
        var result = DecoStopLevels.GetNextStopLevel(depthMm);

        // Assert
        Assert.Equal(0u, result);
    }

    [Fact]
    public void GetNextStopLevel_ShouldReturnLowerLevel_WhenDepthIsBetweenLevels()
    {
        // Arrange
        var depthMm = 4501u;

        // Act
        var result = DecoStopLevels.GetNextStopLevel(depthMm);

        // Assert
        Assert.Equal(3000u, result);
    }

    [Fact]
    public void GetNextStopLevel_ShouldReturnPreviousLevel_WhenDepthIsJustAboveLevel()
    {
        // Arrange
        var depthMm = 3001u;

        // Act
        var result = DecoStopLevels.GetNextStopLevel(depthMm);

        // Assert
        Assert.Equal(3000u, result);
    }

    [Fact]
    public void GetNextStopLevel_ShouldReturnSecondToLastLevel_WhenDepthIsAboveMaxLevel()
    {
        // Arrange
        var depthMm = 400000u;

        // Act
        var result = DecoStopLevels.GetNextStopLevel(depthMm);

        // Assert
        Assert.Equal(380000u, result);
    }

    [Fact]
    public void RoundUpToStopLevel_ShouldReturn0_WhenDepthIs0()
    {
        // Arrange
        var depthMm = 0u;

        // Act
        var result = DecoStopLevels.RoundUpToStopLevel(depthMm);

        // Assert
        Assert.Equal(0u, result);
    }

    [Fact]
    public void RoundUpToStopLevel_ShouldReturnSameLevel_WhenDepthIsExactStopLevel()
    {
        // Arrange
        var depthMm = 6000u;

        // Act
        var result = DecoStopLevels.RoundUpToStopLevel(depthMm);

        // Assert
        Assert.Equal(6000u, result);
    }

    [Fact]
    public void RoundUpToStopLevel_ShouldReturnNextLevel_WhenDepthIsBetweenLevels()
    {
        // Arrange
        var depthMm = 6001u;

        // Act
        var result = DecoStopLevels.RoundUpToStopLevel(depthMm);

        // Assert
        Assert.Equal(9000u, result);
    }

    [Fact]
    public void RoundUpToStopLevel_ShouldReturnMaxLevel_WhenDepthIsAboveMaxLevel()
    {
        // Arrange
        var depthMm = 999999u;

        // Act
        var result = DecoStopLevels.RoundUpToStopLevel(depthMm);

        // Assert
        Assert.Equal(380000u, result);
    }

    [Fact]
    public void GetStopLevelIndex_ShouldReturnIndex_WhenDepthIsStopLevel()
    {
        // Arrange
        var depthMm = 9000u;

        // Act
        var result = DecoStopLevels.GetStopLevelIndex(depthMm);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetStopLevelIndex_ShouldReturn0_WhenDepthIs0()
    {
        // Arrange
        var depthMm = 0u;

        // Act
        var result = DecoStopLevels.GetStopLevelIndex(depthMm);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetStopLevelIndex_ShouldReturnMinus1_WhenDepthIsNotStopLevel()
    {
        // Arrange
        var depthMm = 1u;

        // Act
        var result = DecoStopLevels.GetStopLevelIndex(depthMm);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetStopLevelIndex_ShouldReturnLastIndex_WhenDepthIsMaxStopLevel()
    {
        // Arrange
        var depthMm = 380000u;

        // Act
        var result = DecoStopLevels.GetStopLevelIndex(depthMm);

        // Assert
        Assert.Equal(DecoStopLevels.Mm.Length - 1, result);
    }
}
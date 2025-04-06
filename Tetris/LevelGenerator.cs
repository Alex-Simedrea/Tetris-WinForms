using System;
using System.Drawing;
using Tetris;

public class LevelGenerator
{
    private static readonly Random random = new Random();

    private enum LevelPatternType
    {
        SimpleLines,       // Simple pattern for beginners
        HolesInMiddle,
        HolesAtMargins,
        DiagonalGrowth,
        ZigZag,
        PyramidStructure,
        CheckerboardPattern
    }

    /// <summary>
    /// Generates a level based on the level index
    /// </summary>
    /// <param name="levelIndex">The index of the level (1-based)</param>
    /// <param name="width">Width of the canvas</param>
    /// <param name="height">Height of the canvas</param>
    /// <param name="scoreTarget">Output score target for this level</param>
    /// <param name="allowedMoves">Output allowed moves for this level</param>
    /// <returns>A 2D array of CanvasBlocks representing the initial state of the level</returns>
    public static CanvasBlock[,] GenerateLevel(int levelIndex, int width, int height,
                                              out int scoreTarget, out int allowedMoves)
    {
        // Initialize the canvas with empty blocks
        CanvasBlock[,] canvas = new CanvasBlock[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                canvas[x, y] = new CanvasBlock
                {
                    IsFilled = false,
                    Color = Color.Transparent
                };
            }
        }

        // Calculate difficulty parameters
        double difficultyFactor = Math.Min(levelIndex / 100.0, 0.9); // Caps at 90% for level 100

        // Calculate the fill height based on level index (never above half the grid)
        int maxFillHeight = height / 2; // Strict limit at half grid height
        int fillHeight = CalculateFillHeight(levelIndex, height);

        // Make sure fill height is within allowed range
        fillHeight = Math.Min(fillHeight, maxFillHeight);

        // Determine pattern type based on level index
        LevelPatternType patternType = GetPatternTypeForLevel(levelIndex);

        // Add the base fill at the bottom of the grid first
        int blockCount = FillBaseHeight(canvas, width, height, fillHeight, levelIndex);

        // Calculate how much height is left for pattern generation
        // This ensures patterns only go up to half the grid height at maximum
        int availablePatternHeight = maxFillHeight - fillHeight;
        if (availablePatternHeight > 0)
        {
            // Only generate patterns if there's space available below the half-height mark
            blockCount += FillCanvasWithPattern(canvas, patternType, width, height, fillHeight, availablePatternHeight, levelIndex);
        }

        // Calculate score target and allowed moves
        CalculateLevelRequirements(levelIndex, width, height, blockCount,
                                   out scoreTarget, out allowedMoves);

        return canvas;
    }

    private static int CalculateFillHeight(int levelIndex, int height)
    {
        // Ensure we never exceed half the grid height
        int maxAllowedHeight = height / 2;

        // For very early levels, use much smaller fill heights
        if (levelIndex <= 5)
        {
            // Levels 1-5: Very minimal fill (5-10% of height)
            return Math.Min((int)(height * (0.05 + (levelIndex - 1) * 0.01)), maxAllowedHeight);
        }
        else if (levelIndex <= 20)
        {
            // Levels 6-20: Gradually increase (10-25% of height)
            double percentage = 0.10 + ((levelIndex - 5) * 0.01);
            return Math.Min((int)(height * percentage), maxAllowedHeight);
        }
        else if (levelIndex <= 50)
        {
            // Levels 21-50: Medium fill (25-35% of height)
            double percentage = 0.25 + ((levelIndex - 20) * 0.003);
            return Math.Min((int)(height * percentage), maxAllowedHeight);
        }
        else
        {
            // Levels 51+: Higher fill but never exceeds half height
            double percentage = 0.35 + ((levelIndex - 50) * 0.002);
            return Math.Min((int)(height * Math.Min(percentage, 0.5)), maxAllowedHeight);
        }
    }

    private static LevelPatternType GetPatternTypeForLevel(int levelIndex)
    {
        // Start with simpler patterns for early levels
        if (levelIndex <= 5)
        {
            // First 5 levels only get the simplest pattern
            return LevelPatternType.SimpleLines;
        }
        else if (levelIndex <= 15)
        {
            // Levels 6-15 get simple or holes patterns
            int pattern = (levelIndex - 6) % 3;
            switch (pattern)
            {
                case 0: return LevelPatternType.SimpleLines;
                case 1: return LevelPatternType.HolesAtMargins;
                default: return LevelPatternType.HolesInMiddle;
            }
        }
        else if (levelIndex <= 30)
        {
            // Levels 16-30 get intermediate patterns
            int pattern = (levelIndex - 16) % 4;
            switch (pattern)
            {
                case 0: return LevelPatternType.HolesAtMargins;
                case 1: return LevelPatternType.HolesInMiddle;
                case 2: return LevelPatternType.DiagonalGrowth;
                default: return LevelPatternType.ZigZag;
            }
        }
        else
        {
            // Levels 31+ get all pattern types
            int pattern = (levelIndex - 31) % 7;
            return (LevelPatternType)pattern;
        }
    }

    private static int FillCanvasWithPattern(CanvasBlock[,] canvas, LevelPatternType patternType,
                                           int width, int height, int fillHeight, int availablePatternHeight, int levelIndex)
    {
        Color[] levelColors = GetLevelColors(levelIndex);
        int blockCount = 0;

        // For very low levels, ensure minimal pattern blocks
        if (levelIndex <= 3)
        {
            availablePatternHeight = Math.Min(availablePatternHeight, 2); // Maximum 2 rows for first 3 levels
        }

        // Skip pattern generation entirely for level 1
        if (levelIndex == 1)
        {
            return 0;
        }

        // The startY value is the position where patterns will begin
        // This ensures we only place blocks in the lower half of the grid
        int startY = height - fillHeight - availablePatternHeight;

        switch (patternType)
        {
            case LevelPatternType.SimpleLines:
                blockCount = GenerateSimpleLinesPattern(canvas, width, height, startY, availablePatternHeight, levelColors, levelIndex);
                break;

            case LevelPatternType.HolesInMiddle:
                blockCount = GenerateHolesInMiddlePattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;

            case LevelPatternType.HolesAtMargins:
                blockCount = GenerateHolesAtMarginsPattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;

            case LevelPatternType.DiagonalGrowth:
                blockCount = GenerateDiagonalGrowthPattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;

            case LevelPatternType.ZigZag:
                blockCount = GenerateZigZagPattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;

            case LevelPatternType.PyramidStructure:
                blockCount = GeneratePyramidStructurePattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;

            case LevelPatternType.CheckerboardPattern:
                blockCount = GenerateCheckerboardPattern(canvas, width, height, startY, availablePatternHeight, levelColors);
                break;
        }

        return blockCount;
    }

    private static int FillBaseHeight(CanvasBlock[,] canvas, int width, int height, int fillHeight, int levelIndex)
    {
        Color[] levelColors = GetLevelColors(levelIndex);
        int blockCount = 0;

        // Calculate the density of holes based on level
        // Lower levels have more holes to make them easier
        double holeChance = Math.Max(0.05, 0.4 - (levelIndex / 200.0));

        // For very early levels, add even more holes
        if (levelIndex <= 5)
        {
            holeChance += 0.2; // 20% more holes in the first 5 levels
        }

        // Fill the bottom portion up to fillHeight
        for (int y = height - 1; y >= height - fillHeight; y--)
        {
            for (int x = 0; x < width; x++)
            {
                // Introduce some strategic holes
                if (random.NextDouble() > holeChance)
                {
                    canvas[x, y].IsFilled = true;
                    canvas[x, y].Color = levelColors[random.Next(levelColors.Length)];
                    blockCount++;
                }
            }
        }

        // Ensure that the top row of the filled base has more holes
        // This makes it easier for the player to start playing
        if (fillHeight > 0)
        {
            int topBaseRow = height - fillHeight;
            for (int x = 0; x < width; x++)
            {
                if (random.NextDouble() < 0.5) // 50% chance to create a hole (increased from 40%)
                {
                    canvas[x, topBaseRow].IsFilled = false;
                    canvas[x, topBaseRow].Color = Color.Transparent;
                    blockCount--;
                }
            }

            // Ensure there are multiple holes in the top row for early levels
            int guaranteedHoles = Math.Max(2, width / 4);
            if (levelIndex <= 10)
            {
                guaranteedHoles = Math.Max(3, width / 3); // More guaranteed holes for early levels
            }

            int holesCreated = 0;
            for (int x = 0; x < width; x++)
            {
                if (!canvas[x, topBaseRow].IsFilled)
                {
                    holesCreated++;
                }
            }

            // Create more holes if needed
            while (holesCreated < guaranteedHoles)
            {
                int holePosition = random.Next(width);
                if (canvas[holePosition, topBaseRow].IsFilled)
                {
                    canvas[holePosition, topBaseRow].IsFilled = false;
                    canvas[holePosition, topBaseRow].Color = Color.Transparent;
                    blockCount--;
                    holesCreated++;
                }
            }
        }

        return blockCount;
    }

    private static Color[] GetLevelColors(int levelIndex)
    {
        // Different color schemes for different level ranges
        if (levelIndex <= 10)
        {
            return new Color[] { Color.LightBlue, Color.LightGreen, Color.LightPink };
        }
        else if (levelIndex <= 30)
        {
            return new Color[] { Color.Orange, Color.Yellow, Color.LightCoral };
        }
        else if (levelIndex <= 60)
        {
            return new Color[] { Color.Purple, Color.Magenta, Color.Cyan };
        }
        else
        {
            return new Color[] { Color.Red, Color.Blue, Color.Green, Color.Yellow };
        }
    }

    // New pattern implementation for the simplest levels
    private static int GenerateSimpleLinesPattern(CanvasBlock[,] canvas, int width, int height,
                                               int startY, int availableHeight, Color[] colors, int levelIndex)
    {
        int blockCount = 0;

        // Determine how many lines to create based on level index
        int lineCount = Math.Min(2, availableHeight);
        if (levelIndex > 3)
        {
            lineCount = Math.Min(3, availableHeight);
        }

        // Create simple horizontal lines with gaps
        for (int line = 0; line < lineCount; line++)
        {
            // Calculate Y position ensuring we stay below half grid height
            int y = startY + availableHeight - line - 1;
            if (y >= height || y < startY) continue;

            Color lineColor = colors[line % colors.Length];

            // For the first few levels, create very sparse lines
            double fillChance = 0.5;
            if (levelIndex <= 3)
            {
                fillChance = 0.3; // Only 30% fill for early levels
            }

            // Add more gaps for easier gameplay
            for (int x = 0; x < width; x++)
            {
                if (random.NextDouble() < fillChance)
                {
                    canvas[x, y].IsFilled = true;
                    canvas[x, y].Color = lineColor;
                    blockCount++;
                }
            }

            // Ensure there are gaps in each line
            int guaranteedGaps = width / 3;
            int gapsCreated = 0;

            for (int x = 0; x < width; x++)
            {
                if (!canvas[x, y].IsFilled)
                {
                    gapsCreated++;
                }
            }

            while (gapsCreated < guaranteedGaps)
            {
                int position = random.Next(width);
                if (canvas[position, y].IsFilled)
                {
                    canvas[position, y].IsFilled = false;
                    canvas[position, y].Color = Color.Transparent;
                    blockCount--;
                    gapsCreated++;
                }
            }
        }

        return blockCount;
    }

    private static int GenerateHolesInMiddlePattern(CanvasBlock[,] canvas, int width, int height,
                                                 int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;

        // Skip if not enough height available
        if (availableHeight <= 2) return 0;

        int centerX = width / 2;
        int centerY = startY + availableHeight / 2;
        int maxRadius = Math.Min(width, availableHeight) / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = startY; y < startY + availableHeight; y++)
            {
                // Only process rows within our allowed height range
                if (y >= height) continue;

                // Calculate distance from center
                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));

                // Fill blocks closer to the edge, leave the middle empty
                if (distance > maxRadius * 0.5 && distance < maxRadius * 0.9)
                {
                    if (random.NextDouble() < 0.6) // Reduced from 70% to 60% chance
                    {
                        canvas[x, y].IsFilled = true;
                        canvas[x, y].Color = colors[random.Next(colors.Length)];
                        blockCount++;
                    }
                }
            }
        }

        return blockCount;
    }

    private static int GenerateHolesAtMarginsPattern(CanvasBlock[,] canvas, int width, int height,
                                                  int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;
        int margin = Math.Max(2, width / 6);

        for (int x = margin; x < width - margin; x++)
        {
            for (int y = startY; y < startY + availableHeight; y++)
            {
                // Only process rows within our allowed height range
                if (y >= height) continue;

                if (random.NextDouble() < 0.6) // Reduced from 70% to 60% chance
                {
                    canvas[x, y].IsFilled = true;
                    canvas[x, y].Color = colors[random.Next(colors.Length)];
                    blockCount++;
                }
            }
        }

        return blockCount;
    }

    private static int GenerateDiagonalGrowthPattern(CanvasBlock[,] canvas, int width, int height,
                                                  int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;
        double fillProbability = 0.5; // Reduced from 0.6 base probability

        for (int x = 0; x < width; x++)
        {
            for (int y = startY; y < startY + availableHeight; y++)
            {
                // Only process rows within our allowed height range
                if (y >= height) continue;

                // The closer to the main diagonal, the higher the probability of a block
                double diagonalFactor = 1.0 - Math.Abs((double)x / width - (double)(y - startY) / availableHeight);

                if (random.NextDouble() < fillProbability * diagonalFactor * 2)
                {
                    canvas[x, y].IsFilled = true;
                    canvas[x, y].Color = colors[random.Next(colors.Length)];
                    blockCount++;
                }
            }
        }

        return blockCount;
    }

    private static int GenerateZigZagPattern(CanvasBlock[,] canvas, int width, int height,
                                          int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;
        int zigzagWidth = Math.Max(2, width / 10);
        int zigzagCount = availableHeight / zigzagWidth;

        for (int zigzag = 0; zigzag < zigzagCount; zigzag++)
        {
            bool goingRight = zigzag % 2 == 0;
            int currentStartY = startY + (zigzag * zigzagWidth);
            int endY = Math.Min(currentStartY + zigzagWidth, startY + availableHeight);

            for (int y = currentStartY; y < endY; y++)
            {
                // Only process rows within our allowed height range
                if (y >= height) continue;

                int startX = goingRight ? 0 : width - 1;
                int endX = goingRight ? width : -1;
                int step = goingRight ? 1 : -1;

                for (int x = startX; x != endX; x += step)
                {
                    if (random.NextDouble() < 0.6) // Reduced from 70% to 60% chance
                    {
                        canvas[x, y].IsFilled = true;
                        canvas[x, y].Color = colors[random.Next(colors.Length)];
                        blockCount++;
                    }
                }
            }
        }

        return blockCount;
    }

    private static int GeneratePyramidStructurePattern(CanvasBlock[,] canvas, int width, int height,
                                                    int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;
        int pyramidHeight = Math.Min(availableHeight, 6); // Reduced from 8 to 6
        int baseWidth = width;

        // Start from the bottom of the available area
        int pyramidBottom = startY + availableHeight - 1;

        for (int level = 0; level < pyramidHeight; level++)
        {
            int currentWidth = baseWidth - (level * 2);
            int startX = (width - currentWidth) / 2;
            int y = pyramidBottom - level;

            // Skip if we're outside the valid range
            if (y < startY || y >= height) continue;

            for (int x = startX; x < startX + currentWidth; x++)
            {
                if (x >= 0 && x < width)
                {
                    if (random.NextDouble() < 0.8) // Reduced from 90% to 80% chance
                    {
                        canvas[x, y].IsFilled = true;
                        canvas[x, y].Color = colors[level % colors.Length];
                        blockCount++;
                    }
                }
            }
        }

        return blockCount;
    }

    private static int GenerateCheckerboardPattern(CanvasBlock[,] canvas, int width, int height,
                                                int startY, int availableHeight, Color[] colors)
    {
        int blockCount = 0;
        int checkSize = Math.Max(1, 3); // Default check size

        for (int x = 0; x < width; x++)
        {
            for (int y = startY; y < startY + availableHeight; y++)
            {
                // Only process rows within our allowed height range
                if (y >= height) continue;

                if ((x / checkSize + (y - startY) / checkSize) % 2 == 0)
                {
                    if (random.NextDouble() < 0.7) // Reduced from 80% to 70% chance
                    {
                        canvas[x, y].IsFilled = true;
                        canvas[x, y].Color = colors[random.Next(colors.Length)];
                        blockCount++;
                    }
                }
            }
        }

        return blockCount;
    }

    private static void CalculateLevelRequirements(int levelIndex, int width, int height,
                                                 int blockCount, out int scoreTarget,
                                                 out int allowedMoves)
    {
        // Calculate score target based on level index and block count
        // Higher levels require higher scores
        scoreTarget = 1000 + (levelIndex * 500) + (blockCount * 50);

        // Calculate allowed moves based on level index and block count
        // Higher levels have fewer moves relative to the challenge
        int baseAllowedMoves = 30 + (levelIndex * 2);

        // Make early levels more forgiving
        double difficultyMultiplier = 1.0;
        if (levelIndex <= 10)
        {
            difficultyMultiplier = 1.2; // 20% more moves for early levels
        }
        else
        {
            difficultyMultiplier = Math.Max(0.6, 1.0 - (levelIndex / 200.0)); // Decreases with level
        }

        // Adjust moves based on block count - more blocks means more moves needed
        int blockCountAdjustment = blockCount / 4; // Increased adjustment (was /5)

        allowedMoves = (int)((baseAllowedMoves + blockCountAdjustment) * difficultyMultiplier);

        // Ensure minimum values
        scoreTarget = Math.Max(1000, scoreTarget);
        allowedMoves = Math.Max(20, allowedMoves); // Increased minimum from 15 to 20
    }
}
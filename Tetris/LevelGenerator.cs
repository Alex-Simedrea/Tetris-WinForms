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

    public enum LevelTargetType
    {
        Score,
        LinesCleared
    }

    /// <summary>
    /// Generates a level based on the level index
    /// </summary>
    /// <param name="levelIndex">The index of the level (1-based)</param>
    /// <param name="width">Width of the canvas</param>
    /// <param name="height">Height of the canvas</param>
    /// <param name="scoreTarget">Output score target for this level</param>
    /// <param name="allowedMoves">Output allowed moves for this level</param>
    /// <param name="targetType">Output target type (score or lines cleared)</param>
    /// <param name="linesTarget">Output lines cleared target for this level</param>
    /// <returns>A 2D array of CanvasBlocks representing the initial state of the level</returns>
    public static CanvasBlock[,] GenerateLevel(int levelIndex, int width, int height,
                                              out int scoreTarget, out int allowedMoves, 
                                              out LevelTargetType targetType, out int linesTarget)
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

        // Calculate difficulty parameters - much easier curve
        double difficultyFactor = Math.Min(levelIndex / 200.0, 0.7); // Caps at 70% for level 200 (was 90% at 100)

        // Calculate the fill height based on level index (much more conservative)
        int maxFillHeight = Math.Max(2, height / 3); // Allow up to 1/3 height (was 1/2)
        int fillHeight = CalculateFillHeight(levelIndex, height);

        // Make sure fill height is within allowed range
        fillHeight = Math.Min(fillHeight, maxFillHeight);

        // Determine pattern type based on level index
        LevelPatternType patternType = GetPatternTypeForLevel(levelIndex);

        // Add the base fill at the bottom of the grid first
        int blockCount = FillBaseHeight(canvas, width, height, fillHeight, levelIndex);

        // Calculate how much height is left for pattern generation
        int availablePatternHeight = maxFillHeight - fillHeight;
        if (availablePatternHeight > 0)
        {
            // Only generate patterns if there's space available
            blockCount += FillCanvasWithPattern(canvas, patternType, width, height, fillHeight, availablePatternHeight, levelIndex);
        }

        // Calculate targets and determine target type
        CalculateLevelRequirements(levelIndex, width, height, blockCount,
                                   out scoreTarget, out allowedMoves, out targetType, out linesTarget);

        return canvas;
    }

    /// <summary>
    /// Generates a level based on the level index (backward compatibility)
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
        LevelTargetType targetType;
        int linesTarget;
        return GenerateLevel(levelIndex, width, height, out scoreTarget, out allowedMoves, out targetType, out linesTarget);
    }

    private static int CalculateFillHeight(int levelIndex, int height)
    {
        // Ensure we never exceed 1/3 the grid height (much more conservative)
        int maxAllowedHeight = Math.Max(2, height / 3);

        // For very early levels, use minimal fill heights
        if (levelIndex <= 10)
        {
            // Levels 1-10: Very minimal fill (2-8% of height)
            return Math.Min((int)(height * (0.02 + (levelIndex - 1) * 0.006)), maxAllowedHeight);
        }
        else if (levelIndex <= 30)
        {
            // Levels 11-30: Gradually increase (8-15% of height)
            double percentage = 0.08 + ((levelIndex - 10) * 0.0035);
            return Math.Min((int)(height * percentage), maxAllowedHeight);
        }
        else if (levelIndex <= 60)
        {
            // Levels 31-60: Medium fill (15-22% of height)
            double percentage = 0.15 + ((levelIndex - 30) * 0.0023);
            return Math.Min((int)(height * percentage), maxAllowedHeight);
        }
        else
        {
            // Levels 61+: Higher fill but never exceeds 1/3 height
            double percentage = 0.22 + ((levelIndex - 60) * 0.0015);
            return Math.Min((int)(height * Math.Min(percentage, 0.33)), maxAllowedHeight);
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

        // Calculate the density of holes based on level - much more holes for easier gameplay
        double holeChance = Math.Max(0.1, 0.6 - (levelIndex / 300.0)); // More holes, slower decrease

        // For very early levels, add even more holes
        if (levelIndex <= 10)
        {
            holeChance += 0.3; // 30% more holes in the first 10 levels (was 20% for 5 levels)
        }
        else if (levelIndex <= 20)
        {
            holeChance += 0.2; // 20% more holes for levels 11-20
        }

        // Fill the bottom portion up to fillHeight
        for (int y = height - 1; y >= height - fillHeight; y--)
        {
            for (int x = 0; x < width; x++)
            {
                // Introduce strategic holes
                if (random.NextDouble() > holeChance)
                {
                    canvas[x, y].IsFilled = true;
                    canvas[x, y].Color = levelColors[random.Next(levelColors.Length)];
                    blockCount++;
                }
            }
        }

        // Ensure that the top row of the filled base has more holes
        if (fillHeight > 0)
        {
            int topBaseRow = height - fillHeight;
            for (int x = 0; x < width; x++)
            {
                if (random.NextDouble() < 0.6) // 60% chance to create a hole (increased from 50%)
                {
                    if (canvas[x, topBaseRow].IsFilled)
                    {
                        canvas[x, topBaseRow].IsFilled = false;
                        canvas[x, topBaseRow].Color = Color.Transparent;
                        blockCount--;
                    }
                }
            }

            // Ensure there are multiple holes in the top row for early levels
            int guaranteedHoles = Math.Max(3, width / 3); // More guaranteed holes
            if (levelIndex <= 15)
            {
                guaranteedHoles = Math.Max(4, width / 2); // Even more guaranteed holes for early levels
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
                                                 out int allowedMoves, out LevelTargetType targetType, out int linesTarget)
    {
        // Determine target type based on level index
        if (levelIndex <= 3)
        {
            // First 3 levels: Score targets only, very easy
            targetType = LevelTargetType.Score;
            scoreTarget = 200 + (levelIndex * 100); // 300, 400, 500
            linesTarget = 0;
        }
        else if (levelIndex <= 10)
        {
            // Levels 4-10: Score targets, easy
            targetType = LevelTargetType.Score;
            scoreTarget = 500 + (levelIndex * 150); // 650, 800, 950, etc.
            linesTarget = 0;
        }
        else if (levelIndex <= 20)
        {
            // Levels 11-20: Mix of score and lines targets
            if (levelIndex % 2 == 0)
            {
                targetType = LevelTargetType.LinesCleared;
                linesTarget = Math.Max(1, (levelIndex - 10) / 2); // 1, 2, 3, 4, 5 lines
                scoreTarget = 0;
            }
            else
            {
                targetType = LevelTargetType.Score;
                scoreTarget = 800 + (levelIndex * 200);
                linesTarget = 0;
            }
        }
        else
        {
            // Levels 21+: Alternating targets with moderate difficulty
            if (levelIndex % 3 == 0)
            {
                targetType = LevelTargetType.LinesCleared;
                linesTarget = Math.Max(2, (levelIndex - 15) / 3); // Progressive lines target
                scoreTarget = 0;
            }
            else
            {
                targetType = LevelTargetType.Score;
                scoreTarget = 1000 + (levelIndex * 250) + (blockCount * 30); // More achievable
                linesTarget = 0;
            }
        }

        // Calculate allowed moves - make it much more reasonable
        int baseAllowedMoves;
        if (levelIndex <= 5)
        {
            baseAllowedMoves = 15 + (levelIndex * 3); // 18, 21, 24, 27, 30 moves
        }
        else if (levelIndex <= 15)
        {
            baseAllowedMoves = 25 + (levelIndex * 2); // 27, 29, 31, etc.
        }
        else
        {
            baseAllowedMoves = 40 + levelIndex; // More reasonable progression
        }

        // Adjust moves based on block count - but much less generous
        int blockCountAdjustment = blockCount / 8; // Much less generous (was /2)

        // Apply difficulty multiplier - more reasonable
        double difficultyMultiplier = 1.2; // Base 20% more moves (was 50%)
        if (levelIndex <= 5)
        {
            difficultyMultiplier = 1.4; // 40% more moves for first 5 levels (was 100%)
        }
        else if (levelIndex <= 15)
        {
            difficultyMultiplier = 1.3; // 30% more moves for early levels (was 70%)
        }

        allowedMoves = (int)((baseAllowedMoves + blockCountAdjustment) * difficultyMultiplier);

        // Ensure minimum values - more reasonable
        if (targetType == LevelTargetType.Score)
        {
            scoreTarget = Math.Max(200, scoreTarget);
        }
        else
        {
            linesTarget = Math.Max(1, linesTarget);
        }
        allowedMoves = Math.Max(15, allowedMoves); // Reduced minimum from 40 to 15
    }
}
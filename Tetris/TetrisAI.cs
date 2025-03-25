using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    public class TetrisAI
    {
        private class Move
        {
            public int Rotation { get; set; }
            public int Position { get; set; }
            public double Score { get; set; }
        }

        private static readonly double HeightWeight = -0.510066;
        private static readonly double LinesWeight = 0.760667;
        private static readonly double HolesWeight = -0.35663;
        private static readonly double BumpinessWeight = -0.184483;

        /// <summary>
        /// Finds the best move for the current piece
        /// </summary>
        public (int rotation, int position) GetBestMove(CanvasBlock[,] board, Shape currentShape, int canvasWidth, int canvasHeight)
        {
            Move bestMove = new Move { Score = double.NegativeInfinity };
            int maxRotations = GetMaxRotations(currentShape);

            // Try all possible rotations and positions
            for (int rotation = 0; rotation < maxRotations; rotation++)
            {
                var testShape = CloneShape(currentShape);
                for (int r = 0; r < rotation; r++)
                    testShape.Turn();

                // Try all possible x positions
                for (int x = 0; x < canvasWidth - testShape.Width + 1; x++) // Modified x range
                {
                    // Check if this is a valid starting position
                    if (IsValidPlacement(board, testShape, x, 0, canvasWidth, canvasHeight))
                    {
                        // Find where piece would land
                        int landingHeight = FindLandingHeight(board, testShape, x, canvasWidth, canvasHeight);

                        if (landingHeight >= 0)
                        {
                            // Create a test board and place the piece
                            var testBoard = CloneBoard(board);
                            PlacePiece(testBoard, testShape, x, landingHeight);

                            // Evaluate the resulting position
                            double score = EvaluateBoard(testBoard, canvasHeight, canvasWidth);

                            if (score > bestMove.Score)
                            {
                                bestMove.Score = score;
                                bestMove.Position = x;
                                bestMove.Rotation = rotation;
                            }
                        }
                    }
                }
            }

            return (bestMove.Rotation, bestMove.Position);
        }

        private int GetMaxRotations(Shape shape)
        {
            // I piece and O piece have different rotation counts
            if (shape.Width == 1 && shape.Height == 4) return 2; // I piece
            if (shape.Width == 2 && shape.Height == 2) return 1; // O piece
            return 4; // All other pieces
        }

        private bool IsValidPlacement(CanvasBlock[,] board, Shape shape, int x, int y, int width, int height)
        {
            for (int i = 0; i < shape.Width; i++)
            {
                for (int j = 0; j < shape.Height; j++)
                {
                    if (shape.Blocks[j, i] == 1)
                    {
                        int boardX = x + i;
                        int boardY = y + j;

                        if (boardX < 0 || boardX >= width || boardY >= height)
                            return false;

                        if (boardY >= 0 && board[boardX, boardY].IsFilled)
                            return false;
                    }
                }
            }
            return true;
        }

        private int FindLandingHeight(CanvasBlock[,] board, Shape shape, int x, int width, int height)
        {
            int y = 0;
            while (y < height && IsValidPlacement(board, shape, x, y, width, height))
            {
                y++;
            }
            return y - 1;
        }

        private void PlacePiece(CanvasBlock[,] board, Shape shape, int x, int y)
        {
            for (int i = 0; i < shape.Width; i++)
            {
                for (int j = 0; j < shape.Height; j++)
                {
                    if (shape.Blocks[j, i] == 1)
                    {
                        board[x + i, y + j].IsFilled = true;
                    }
                }
            }
        }

        private double EvaluateBoard(CanvasBlock[,] board, int height, int width)
        {
            int holes = CountHoles(board, height, width);
            int completeLines = CountCompleteLines(board, height, width);
            double aggregateHeight = CalculateAggregateHeight(board, height, width);
            double bumpiness = CalculateBumpiness(board, height, width);

            return (HeightWeight * aggregateHeight) +
                   (LinesWeight * completeLines) +
                   (HolesWeight * holes) +
                   (BumpinessWeight * bumpiness);
        }

        private int CountHoles(CanvasBlock[,] board, int height, int width)
        {
            int holes = 0;
            for (int x = 0; x < width; x++)
            {
                bool blockFound = false;
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y].IsFilled)
                        blockFound = true;
                    else if (blockFound)
                        holes++;
                }
            }
            return holes;
        }

        private int CountCompleteLines(CanvasBlock[,] board, int height, int width)
        {
            int lines = 0;
            for (int y = 0; y < height; y++)
            {
                bool complete = true;
                for (int x = 0; x < width; x++)
                {
                    if (!board[x, y].IsFilled)
                    {
                        complete = false;
                        break;
                    }
                }
                if (complete) lines++;
            }
            return lines;
        }

        private double CalculateAggregateHeight(CanvasBlock[,] board, int height, int width)
        {
            double totalHeight = 0;
            for (int x = 0; x < width; x++)
            {
                totalHeight += GetColumnHeight(board, x, height);
            }
            return totalHeight;
        }

        private double CalculateBumpiness(CanvasBlock[,] board, int height, int width)
        {
            double bumpiness = 0;
            int[] heights = new int[width];

            for (int x = 0; x < width; x++)
            {
                heights[x] = GetColumnHeight(board, x, height);
            }

            for (int x = 0; x < width - 1; x++)
            {
                bumpiness += Math.Abs(heights[x] - heights[x + 1]);
            }

            return bumpiness;
        }

        private int GetColumnHeight(CanvasBlock[,] board, int x, int height)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y].IsFilled)
                    return height - y;
            }
            return 0;
        }

        private CanvasBlock[,] CloneBoard(CanvasBlock[,] original)
        {
            int width = original.GetLength(0);
            int height = original.GetLength(1);
            var clone = new CanvasBlock[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    clone[x, y] = original[x, y];
                }
            }

            return clone;
        }

        private Shape CloneShape(Shape original)
        {
            return new Shape
            {
                Width = original.Width,
                Height = original.Height,
                Blocks = (int[,])original.Blocks.Clone(),
                Color = original.Color
            };
        }
    }
}

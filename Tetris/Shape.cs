using System;
using System.Drawing;

namespace Tetris
{
    public class Shape
    {
        public int Width;
        public int Height;
        public int[,] Blocks;
        public Color Color;

        private int[,] backupBlocks;

        public void Turn()
        {
            backupBlocks = Blocks;

            Blocks = new int[Width, Height];
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    Blocks[i, j] = backupBlocks[Height - 1 - j, i];
                }
            }

            (Width, Height) = (Height, Width);
        }

        public void Rollback()
        {
            Blocks = backupBlocks;

            (Width, Height) = (Height, Width);
        }
    }
}
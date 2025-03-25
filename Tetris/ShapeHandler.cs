using System;
using System.Drawing;

namespace Tetris
{
    static class ShapeHandler
    {
        private static Shape[] shapesArray;

        private static Random random = new Random();

        static ShapeHandler()
        {
            shapesArray = new Shape[]
                {
                    new Shape {
                        Width = 2,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 1, 1 },
                            { 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 1,
                        Height = 4,
                        Blocks = new int[,]
                        {
                            { 1 },
                            { 1 },
                            { 1 },
                            { 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 0, 1, 0 },
                            { 1, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 0, 0, 1 },
                            { 1, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 1, 0, 0 },
                            { 1, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 1, 1, 0 },
                            { 0, 1, 1 }
                        }
                    },
                    new Shape {
                        Width = 3,
                        Height = 2,
                        Blocks = new int[,]
                        {
                            { 0, 1, 1 },
                            { 1, 1, 0 }
                        }
                    }
                };
        }

        private static Color GetRandomColor()
        {
            Color[] colors = new Color[]
            {
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Cyan,
            Color.Yellow,
            Color.Purple,
            Color.Orange
            };
            return colors[random.Next(colors.Length)];
        }

        public static Shape GetRandomShape()
        {
            var shape = shapesArray[random.Next(shapesArray.Length)];

            var newShape = new Shape
            {
                Width = shape.Width,
                Height = shape.Height,
                Blocks = (int[,])shape.Blocks.Clone(),
                Color = GetRandomColor()
            };

            return newShape;
        }
    }
}

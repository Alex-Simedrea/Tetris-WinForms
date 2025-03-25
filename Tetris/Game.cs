using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tetris
{
    public struct CanvasBlock
    {
        public bool IsFilled;
        public Color Color;
    }

    public partial class Game : Form
    {
        private Shape currentShape;
        private Shape nextShape;
        private Shape heldShape;
        private readonly Timer timer = new Timer();
        private readonly int canvasWidth = 15;
        private readonly int canvasHeight = 20;
        private readonly int blockSize = 20;
        private int currentX;
        private int currentY;
        private int score;
        private int linesCleared = 0;
        private bool hasHeldThisTurn = false;

        private CanvasBlock[,] canvasBlockArray;
        private Bitmap canvasBitmap;
        private Graphics canvasGraphics;
        private Bitmap workingBitmap;
        private Graphics workingGraphics;
        private Bitmap nextShapeBitmap;
        private Graphics nextShapeGraphics;
        private Bitmap holdBitmap;
        private Graphics holdGraphics;

        private TetrisAI ai = new TetrisAI();
        private bool aiMode = false;
        private Timer aiTimer = new Timer { Interval = 500 };

        private bool isPaused = false;

        private static class ScoreSystem
        {
            public const int SOFT_DROP_MULTIPLIER = 1;
            public const int HARD_DROP_MULTIPLIER = 2;
            public const int SINGLE_LINE_CLEAR = 100;
            public const int DOUBLE_LINE_CLEAR = 300;
            public const int TRIPLE_LINE_CLEAR = 500;
        }

        public Game()
        {
            InitializeComponent();

            this.KeyPreview = true;

            foreach (Control control in this.Controls)
            {
                if (control is Button button)
                {
                    button.TabStop = false;
                    button.GotFocus += (s, e) => { ((Button)s).Focus(); };
                }
            }

            LoadCanvas();

            currentShape = GetRandomShapeWithCenterAligned();
            nextShape = GetNextShape();
            DrawHeldPiece();

            timer.Tick += Timer_Tick;
            timer.Interval = 500;
            timer.Start();

            aiTimer.Tick += AiTimer_Tick;

            this.KeyDown += Form1_KeyDown;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Space:
                    Form1_KeyDown(this, new KeyEventArgs(keyData));
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Focus();
        }

        /// <summary>
        /// Initializes the game canvas with a black background and gray grid
        /// </summary>
        private void LoadCanvas()
        {
            pictureBox1.Width = canvasWidth * blockSize;
            pictureBox1.Height = canvasHeight * blockSize;
            canvasBitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            canvasGraphics = Graphics.FromImage(canvasBitmap);

            canvasGraphics.FillRectangle(Brushes.Black, 0, 0, canvasBitmap.Width, canvasBitmap.Height);
            DrawGrid(canvasGraphics);

            pictureBox1.Image = canvasBitmap;
            canvasBlockArray = new CanvasBlock[canvasWidth, canvasHeight];

            holdBitmap = new Bitmap(6 * blockSize, 6 * blockSize);
            holdGraphics = Graphics.FromImage(holdBitmap);

            holdGraphics.FillRectangle(Brushes.Black, 0, 0, holdBitmap.Width, holdBitmap.Height);
            DrawGrid(holdGraphics);

            pictureBox3.Size = holdBitmap.Size;
            pictureBox3.Image = holdBitmap;
        }

        /// <summary>
        /// Draws a grid of empty blocks with gray borders
        /// </summary>
        private void DrawGrid(Graphics graphics)
        {
            using (Pen pen = new Pen(Color.DimGray, 1))
            {
                for (int i = 0; i < canvasWidth; i++)
                {
                    for (int j = 0; j < canvasHeight; j++)
                    {
                        graphics.DrawRectangle(pen, i * blockSize, j * blockSize, blockSize, blockSize);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new random shape and centers it at the top of the canvas
        /// </summary>
        private Shape GetRandomShapeWithCenterAligned()
        {
            var shape = ShapeHandler.GetRandomShape();
            currentX = 7;
            currentY = -1;
            return shape;
        }

        /// <summary>
        /// Main game timer tick - handles piece falling and placement
        /// </summary
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!MoveShapeIfPossible(moveDown: 1))
            {
                canvasBitmap = new Bitmap(workingBitmap);
                UpdateCanvasDotArrayWithCurrentShape();
                currentShape = nextShape;
                nextShape = GetNextShape();
                ClearFilledRowsAndUpdateScore();
                hasHeldThisTurn = false;
            }
        }

        /// <summary>
        /// Updates the canvas array with the current shape's position and color
        /// </summary>
        private void UpdateCanvasDotArrayWithCurrentShape()
        {
            for (int i = 0; i < currentShape.Width; i++)
            {
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (currentShape.Blocks[j, i] == 1)
                    {
                        CheckIfGameOver(currentShape);
                        canvasBlockArray[currentX + i, currentY + j].IsFilled = true;
                        canvasBlockArray[currentX + i, currentY + j].Color = currentShape.Color;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the game is over (piece placed above top of board)
        /// </summary>
        private void CheckIfGameOver(Shape shape)
        {
            if (currentY < 0)
            {
                //this.label4.Text = $"{printShape(shape)}";
                aiTimer.Stop();
                timer.Stop();
                MessageBox.Show("Game Over");
                //Application.Restart();
                aiTimer.Start();
            }
        }

        /// <summary>
        /// Attempts to move the current shape to a new position
        /// </summary>
        /// <returns>True if move was successful, false if blocked</returns>
        private bool MoveShapeIfPossible(int moveDown = 0, int moveSide = 0)
        {
            var newX = currentX + moveSide;
            var newY = currentY + moveDown;

            if (!IsValidPosition(newX, newY))
                return false;

            currentX = newX;
            currentY = newY;
            DrawShape();
            return true;
        }

        /// <summary>
        /// Calculates where the current shape would land if dropped
        /// </summary>
        private int GetProjectedY()
        {
            int projectedY = currentY;
            while (IsValidPosition(currentX, projectedY + 1))
                projectedY++;
            return projectedY;
        }

        /// <summary>
        /// Checks if a position is valid for the current shape
        /// </summary>
        private bool IsValidPosition(int x, int y)
        {
            if (x < 0 || x + currentShape.Width > canvasWidth || y + currentShape.Height > canvasHeight)
                return false;

            for (int i = 0; i < currentShape.Width; i++)
            {
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (y + j > 0 && canvasBlockArray[x + i, y + j].IsFilled && currentShape.Blocks[j, i] == 1)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Draws the current shape and its preview on the canvas
        /// </summary>
        private void DrawShape()
        {
            workingBitmap = new Bitmap(canvasBitmap);
            workingGraphics = Graphics.FromImage(workingBitmap);

            int projectedY = GetProjectedY();
            if (projectedY != currentY)
                DrawShapePreview(projectedY);
            DrawCurrentShape();

            pictureBox1.Image = workingBitmap;
        }

        /// <summary>
        /// Draws the semi-transparent preview of where the shape will land
        /// </summary>
        private void DrawShapePreview(int projectedY)
        {
            for (int i = 0; i < currentShape.Width; i++)
            {
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (currentShape.Blocks[j, i] == 1)
                    {
                        using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, currentShape.Color)))
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            workingGraphics.FillRectangle(brush,
                                (currentX + i) * blockSize,
                                (projectedY + j) * blockSize,
                                blockSize, blockSize);
                            workingGraphics.DrawRectangle(pen,
                                (currentX + i) * blockSize,
                                (projectedY + j) * blockSize,
                                blockSize, blockSize);
                        }
                    }
                }
            }
        }

        private void DrawCurrentShape()
        {
            for (int i = 0; i < currentShape.Width; i++)
            {
                for (int j = 0; j < currentShape.Height; j++)
                {
                    if (currentShape.Blocks[j, i] == 1)
                    {
                        using (SolidBrush brush = new SolidBrush(currentShape.Color))
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            workingGraphics.FillRectangle(brush,
                                (currentX + i) * blockSize,
                                (currentY + j) * blockSize,
                                blockSize, blockSize);
                            workingGraphics.DrawRectangle(pen,
                                (currentX + i) * blockSize,
                                (currentY + j) * blockSize,
                                blockSize, blockSize);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles keyboard input for moving and rotating shapes
        /// </summary>
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (isPaused) return;

            if (e.KeyCode == Keys.F1)
            {
                ToggleAI();
                return;
            }

            if (aiMode) return;

            var verticalMove = 0;
            var horizontalMove = 0;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    verticalMove--;
                    break;
                case Keys.Right:
                    verticalMove++;
                    break;
                case Keys.Down:
                    horizontalMove++;
                    // Add soft drop score when successfully moved down
                    if (MoveShapeIfPossible(horizontalMove, verticalMove))
                    {
                        AddScore(ScoreSystem.SOFT_DROP_MULTIPLIER);
                    }
                    return;
                case Keys.Up:
                    currentShape.Turn();
                    break;
                case Keys.C:
                    HoldPiece();
                    return;
                case Keys.Space:
                    HardDrop();
                    return;
                default:
                    return;
            }

            var isMoveSuccess = MoveShapeIfPossible(horizontalMove, verticalMove);

            if (!isMoveSuccess && e.KeyCode == Keys.Up)
                currentShape.Rollback();
        }

        /// <summary>
        /// Checks for and clears completed rows, updates score and speed
        /// </summary>
        public void ClearFilledRowsAndUpdateScore()
        {
            int clearedLines = 0;
            List<int> rowsToClear = new List<int>();

            // First pass: count complete rows
            for (int i = 0; i < canvasHeight; i++)
            {
                if (IsRowComplete(i))
                {
                    clearedLines++;
                    rowsToClear.Add(i);
                }
            }

            // Add score based on number of lines cleared
            if (clearedLines > 0)
            {
                int lineScore = 0;
                switch (clearedLines)
                {
                    case 1:
                        lineScore = ScoreSystem.SINGLE_LINE_CLEAR;
                        break;
                    case 2:
                        lineScore = ScoreSystem.DOUBLE_LINE_CLEAR;
                        break;
                    case 3:
                        lineScore = ScoreSystem.TRIPLE_LINE_CLEAR;
                        break;
                    default:
                        if (clearedLines > 3)
                        {
                            lineScore += ScoreSystem.TRIPLE_LINE_CLEAR;
                        }
                        break;
                }
                AddScore(lineScore);
                UpdateLinesCleared(clearedLines);

                // Clear the rows and update level
                foreach (int row in rowsToClear.OrderByDescending(r => r))
                {
                    ShiftRowsDown(row);
                }

                UpdateLevel();
            }

            RedrawCanvas();
        }

        private void UpdateLinesCleared(int lines)
        {
            linesCleared += lines;
            label3.Text = $"Lines cleared: {linesCleared}";
        }

        /// <summary>
        /// Checks if a row is completely filled
        /// </summary>
        private bool IsRowComplete(int row)
        {
            for (int j = canvasWidth - 1; j >= 0; j--)
            {
                if (!canvasBlockArray[j, row].IsFilled)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Shifts all rows above the cleared row down by one position
        /// </summary>
        private void ShiftRowsDown(int clearedRow)
        {
            for (int j = 0; j < canvasWidth; j++)
            {
                for (int k = clearedRow; k > 0; k--)
                {
                    canvasBlockArray[j, k] = canvasBlockArray[j, k - 1];
                }
                canvasBlockArray[j, 0] = new CanvasBlock { IsFilled = false };
            }
        }

        /// <summary>
        /// Redraws the entire canvas based on the current state
        /// </summary>
        private void RedrawCanvas()
        {
            for (int i = 0; i < canvasWidth; i++)
            {
                for (int j = 0; j < canvasHeight; j++)
                {
                    canvasGraphics = Graphics.FromImage(canvasBitmap);
                    if (canvasBlockArray[i, j].IsFilled)
                    {
                        using (SolidBrush brush = new SolidBrush(canvasBlockArray[i, j].Color))
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            canvasGraphics.FillRectangle(brush,
                                i * blockSize, j * blockSize, blockSize, blockSize);
                            canvasGraphics.DrawRectangle(pen,
                                i * blockSize, j * blockSize, blockSize, blockSize);
                        }
                    }
                    else
                    {
                        canvasGraphics.FillRectangle(Brushes.Black,
                            i * blockSize, j * blockSize, blockSize, blockSize);
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            canvasGraphics.DrawRectangle(pen,
                                i * blockSize, j * blockSize, blockSize, blockSize);
                        }
                    }
                }
            }
            pictureBox1.Image = canvasBitmap;
        }

        /// <summary>
        /// Creates and displays the next shape in the preview panel
        /// </summary>
        private Shape GetNextShape()
        {
            var shape = GetRandomShapeWithCenterAligned();
            DrawNextShapePreview(shape);
            return shape;
        }

        /// <summary>
        /// Draws the next shape preview panel
        /// </summary>
        private void DrawNextShapePreview(Shape shape)
        {
            nextShapeBitmap = new Bitmap(6 * blockSize, 6 * blockSize);
            nextShapeGraphics = Graphics.FromImage(nextShapeBitmap);

            nextShapeGraphics.FillRectangle(Brushes.Black, 0, 0, nextShapeBitmap.Width, nextShapeBitmap.Height);
            DrawGrid(nextShapeGraphics);

            var startX = (6 - shape.Width) / 2;
            var startY = (6 - shape.Height) / 2;

            for (int i = 0; i < shape.Height; i++)
            {
                for (int j = 0; j < shape.Width; j++)
                {
                    if (shape.Blocks[i, j] == 1)
                    {
                        using (SolidBrush brush = new SolidBrush(shape.Color))
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            nextShapeGraphics.FillRectangle(brush,
                                (startX + j) * blockSize, (startY + i) * blockSize,
                                blockSize, blockSize);
                            nextShapeGraphics.DrawRectangle(pen,
                                (startX + j) * blockSize, (startY + i) * blockSize,
                                blockSize, blockSize);
                        }
                    }
                }
            }

            pictureBox2.Size = nextShapeBitmap.Size;
            pictureBox2.Image = nextShapeBitmap;
        }

        /// <summary>
        /// Handles the hold piece functionality
        /// </summary>
        private void HoldPiece()
        {
            if (hasHeldThisTurn)
                return;

            hasHeldThisTurn = true;

            if (heldShape == null)
            {
                // First hold - just store current piece and get a new one
                heldShape = currentShape;
                currentShape = nextShape;
                nextShape = GetRandomShapeWithCenterAligned();
            }
            else
            {
                // Swap current piece with held piece
                var temp = currentShape;
                currentShape = heldShape;
                heldShape = temp;

                // Reset position for the new current piece
                currentX = 7;
                currentY = -1;
            }

            DrawHeldPiece();
            DrawShape();
        }

        /// <summary>
        /// Draws the held piece preview panel
        /// </summary>
        private void DrawHeldPiece()
        {
            if (heldShape == null) return;

            holdBitmap = new Bitmap(6 * blockSize, 6 * blockSize);
            holdGraphics = Graphics.FromImage(holdBitmap);

            holdGraphics.FillRectangle(Brushes.Black, 0, 0, holdBitmap.Width, holdBitmap.Height);
            DrawGrid(holdGraphics);

            var startX = (6 - heldShape.Width) / 2;
            var startY = (6 - heldShape.Height) / 2;

            for (int i = 0; i < heldShape.Height; i++)
            {
                for (int j = 0; j < heldShape.Width; j++)
                {
                    if (heldShape.Blocks[i, j] == 1)
                    {
                        using (SolidBrush brush = new SolidBrush(heldShape.Color))
                        using (Pen pen = new Pen(Color.DimGray, 1))
                        {
                            holdGraphics.FillRectangle(brush,
                                (startX + j) * blockSize, (startY + i) * blockSize,
                                blockSize, blockSize);
                            holdGraphics.DrawRectangle(pen,
                                (startX + j) * blockSize, (startY + i) * blockSize,
                                blockSize, blockSize);
                        }
                    }
                }
            }

            pictureBox3.Size = holdBitmap.Size;
            pictureBox3.Image = holdBitmap;
        }

        /// <summary>
        /// Instantly drops the piece to its projected position with visual feedback
        /// </summary>
        private void HardDrop()
        {
            // Store the final position
            int finalY = GetProjectedY();
            int dropDistance = finalY - currentY;

            AddScore(dropDistance * ScoreSystem.HARD_DROP_MULTIPLIER);

            // Move the piece
            currentY = finalY;
            DrawShape();

            // Handle piece placement
            canvasBitmap = new Bitmap(workingBitmap);
            UpdateCanvasDotArrayWithCurrentShape();
            currentShape = nextShape;
            nextShape = GetNextShape();
            ClearFilledRowsAndUpdateScore();
            hasHeldThisTurn = false;
        }

        /// <summary>
        /// Adds to the score and updates the display
        /// </summary>
        private void AddScore(int points)
        {
            score += points;
            UpdateScoreDisplay();
        }

        /// <summary>
        /// Updates the score display
        /// </summary>
        private void UpdateScoreDisplay()
        {
            label1.Text = $"Score: {score:N0}";
        }

        /// <summary>
        /// Updates the level based on score and adjusts game speed
        /// </summary>
        private void UpdateLevel()
        {
            int level = linesCleared / 10 + 1;
            label2.Text = $"Level: {level}";

            timer.Interval = Math.Max(40, 500 - (level * 20));
        }

        private void AiTimer_Tick(object sender, EventArgs e)
        {
            if (!aiMode) return;

            var (rotation, position) = ai.GetBestMove(canvasBlockArray, currentShape, canvasWidth, canvasHeight);

            // Apply rotations
            while (rotation-- > 0)
            {
                currentShape.Turn();
                if (!IsValidPosition(currentX, currentY))
                {
                    currentShape.Rollback();
                    break;
                }
            }

            // Move to best position
            int moveDirection = Math.Sign(position - currentX);
            while (currentX != position)
            {
                if (!MoveShapeIfPossible(0, moveDirection))
                    break;
            }

            // Drop the piece
            HardDrop();
        }

        // Add method to toggle AI mode
        private void ToggleAI()
        {
            aiMode = !aiMode;
            if (aiMode)
            {
                aiTimer.Start();
                this.Text = "Tetris - AI Mode";
            }
            else
            {
                aiTimer.Stop();
                this.Text = "Tetris";
            }
        }

        private void PauseGame()
        {
            timer.Stop();
            aiTimer.Stop();
        }
        private void ResumeGame()
        {
            timer.Start();
            aiTimer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isPaused)
            {
                ResumeGame();
                isPaused = false;
                button1.Text = "";
            }
            else
            {
                PauseGame();
                isPaused = true;
                button1.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ToggleAI();
            if (aiMode == true)
            {
                button2.BackColor = Color.Green;
                button2.ForeColor = Color.White;
            }
            else
            {
                button2.BackColor = SystemColors.Control;
                button2.ForeColor = SystemColors.ControlText;
            }
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            aiTimer.Interval = (trackBar1.Maximum - trackBar1.Value) + 1;
        }
    }
}

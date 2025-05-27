using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Tetris
{
    public partial class LevelGame : Form
    {
        private TetrisGameEngine gameEngine;
        private DatabaseDataSetTableAdapters.TetrisUsersTableAdapter tetrisUsersTableAdapter = new DatabaseDataSetTableAdapters.TetrisUsersTableAdapter();
        private DatabaseDataSet.TetrisUsersRow user;
        private int scoreTarget;
        private int allowedMoves;

        public LevelGame(int UserID)
        {
            InitializeComponent();

            this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == UserID);

            this.KeyPreview = true;

            foreach (Control control in this.Controls)
            {
                if (control is Button button)
                {
                    button.TabStop = false;
                    button.GotFocus += (s, e) => { ((Button)s).Focus(); };
                }
            }

            InitializeGame();
        }

        private void InitializeGame()
        {
            gameEngine = new TetrisGameEngine();
            
            // Set up the UI controls
            gameEngine.MainCanvas = pictureBox1;
            gameEngine.NextShapeCanvas = pictureBox2;
            gameEngine.HoldCanvas = pictureBox3;

            // Generate level-specific canvas
            CanvasBlock[,] levelCanvas = LevelGenerator.GenerateLevel(300, 15, 20, out scoreTarget, out allowedMoves);
            
            // Debug output
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Console.Write(levelCanvas[i, j].IsFilled);
                    Console.Write(" ");
                }
                Console.WriteLine();
            }
            MessageBox.Show($"Score target: {scoreTarget}\nAllowed moves: {allowedMoves}");

            // Subscribe to events
            gameEngine.GameOver += OnGameOver;
            gameEngine.ScoreChanged += OnScoreChanged;
            gameEngine.LinesCleared += OnLinesCleared;
            gameEngine.LevelChanged += OnLevelChanged;

            // Initialize with pre-filled canvas and start the game
            gameEngine.Initialize(levelCanvas);
            gameEngine.Start();
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
                    gameEngine.HandleKeyInput(keyData);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.Focus();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            gameEngine.HandleKeyInput(e.KeyCode);
        }

        private void OnGameOver(object sender, GameEventArgs e)
        {
            if (e.Score > user.HighScore)
            {
                user.HighScore = e.Score;
                tetrisUsersTableAdapter.Update(user);
                MessageBox.Show($"Game Over! New high score: {e.Score}!");
            }
            else
            {
                MessageBox.Show($"Game Over! Your score was {e.Score}.");
            }
            this.Close();
        }

        private void OnScoreChanged(object sender, GameEventArgs e)
        {
            label1.Text = $"Score: {e.Score:N0}";
            
            // Check if level target is reached
            if (e.Score >= scoreTarget)
            {
                MessageBox.Show($"Level completed! Target score {scoreTarget} reached!");
                this.Close();
            }
        }

        private void OnLinesCleared(object sender, GameEventArgs e)
        {
            label3.Text = $"Lines cleared: {e.LinesCleared}";
        }

        private void OnLevelChanged(object sender, GameEventArgs e)
        {
            // Update level display if needed
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (gameEngine.IsPaused)
            {
                gameEngine.Resume();
                button1.Text = "Pause";
            }
            else
            {
                gameEngine.Pause();
                button1.Text = "Resume";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            gameEngine.ToggleAI();
            if (gameEngine.IsAIMode)
            {
                button2.BackColor = Color.Green;
                button2.ForeColor = Color.White;
                this.Text = "Tetris - AI Mode";
            }
            else
            {
                button2.BackColor = SystemColors.Control;
                button2.ForeColor = SystemColors.ControlText;
                this.Text = "Tetris";
            }
        }
    }
}

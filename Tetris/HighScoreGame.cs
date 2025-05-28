using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tetris
{
    public partial class HighScoreGame : Form
    {
        private TetrisGameEngine gameEngine;
        private DatabaseDataSetTableAdapters.TetrisUsersTableAdapter tetrisUsersTableAdapter = new DatabaseDataSetTableAdapters.TetrisUsersTableAdapter();
        private DatabaseDataSet.TetrisUsersRow user;
        private Label levelLabel;

        public HighScoreGame(int UserID)
        {
            InitializeComponent();

            this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == UserID);

            this.KeyPreview = true;

            // Wire up button events manually since Designer doesn't have them
            button1.Click += button1_Click;
            button2.Click += button2_Click;
            trackBar1.ValueChanged += trackBar1_ValueChanged;

            foreach (Control control in this.Controls)
            {
                if (control is Button button)
                {
                    button.TabStop = false;
                    button.GotFocus += (s, e) => { ((Button)s).Focus(); };
                }
            }

            // Add form closing event for confirmation dialog
            this.FormClosing += HighScoreGame_FormClosing;

            InitializeGame();
        }

        private void InitializeGame()
        {
            gameEngine = new TetrisGameEngine();
            
            // Set up the UI controls
            gameEngine.MainCanvas = pictureBox1;
            gameEngine.NextShapeCanvas = pictureBox2;
            gameEngine.HoldCanvas = pictureBox3;

            // Set window title
            this.Text = "Tetris - High Score Game";

            // Initialize AI speed control trackbar
            if (trackBar1 != null)
            {
                trackBar1.Minimum = 1;
                trackBar1.Maximum = 10;
                trackBar1.Value = 5; // Default medium AI speed
            }

            // Subscribe to events
            gameEngine.GameOver += OnGameOver;
            gameEngine.ScoreChanged += OnScoreChanged;
            gameEngine.LinesCleared += OnLinesCleared;

            // Show welcome message
            MessageBox.Show(
                $"Welcome to High Score Mode! 🎯\n\n" +
                $"Current High Score: {user.HighScore:N0}\n\n" +
                $"Tips:\n" +
                $"• Try to beat your personal best!\n" +
                $"• Use 'AI Mode' to watch and learn\n" +
                $"• Adjust AI speed with the trackbar\n" +
                $"• Use 'C' to hold pieces\n\n" +
                $"Good luck, {user.Username}!",
                "High Score Challenge",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            // Initialize and start the game
            gameEngine.Initialize();
            gameEngine.Start();
            
            // Set initial AI speed and update label
            if (trackBar1 != null)
            {
                gameEngine.SetAISpeed(trackBar1.Value);
                //aiSpeedLabel.Text = $"AI Speed: {trackBar1.Value} ({GetSpeedDescription(trackBar1.Value)})";
            }
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
            bool isNewHighScore = e.Score > user.HighScore;
            
            if (isNewHighScore)
            {
                user.HighScore = e.Score;
                tetrisUsersTableAdapter.Update(user);
                
                // Show congratulations message with options
                var result = MessageBox.Show(
                    $"🎉 CONGRATULATIONS! 🎉\n\n" +
                    $"New High Score: {e.Score:N0}\n" +
                    $"Previous Best: {(e.Score > 0 ? (e.Score - (e.Score - user.HighScore)).ToString("N0") : "0")}\n" +
                    $"Lines Cleared: {e.LinesCleared}\n" +
                    $"Level Reached: {e.Level}\n\n" +
                    $"Would you like to play again?",
                    "New High Score!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Exclamation
                );
                
                if (result == DialogResult.Yes)
                {
                    RestartGame();
                    return;
                }
            }
            else
            {
                var result = MessageBox.Show(
                    $"Game Over!\n\n" +
                    $"Your Score: {e.Score:N0}\n" +
                    $"High Score: {user.HighScore:N0}\n" +
                    $"Lines Cleared: {e.LinesCleared}\n" +
                    $"Level Reached: {e.Level}\n\n" +
                    $"Would you like to try again?",
                    "Game Over",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );
                
                if (result == DialogResult.Yes)
                {
                    RestartGame();
                    return;
                }
            }
            
            this.Close();
        }

        private void RestartGame()
        {
            // Stop current game
            if (gameEngine != null)
            {
                gameEngine.Stop();
            }
            
            // Reset UI
            label1.Text = "Score: 0";
            label3.Text = "Lines cleared: 0";
            levelLabel.Text = "Level: 1";
            
            button2.BackColor = SystemColors.Control;
            button2.ForeColor = SystemColors.ControlText;
            this.Text = "Tetris - High Score Game";
            
            // Initialize new game
            InitializeGame();
        }

        private void OnScoreChanged(object sender, GameEventArgs e)
        {
            label1.Text = $"Score: {e.Score:N0}";
        }

        private void OnLinesCleared(object sender, GameEventArgs e)
        {
            label3.Text = $"Lines cleared: {e.LinesCleared}";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (gameEngine.IsPaused)
            {
                gameEngine.Resume();
                button1.Text = "";
            }
            else
            {
                gameEngine.Pause();
                button1.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            gameEngine.ToggleAI();
            if (gameEngine.IsAIMode)
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
            if (gameEngine != null)
            {
                // TrackBar controls AI speed (1-10, where 1 is slowest and 10 is fastest)
                gameEngine.SetAISpeed(trackBar1.Value);
            }
        }

        private void HighScoreGame_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Don't show confirmation if game is already over (form closing from game over dialog)
            if (gameEngine != null && gameEngine.IsGameOver)
            {
                return;
            }

            // Show confirmation dialog for manual close attempts
            var result = MessageBox.Show(
                "Are you sure you want to quit the high score game?\n\nYour current progress will be lost, but any new high score achieved will be saved.",
                "Confirm Quit",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2 // Default to "No"
            );

            // If user clicks "No", cancel the close operation
            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            // If user clicks "Yes", proceed with closing and cleanup
            if (gameEngine != null)
            {
                gameEngine.Stop();
            }
        }
    }
}

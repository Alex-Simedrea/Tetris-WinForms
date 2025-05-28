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
        private int currentMoves = 0;
        private LevelGenerator.LevelTargetType targetType;
        private int linesTarget;
        private bool gameEnded = false; // Flag to prevent message spam
        private int currentLevel; // Track current level being played
        private Timer completionDelayTimer; // Timer to delay completion handling
        private Label currentScoreLabel; // Label for current score display

        // Powerup UI elements
        private Button aiPowerupButton; // button2
        private Button lineClearPowerupButton; // button3
        private Button quitButton; // button4

        public LevelGame(int UserID) : this(UserID, -1) // -1 means use user's current level
        {
        }

        public LevelGame(int UserID, int levelIndex)
        {
            InitializeComponent();

            // Lock down form size programmatically to prevent any resizing
            this.Size = new Size(1242 * 1/2, 1350 * 1/2);
            this.MinimumSize = new Size(1242 * 1/2, 1350 * 1/2);
            this.MaximumSize = new Size(1242 * 1/2, 1350 * 1/2);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == UserID);

            // Determine which level to play
            if (levelIndex == -1)
            {
                // Use user's current level (1-based, so if user.Level is 0, start at level 1)
                currentLevel = Math.Max(1, user.Level + 1);
            }
            else
            {
                currentLevel = levelIndex;
            }

            // Initialize completion delay timer
            completionDelayTimer = new Timer();
            completionDelayTimer.Interval = 1000; // 1 second delay to allow final animations
            completionDelayTimer.Tick += CompletionDelayTimer_Tick;

            // Clean up timer when form closes
            this.FormClosed += (s, e) => {
                completionDelayTimer?.Stop();
                completionDelayTimer?.Dispose();
            };

            // Add form closing event for confirmation dialog
            this.FormClosing += LevelGame_FormClosing;

            // Wire up button events
            button1.Click += button1_Click;
            
            // Create current score label
            currentScoreLabel = new Label();
            currentScoreLabel.AutoSize = true;
            currentScoreLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            currentScoreLabel.Location = new System.Drawing.Point(82, 120); // Position it near the level label
            currentScoreLabel.Name = "currentScoreLabel";
            currentScoreLabel.Text = "Score: 0";
            this.Controls.Add(currentScoreLabel);

            this.KeyPreview = true;

            foreach (Control control in this.Controls)
            {
                if (control is Button button)
                {
                    button.TabStop = false;
                    button.GotFocus += (s, e) => { ((Button)s).Focus(); };
                }
            }

            // Initialize powerup buttons
            aiPowerupButton = button2;
            lineClearPowerupButton = button3;
            
            // Wire up powerup button events
            aiPowerupButton.Click += AiPowerupButton_Click;
            lineClearPowerupButton.Click += LineClearPowerupButton_Click;

            // Initialize quit button
            quitButton = button4;
            quitButton.Click += QuitButton_Click;

            InitializeGame(currentLevel);
        }

        private void InitializeGame() => InitializeGame(1);

        private void InitializeGame(int levelIndex)
        {
            gameEngine = new TetrisGameEngine();
            
            // Set up the UI controls
            gameEngine.MainCanvas = pictureBox1;
            gameEngine.NextShapeCanvas = pictureBox2;
            gameEngine.HoldCanvas = pictureBox3;

            // Generate level-specific canvas with new target system
            CanvasBlock[,] levelCanvas = LevelGenerator.GenerateLevel(levelIndex, 15, 20, out scoreTarget, out allowedMoves, out targetType, out linesTarget);
            
            // Display appropriate target information
            string targetInfo = targetType == LevelGenerator.LevelTargetType.Score 
                ? $"Score target: {scoreTarget:N0}" 
                : $"Lines to clear: {linesTarget}";
            
            MessageBox.Show($"Level {levelIndex}\n{targetInfo}\nAllowed moves: {allowedMoves}");

            // Subscribe to events
            gameEngine.GameOver += OnGameOver;
            gameEngine.ScoreChanged += OnScoreChanged;
            gameEngine.LinesCleared += OnLinesCleared;
            gameEngine.LevelChanged += OnLevelChanged;
            gameEngine.ShapePlaced += OnShapePlaced;
            gameEngine.PowerupUsed += OnPowerupUsed;

            // Initialize display labels
            UpdateAllDisplays();
            
            // Initialize powerup buttons
            UpdatePowerupButtons();

            // Initialize with pre-filled canvas and start the game
            gameEngine.Initialize(levelCanvas);
            gameEngine.Start();
        }

        private void UpdateAllDisplays()
        {
            // label2 = Level display
            label2.Text = $"Level {currentLevel}";
            
            // label3 = Moves left
            int movesLeft = Math.Max(0, allowedMoves - currentMoves);
            label3.Text = $"Moves Left: {movesLeft}";
            
            // label1 = Target remaining (this will be updated in score/lines events)
            UpdateTargetDisplay();
            
            // currentScoreLabel = Current score
            if (currentScoreLabel != null)
            {
                currentScoreLabel.Text = $"Score: {gameEngine?.Score ?? 0:N0}";
            }
            
            // Update powerup buttons
            if (aiPowerupButton != null && lineClearPowerupButton != null)
            {
                UpdatePowerupButtons();
            }
        }

        private void UpdateTargetDisplay()
        {
            if (targetType == LevelGenerator.LevelTargetType.Score)
            {
                int scoreRemaining = Math.Max(0, scoreTarget - (gameEngine?.Score ?? 0));
                label1.Text = $"Score needed: {scoreRemaining:N0}";
            }
            else
            {
                int linesRemaining = Math.Max(0, linesTarget - (gameEngine?.LinesClearedCount ?? 0));
                label1.Text = $"Lines needed: {linesRemaining}";
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
            if (gameEnded) return; // Prevent message spam
            gameEnded = true;
            
            gameEngine.Stop(); // Stop the game immediately
            
            if (e.Score > user.HighScore)
            {
                user.HighScore = e.Score;
                tetrisUsersTableAdapter.Update(user);
                MessageBox.Show($"Game Over! New high score: {e.Score}!\n\nLevel {currentLevel} failed - pieces reached the top.");
            }
            else
            {
                MessageBox.Show($"Game Over! Your score was {e.Score}.\n\nLevel {currentLevel} failed - pieces reached the top.");
            }
            this.Close();
        }

        private void OnScoreChanged(object sender, GameEventArgs e)
        {
            if (gameEnded) return; // Prevent message spam
            
            // Update target display to show remaining
            UpdateTargetDisplay();
            
            // Update current score display
            if (currentScoreLabel != null)
            {
                currentScoreLabel.Text = $"Score: {e.Score:N0}";
            }
            
            // Update window title to show current score
            this.Text = $"Tetris Level Game - Score: {e.Score:N0}";
            
            // Check if level target is reached based on target type
            bool targetReached = false;
            
            if (targetType == LevelGenerator.LevelTargetType.Score && e.Score >= scoreTarget)
            {
                targetReached = true;
            }
            
            if (targetReached)
            {
                gameEnded = true;
                // Start delay timer to allow final animations to complete
                completionDelayTimer.Start();
            }
        }

        private int CalculateGoldReward()
        {
            // Base reward increases with level
            int baseReward = 10 + (currentLevel * 5);
            
            // Bonus for efficiency (fewer moves used)
            double moveEfficiency = 1.0 - ((double)currentMoves / allowedMoves);
            int efficiencyBonus = (int)(baseReward * moveEfficiency * 0.5);
            
            // Bonus for target type (lines cleared is harder)
            int targetTypeBonus = targetType == LevelGenerator.LevelTargetType.LinesCleared ? 10 : 0;
            
            // Level milestone bonuses
            int milestoneBonus = 0;
            if (currentLevel % 10 == 0) milestoneBonus = 50; // Every 10th level
            else if (currentLevel % 5 == 0) milestoneBonus = 20; // Every 5th level
            
            int totalReward = baseReward + efficiencyBonus + targetTypeBonus + milestoneBonus;
            return Math.Max(5, totalReward); // Minimum 5 gold
        }

        private void OnLinesCleared(object sender, GameEventArgs e)
        {
            if (gameEnded) return; // Prevent message spam
            
            // Update target display to show remaining
            UpdateTargetDisplay();
            
            // Check if lines cleared target is reached
            if (targetType == LevelGenerator.LevelTargetType.LinesCleared && e.LinesCleared >= linesTarget)
            {
                gameEnded = true;
                // Start delay timer to allow final animations to complete
                completionDelayTimer.Start();
            }
        }

        private void OnLevelChanged(object sender, GameEventArgs e)
        {
            // Update level display if needed
        }

        private void OnShapePlaced(object sender, GameEventArgs e)
        {
            if (gameEnded) return; // Prevent message spam
            
            currentMoves++;
            
            // Update all displays
            UpdateAllDisplays();
            
            // Check if moves limit is reached
            if (currentMoves >= allowedMoves)
            {
                gameEnded = true;
                gameEngine.Stop(); // Stop the game immediately
                
                string failureMessage = targetType == LevelGenerator.LevelTargetType.Score 
                    ? $"Level {currentLevel} failed! You used all {allowedMoves} moves without reaching the target score of {scoreTarget:N0}."
                    : $"Level {currentLevel} failed! You used all {allowedMoves} moves without clearing {linesTarget} lines.";
                
                MessageBox.Show(failureMessage);
                this.Close();
            }
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

        private void StartNextLevel()
        {
            // Reset game state
            gameEnded = false;
            currentMoves = 0;
            currentLevel++;
            
            // Generate new level
            CanvasBlock[,] levelCanvas = LevelGenerator.GenerateLevel(currentLevel, 15, 20, out scoreTarget, out allowedMoves, out targetType, out linesTarget);
            
            // Display new level information
            string targetInfo = targetType == LevelGenerator.LevelTargetType.Score 
                ? $"Score target: {scoreTarget:N0}" 
                : $"Lines to clear: {linesTarget}";
            
            MessageBox.Show($"Level {currentLevel}\n{targetInfo}\nAllowed moves: {allowedMoves}");
            
            // Update all displays for new level
            UpdateAllDisplays();
            
            // Re-subscribe to powerup events for new level
            gameEngine.PowerupUsed += OnPowerupUsed;
            
            // Initialize and start new level
            gameEngine.Initialize(levelCanvas);
            gameEngine.Start();
        }

        private void CompletionDelayTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer
            completionDelayTimer.Stop();
            
            // Now stop the game engine
            gameEngine.Stop();
            
            // Calculate gold reward based on level and performance
            int goldReward = CalculateGoldReward();
            
            // Update user's level and gold
            if (currentLevel > user.Level)
            {
                user.Level = currentLevel;
            }
            user.Gold += goldReward;
            
            // Update high score if applicable
            if (gameEngine.Score > user.HighScore)
            {
                user.HighScore = gameEngine.Score;
            }
            
            tetrisUsersTableAdapter.Update(user);
            
            // Determine completion message based on target type
            string completionMessage = targetType == LevelGenerator.LevelTargetType.Score 
                ? $"Level {currentLevel} completed! Target score {scoreTarget:N0} reached!"
                : $"Level {currentLevel} completed! Target of {linesTarget} lines cleared!";
            
            // Show completion message and ask about next level
            MessageBox.Show($"{completionMessage}\n\nReward: {goldReward} gold!\nTotal gold: {user.Gold}");
            
            // Ask if player wants to continue to next level
            var result = MessageBox.Show($"Would you like to continue to Level {currentLevel + 1}?", 
                "Level Complete!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // Start next level
                StartNextLevel();
            }
            else
            {
                this.Close();
            }
        }

        private void AiPowerupButton_Click(object sender, EventArgs e)
        {
            if (gameEnded || gameEngine.IsPaused) return;
            
            // Check if user has AI powerups available
            if (user.AIPowerUps > 0)
            {
                // Use the powerup
                gameEngine.UseAIPowerup();
                
                // Decrease powerup count in database
                user.AIPowerUps--;
                tetrisUsersTableAdapter.Update(user);
                
                // Update UI
                UpdatePowerupButtons();
            }
            else
            {
                MessageBox.Show("No AI powerups available!");
            }
        }

        private void LineClearPowerupButton_Click(object sender, EventArgs e)
        {
            if (gameEnded || gameEngine.IsPaused) return;
            
            // Check if user has line clear powerups available
            if (user.ClearRowPowerUps > 0)
            {
                // Use the powerup
                gameEngine.UseClearLinePowerup();
                
                // Decrease powerup count in database
                user.ClearRowPowerUps--;
                tetrisUsersTableAdapter.Update(user);
                
                // Update UI
                UpdatePowerupButtons();
            }
            else
            {
                MessageBox.Show("No line clear powerups available!");
            }
        }

        private void UpdatePowerupButtons()
        {
            // Update AI powerup button text
            aiPowerupButton.Text = $"AI Powerup - {user.AIPowerUps} Left";
            aiPowerupButton.Enabled = user.AIPowerUps > 0 && !gameEnded && !gameEngine.IsPaused;
            
            // Update line clear powerup button text
            lineClearPowerupButton.Text = $"Line Clear - {user.ClearRowPowerUps} Left";
            lineClearPowerupButton.Enabled = user.ClearRowPowerUps > 0 && !gameEnded && !gameEngine.IsPaused;
            
            // Visual feedback for AI powerup when active
            if (gameEngine.IsAIPowerupActive)
            {
                aiPowerupButton.BackColor = Color.Orange;
                aiPowerupButton.ForeColor = Color.White;
                aiPowerupButton.Text = "AI Active - " + user.AIPowerUps + " Left";
            }
            else
            {
                aiPowerupButton.BackColor = SystemColors.Control;
                aiPowerupButton.ForeColor = SystemColors.ControlText;
            }
        }

        private void OnPowerupUsed(object sender, GameEventArgs e)
        {
            UpdatePowerupButtons();
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LevelGame_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Show confirmation dialog before closing
            var result = MessageBox.Show(
                "Are you sure you want to quit the level? Your progress will be lost.",
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

            // If user clicks "Yes", proceed with closing
            // Stop the game engine and clean up
            if (gameEngine != null)
            {
                gameEngine.Stop();
            }

            // Stop and dispose of timers
            if (completionDelayTimer != null)
            {
                completionDelayTimer.Stop();
                completionDelayTimer.Dispose();
            }
        }
    }
}

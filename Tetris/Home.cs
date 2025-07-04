﻿using System;
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
    public partial class Home : Form
    {
        DatabaseDataSetTableAdapters.TetrisUsersTableAdapter tetrisUsersTableAdapter = new DatabaseDataSetTableAdapters.TetrisUsersTableAdapter();
        DatabaseDataSet.TetrisUsersRow user;

        const int AI_POWER_UP_PRICE = 30;
        const int CLEAR_ROW_POWER_UP_PRICE = 20;

        public Home(int id)
        {
            InitializeComponent();
            this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == id);

            // Use RefreshUserDisplay for consistent formatting
            RefreshUserDisplay();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (user.Gold >= AI_POWER_UP_PRICE)
            {
                user.Gold -= AI_POWER_UP_PRICE;
                user.AIPowerUps++;
                tetrisUsersTableAdapter.Update(user);
                UpdateShopTexts();
            }
            else
            {
                MessageBox.Show("Not enough gold");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (user.Gold >= CLEAR_ROW_POWER_UP_PRICE)
            {
                user.Gold -= CLEAR_ROW_POWER_UP_PRICE;
                user.ClearRowPowerUps++;
                tetrisUsersTableAdapter.Update(user);
                UpdateShopTexts();
            }
            else
            {
                MessageBox.Show("Not enough gold");
            }
        }

        private void UpdateShopTexts()
        {
            RefreshUserDisplay();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var highScoreGame = new HighScoreGame(user.Id);
            highScoreGame.Show();
            this.Hide();
            highScoreGame.FormClosed += (s, args) => {
                // Refresh user data when returning from high score game
                this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == user.Id);
                RefreshUserDisplay();
                this.Show();
            };
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var levelGame = new LevelGame(user.Id);
            levelGame.Show();
            this.Hide();
            levelGame.FormClosed += (s, args) => {
                // Refresh user data when returning from level game
                this.user = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Id == user.Id);
                RefreshUserDisplay();
                this.Show();
            };
        }

        private void RefreshUserDisplay()
        {
            this.label12.Text = $"Hello, {user.Username}!";
            this.label2.Text = $"{user.Gold} gold";
            this.label4.Text = $"{user.AIPowerUps} remaining";
            this.label5.Text = $"{user.ClearRowPowerUps} remaining";
            
            // Show next level to play (user.Level + 1, or 1 if they haven't completed any)
            int nextLevel = Math.Max(1, user.Level + 1);
            this.label9.Text = $"Next Level: {nextLevel}";
            
            this.label10.Text = $"{user.HighScore} points";
        }
    }
}

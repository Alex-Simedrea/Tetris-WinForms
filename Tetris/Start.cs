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
    public partial class Start : Form
    {
        DatabaseDataSetTableAdapters.TetrisUsersTableAdapter tetrisUsersTableAdapter = new DatabaseDataSetTableAdapters.TetrisUsersTableAdapter();
        public Start()
        {
            InitializeComponent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var signUp = new SignUp();
            signUp.Show();
            this.Hide();
            signUp.FormClosed += (s, args) => this.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text;
            var password = textBox2.Text;

            var users = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Username == username && x.Password == password);
            if (users != null)
            {
                MessageBox.Show("Login successful");
            }
            else
            {
                MessageBox.Show("Invalid username or password");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var game = new Game();
            game.Show();
            this.Hide();
            game.FormClosed += (s, args) => this.Show();
        }
    }
}

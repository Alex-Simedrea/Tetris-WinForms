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

            var err = Validators.ValidateLogin(username, password);
            if (err != null)
            {
                MessageBox.Show(err);
                return;
            }

            button2.Text = "Loading...";
            button2.Enabled = false;

            var users = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Username == username && PasswordHasher.VerifyPassword(password, x.Password));
            if (users == null)
            {
                MessageBox.Show(Validators.BAD_LOGIN_ERROR);

                // Reset button state when login fails
                button2.Text = "Login";
                button2.Enabled = true;

                textBox1.Text = "";
                textBox2.Text = "";
                return;
            }

            var home = new Home(users.Id);
            home.Show();
            this.Hide();
            home.FormClosed += (s, args) =>
            {
                // Reset button state when returning from home
                button2.Text = "Login";
                button2.Enabled = true;
                this.Show();
            };

            textBox1.Text = "";
            textBox2.Text = "";
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

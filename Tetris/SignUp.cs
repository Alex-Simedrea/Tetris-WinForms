using System;
using System.Linq;
using System.Windows.Forms;

namespace Tetris
{
    public partial class SignUp : Form
    {
        DatabaseDataSetTableAdapters.TetrisUsersTableAdapter tetrisUsersTableAdapter = new DatabaseDataSetTableAdapters.TetrisUsersTableAdapter();
        public SignUp()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text;
            var password = textBox2.Text;
            var repeatPassword = textBox3.Text;

            var err = Validators.ValidateSignUp(username, password, repeatPassword);
            if (err != null)
            {
                MessageBox.Show(err);
                return;
            }

            button1.Text = "Loading...";
            button1.Enabled = false;

            var users = tetrisUsersTableAdapter.GetData().FirstOrDefault(x => x.Username == username);

            if (users != null)
            {
                MessageBox.Show(Validators.USERNAME_TAKEN_ERROR);
                return;
            }

            tetrisUsersTableAdapter.InsertWithCredentials(username, PasswordHasher.HashPassword(password));

            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tetris
{
    public static class Validators
    {
        public static string USERNAME_ERROR = "Username must be between 3 and 20 characters long and can only contain letters and digits.";
        public static string PASSWORD_ERROR = "Password must be between 8 and 20 characters long and must contain at least one letter and one digit.";
        public static string USERNAME_TAKEN_ERROR = "Username already exists.";
        public static string PASSWORD_MISMATCH_ERROR = "Passwords do not match.";
        public static string BAD_LOGIN_ERROR = "Invalid username or password.";

        public static bool ValidateUsername(string username)
        {
            return !string.IsNullOrEmpty(username) && username.All(char.IsLetterOrDigit) && username.Length >= 3 && username.Length <= 20;
        }
        public static bool ValidatePassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= 8 && password.Length <= 20 && password.Any(char.IsDigit) && password.Any(char.IsLetter);
        }

        public static string ValidateLogin(string username, string password)
        {
            if (!ValidateUsername(username))
            {
                return USERNAME_ERROR;
            }
            if (!ValidatePassword(password))
            {
                return PASSWORD_ERROR;
            }
            return null;
        }

        public static string ValidateSignUp(string username, string password, string repeatPassword)
        {
            var error = ValidateLogin(username, password);
            if (error != null)
            {
                return error;
            }
            if (password != repeatPassword)
            {
                return PASSWORD_MISMATCH_ERROR;
            }
            return null;
        }
    }
}

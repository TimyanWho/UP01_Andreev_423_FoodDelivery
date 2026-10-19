using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace UP01_Andreev_423_FoodDelivery
{
    public static class Helpers
    {
        private const int PasswordIterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            byte[] salt = new byte[SaltSize];
            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(salt);

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PasswordIterations))
                hash = pbkdf2.GetBytes(HashSize);

            return "pbkdf2$" + PasswordIterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                string[] parts = storedHash.Split('$');
                if (parts.Length != 4 || parts[0] != "pbkdf2") return false;
                int iterations;
                if (!int.TryParse(parts[1], out iterations) || iterations < 10000 || iterations > 1000000)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                    actual = pbkdf2.GetBytes(expected.Length);

                if (actual.Length != expected.Length) return false;
                int difference = 0;
                for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
                return difference == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPassword(string password, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                error = "Пароль должен содержать не менее 6 символов.";
                return false;
            }

            bool hasDigit = false;
            foreach (char ch in password)
            {
                if (ch >= '0' && ch <= '9') hasDigit = true;
                else if (!((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')))
                {
                    error = "Используйте английские буквы и цифры без пробелов и специальных символов.";
                    return false;
                }
            }

            if (!hasDigit)
            {
                error = "Пароль должен содержать хотя бы одну цифру.";
                return false;
            }
            return true;
        }

        public static bool IsValidFIO(string fio)
        {
            if (string.IsNullOrWhiteSpace(fio)) return false;
            return fio.Trim().Length <= 150 && fio.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length >= 2
                && fio.All(ch => char.IsLetter(ch) || ch == ' ' || ch == '-');
        }

        public static bool IsValidLogin(string login)
        {
            return !string.IsNullOrWhiteSpace(login) && login.Length <= 50
                && Regex.IsMatch(login, "^[A-Za-z0-9_-]+$");
        }

        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            string digits = new string(phone.Where(char.IsDigit).ToArray());
            return digits.Length >= 7 && digits.Length <= 15 && phone.Length <= 20
                && phone.All(ch => char.IsDigit(ch) || ch == '+' || ch == '-' || ch == '(' || ch == ')' || ch == ' ');
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            try { return new System.Net.Mail.MailAddress(email).Address == email.Trim() && email.Length <= 150; }
            catch { return false; }
        }

        public static string GenerateCaptcha()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            byte[] data = new byte[6];
            using (var generator = RandomNumberGenerator.Create()) generator.GetBytes(data);
            var result = new StringBuilder();
            for (int i = 0; i < data.Length; i++) result.Append(chars[data[i] % chars.Length]);
            return result.ToString();
        }
    }
}

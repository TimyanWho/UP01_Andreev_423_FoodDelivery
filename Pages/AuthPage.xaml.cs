using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class AuthPage : Page
    {
        private bool _captchaPassed;

        public AuthPage()
        {
            InitializeComponent();

            if (AppSession.FailedLoginAttempts >= 3)
            {
                CaptchaPanel.Visibility = Visibility.Visible;
                if (string.IsNullOrWhiteSpace(AppSession.CaptchaCode))
                    AppSession.CaptchaCode = Helpers.GenerateCaptcha();
                CaptchaText.Text = AppSession.CaptchaCode;
            }
            else
            {
                CaptchaPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ButtonEnter_Click(object sender, RoutedEventArgs e)
        {
            string login = TextBoxLogin.Text.Trim();
            string password = PasswordBox.Password;

            if (login.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Введите логин и пароль.", "Проверка данных",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool captchaRequired = AppSession.FailedLoginAttempts >= 3;
            if (captchaRequired && !_captchaPassed)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(CaptchaInput.Text)
                        ? "После трёх неверных попыток введите код капчи."
                        : "Код капчи введён неверно. Попробуйте новый код.",
                    "Проверка капчи", MessageBoxButton.OK, MessageBoxImage.Warning);
                CaptchaPanel.Visibility = Visibility.Visible;
                CaptchaChange();
                return;
            }

            _captchaPassed = false;

            try
            {
                users user = Core.Context.users.FirstOrDefault(x => x.login == login && x.is_active);

                if (user == null || !Helpers.VerifyPassword(password, user.password_hash))
                {
                    AppSession.FailedLoginAttempts++;
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка авторизации",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    if (AppSession.FailedLoginAttempts >= 3)
                    {
                        CaptchaPanel.Visibility = Visibility.Visible;
                        CaptchaChange();
                    }
                    return;
                }

                roles role = Core.Context.roles.FirstOrDefault(x => x.id == user.role_id);
                string roleName = role == null ? string.Empty : role.name;
                if (roleName != "Admin" && roleName != "User" && roleName != "Courier")
                {
                    if (captchaRequired)
                    {
                        CaptchaPanel.Visibility = Visibility.Visible;
                        CaptchaChange();
                    }
                    MessageBox.Show("Для входа через приложение доступны роли User, Admin и Courier.",
                        "Роль не поддерживается", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                AppSession.ResetLoginSecurity();
                CaptchaPanel.Visibility = Visibility.Collapsed;
                CaptchaInput.Clear();
                AppSession.CurrentUserId = user.id;

                Page destination;
                if (roleName == "Admin") destination = new AdminPage();
                else if (roleName == "Courier") destination = new CourierPage();
                else destination = new UserPage();

                if (NavigationService != null)
                {
                    AppSession.ClearNavigationHistoryOnNextPage = true;
                    NavigationService.Navigate(destination);
                }
                else
                {
                    AppSession.ClearNavigationHistoryOnNextPage = false;
                }
            }
            catch (Exception ex)
            {

                if (captchaRequired)
                {
                    CaptchaPanel.Visibility = Visibility.Visible;
                    CaptchaChange();
                }
                MessageBox.Show("Не удалось получить данные. Проверь подключение EDMX к SQL Server и строку подключения, созданную Visual Studio.\n\n" + ex.Message,
                    "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegPage());
        }

        private void ButtonChangePassword_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChangePassPage());
        }

        private void CaptchaChange()
        {
            AppSession.CaptchaCode = Helpers.GenerateCaptcha();
            CaptchaText.Text = AppSession.CaptchaCode;
            CaptchaInput.Clear();
            _captchaPassed = false;
        }

        private void CaptchaRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (AppSession.FailedLoginAttempts >= 3)
                CaptchaChange();
        }

        private void CaptchaInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _captchaPassed = AppSession.FailedLoginAttempts >= 3
                && !string.IsNullOrEmpty(AppSession.CaptchaCode)
                && string.Equals(CaptchaInput.Text.Trim(), AppSession.CaptchaCode,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void textBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy || e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
                e.Handled = true;
        }

        private void TextBoxLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LoginHint != null)
                LoginHint.Visibility = string.IsNullOrEmpty(TextBoxLogin.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordHint != null)
                PasswordHint.Visibility = string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

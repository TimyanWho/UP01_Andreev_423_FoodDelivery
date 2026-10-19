using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class ChangePassPage : Page
    {
        public ChangePassPage()
        {
            InitializeComponent();
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string currentPassword = CurrentPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;

            if (login.Length == 0 || currentPassword.Length == 0 || newPassword.Length == 0 || ConfirmPasswordBox.Password.Length == 0)
            {
                MessageBox.Show("Заполните все поля.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string error;
            if (!Helpers.IsValidPassword(newPassword, out error)) { MessageBox.Show(error); return; }
            if (newPassword != ConfirmPasswordBox.Password) { MessageBox.Show("Новый пароль и подтверждение не совпадают."); return; }
            if (currentPassword == newPassword) { MessageBox.Show("Новый пароль должен отличаться от текущего."); return; }

            try
            {
                users user = Core.Context.users.FirstOrDefault(x => x.login == login && x.is_active);
                if (user == null || !Helpers.VerifyPassword(currentPassword, user.password_hash))
                {
                    MessageBox.Show("Логин или текущий пароль неверен.");
                    return;
                }

                user.password_hash = Helpers.HashPassword(newPassword);
                Core.Context.SaveChanges();

                MessageBox.Show("Пароль успешно изменён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.Navigate(new AuthPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось изменить пароль.\n\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}

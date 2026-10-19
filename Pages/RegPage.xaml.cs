using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class RegPage : Page
    {
        public RegPage()
        {
            InitializeComponent();
        }

        private void regButton_Click(object sender, RoutedEventArgs e)
        {
            string login = txtbxLog.Text.Trim();
            string fio = txtbxFIO.Text.Trim();
            string phone = txtbxPhone.Text.Trim();
            string email = txtbxEmail.Text.Trim();
            string password = passBxFrst.Password;

            if (login.Length == 0 || fio.Length == 0 || phone.Length == 0 ||
                password.Length == 0 || passBxScnd.Password.Length == 0)
            {
                MessageBox.Show("Заполните логин, ФИО, телефон и оба поля пароля.",
                    "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Helpers.IsValidLogin(login)) { MessageBox.Show("Логин: английские буквы, цифры, _ или -, до 50 символов."); return; }
            if (!Helpers.IsValidFIO(fio)) { MessageBox.Show("Введите ФИО: минимум два слова, только буквы, пробелы и дефисы."); return; }
            if (!Helpers.IsValidPhone(phone)) { MessageBox.Show("Укажите корректный номер телефона."); return; }
            if (!Helpers.IsValidEmail(email)) { MessageBox.Show("Укажите корректный адрес электронной почты."); return; }

            string passwordError;
            if (!Helpers.IsValidPassword(password, out passwordError)) { MessageBox.Show(passwordError); return; }
            if (password != passBxScnd.Password) { MessageBox.Show("Пароли не совпадают."); return; }

            try
            {
                
                if (Core.Context.users.Any(x => x.login == login))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.");
                    return;
                }
                if (Core.Context.users.Any(x => x.phone == phone))
                {
                    MessageBox.Show("Пользователь с таким телефоном уже существует.");
                    return;
                }
                if (email.Length > 0 && Core.Context.users.Any(x => x.email == email))
                {
                    MessageBox.Show("Пользователь с такой электронной почтой уже существует.");
                    return;
                }

                roles role = Core.Context.roles.FirstOrDefault(x => x.name == "User");
                if (role == null)
                {
                    MessageBox.Show("В таблице roles отсутствует роль User. Выполни db/schema.sql.", "Ошибка БД");
                    return;
                }

                users user = new users
                {
                    role_id = role.id,
                    login = login,
                    full_name = fio,
                    phone = phone,
                    email = email.Length == 0 ? null : email,
                    password_hash = Helpers.HashPassword(password),
                    created_at = DateTime.Now,
                    is_active = true
                };

                Core.Context.users.Add(user);
                Core.Context.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно. Теперь можно войти.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.Navigate(new AuthPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось выполнить регистрацию. Проверьте EDMX и ограничения БД.\n\n" + ex.Message,
                    "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AuthPage());
        }
    }
}

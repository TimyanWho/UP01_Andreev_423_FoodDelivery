using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Andreev_423_FoodDelivery
{
    public partial class MainWindow : Window
    {
        private readonly string themeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UP01_Andreev_423_FoodDelivery", "theme.txt");

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(LoadSavedTheme());
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (o, t) => DateTimeNow.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            timer.Start();
            MainFrame.Navigate(new Pages.AuthPage());
        }

        private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var page = e.Content as Page;
            if (page == null) return;

            if (page is Pages.AuthPage)
            {
                if (AppSession.CurrentUserId.HasValue)
                {
                    AppSession.CurrentUserId = null;
                    AppSession.ResetLoginSecurity();
                }

                AppSession.ClearNavigationHistoryOnNextPage = false;
                ClearBackNavigationHistory();
            }
            else if (AppSession.ClearNavigationHistoryOnNextPage)
            {

                ClearBackNavigationHistory();
                AppSession.ClearNavigationHistoryOnNextPage = false;
            }

            PageTitle.Text = GetPageTitle(page);
            bool canNavigateBack = MainFrame.CanGoBack && !(page is Pages.AuthPage);
            BackButton.Visibility = canNavigateBack ? Visibility.Visible : Visibility.Collapsed;
            BackButton.IsEnabled = canNavigateBack;
            LogoutButtonVisibility();

            var user = AppSession.CurrentUserId.HasValue
                ? Core.Context.users.FirstOrDefault(x => x.id == AppSession.CurrentUserId.Value)
                : null;
            string role = string.Empty;
            if (user != null)
            {
                var userRole = Core.Context.roles.FirstOrDefault(x => x.id == user.role_id);
                if (userRole != null) role = userRole.name;
            }

            CurrentUserText.Text = user == null ? string.Empty : user.full_name + " (" + role + ")";
        }

        private void ClearBackNavigationHistory()
        {
            var navigationService = MainFrame.NavigationService;
            if (navigationService == null) return;

            while (navigationService.RemoveBackEntry() != null)
            {
                
            }
        }

        private void LogoutButtonVisibility()
        {
            LogoutButton.Visibility = !AppSession.CurrentUserId.HasValue ? Visibility.Collapsed : Visibility.Visible;
        }

        private string GetPageTitle(Page page)
        {
            if (page is Pages.AuthPage) return "Авторизация";
            if (page is Pages.RegPage) return "Регистрация";
            if (page is Pages.ChangePassPage) return "Смена пароля";
            if (page is Pages.UserPage) return "Кабинет пользователя";
            if (page is Pages.CourierPage) return "Кабинет курьера";
            if (page is Pages.AdminPage) return "Панель администратора";
            if (page is Pages.DiagrammPage) return "Диаграммы и отчёты";
            return "Сервис доставки еды";
        }

        private string LoadSavedTheme()
        {
            try
            {
                if (File.Exists(themeFilePath))
                {
                    string theme = File.ReadAllText(themeFilePath).Trim();
                    if (theme == "Dark" || theme == "Light") return theme;
                }
            }
            catch { }
            return "Light";
        }

        private void SaveTheme(string theme)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(themeFilePath));
                File.WriteAllText(themeFilePath, theme);
            }
            catch { }
        }

        public void ApplyTheme(string theme)
        {
            string dictionaryName = theme == "Dark" ? "DarkTheme.xaml" : "Dictionary.xaml";
            var uri = new Uri(dictionaryName, UriKind.Relative);
            var resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            if (dictionaries.Count == 0)
            {
                dictionaries.Add(resourceDict);
            }
            else
            {
                dictionaries[0] = resourceDict;
                while (dictionaries.Count > 1)
                    dictionaries.RemoveAt(dictionaries.Count - 1);
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            string next = LoadSavedTheme() == "Light" ? "Dark" : "Light";
            ApplyTheme(next);
            SaveTheme(next);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack) MainFrame.GoBack();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AppSession.CurrentUserId.HasValue) return;
            if (MessageBox.Show("Вы действительно хотите выйти из учётной записи?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            AppSession.CurrentUserId = null;
            AppSession.ResetLoginSecurity();
            AppSession.ClearNavigationHistoryOnNextPage = false;
            MainFrame.Navigate(new Pages.AuthPage());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите выйти из приложения?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                e.Cancel = true;
            else
                Core.Context.Dispose();
        }
    }
}

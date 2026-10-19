using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class AdminPage : Page
    {
        private List<object> _allRows = new List<object>();
        private List<PropertyInfo> _properties = new List<PropertyInfo>();
        private string _loadedTableKey;
        private bool _loadingTable;

        public string CurrentViewTitle
        {
            get
            {
                var selected = TableSelector.SelectedItem as ComboBoxItem;
                return selected == null ? "Панель администратора" : Convert.ToString(selected.Content);
            }
        }

        public AdminPage()
        {
            InitializeComponent();
            ApplyLightFilterStyle(TableSelector);
            ApplyLightFilterStyle(SearchColumnBox);
            ApplyLightFilterStyle(SortColumnBox);
            ApplyLightFilterStyle(SortDirectionBox);
            SortDirectionBox.SelectedIndex = 0;
            TableSelector.SelectedIndex = 1;
        }

        private static void ApplyLightFilterStyle(ComboBox combo)
        {
            combo.Background = Brushes.White;
            combo.Foreground = Brushes.Black;
            combo.BorderBrush = Brushes.Gray;
            combo.SetValue(System.Windows.Documents.TextElement.ForegroundProperty, Brushes.Black);
            foreach (ComboBoxItem existingItem in combo.Items)
            {
                existingItem.Foreground = Brushes.Black;
                existingItem.Background = Brushes.White;
            }
            var itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            itemStyle.Setters.Add(new Setter(System.Windows.Documents.TextElement.ForegroundProperty, Brushes.Black));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 5, 8, 5)));
            var highlighted = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlighted.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(220, 232, 245))));
            highlighted.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Black));
            itemStyle.Triggers.Add(highlighted);
            combo.ItemContainerStyle = itemStyle;
        }

        private void TableSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loadingTable) return;
            string selectedKey = SelectedTableKey();
            if (string.IsNullOrEmpty(selectedKey)) return;
            if (!string.IsNullOrEmpty(_loadedTableKey) && selectedKey != _loadedTableKey)
            {
                if (!ResolvePendingChanges("переключить таблицу", false))
                {
                    RestoreTableSelection();
                    return;
                }
            }
            LoadSelectedTable();
        }

        private void LoadSelectedTable()
        {
            string table = SelectedTableKey();
            if (string.IsNullOrEmpty(table)) return;
            try
            {
                _loadingTable = true;
                _loadedTableKey = table;
                _properties = GetScalarProperties(GetEntityType(table));
                _allRows = LoadRowsFromContext(table);
                BuildGridColumns();
                FillFilterChoices();
                _loadingTable = false;
                ApplyView();
            }
            catch (Exception ex)
            {
                _loadingTable = false;
                MessageBox.Show("Не удалось загрузить выбранную таблицу.\n\n" + ex.GetBaseException().Message,
                    "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Type GetEntityType(string table)
        {
            switch (table)
            {
                case "roles": return typeof(roles);
                case "users": return typeof(users);
                case "addresses": return typeof(addresses);
                case "restaurants": return typeof(restaurants);
                case "categories": return typeof(categories);
                case "dishes": return typeof(dishes);
                case "orders": return typeof(orders);
                case "order_items": return typeof(order_items);
                case "payments": return typeof(payments);
                case "couriers": return typeof(couriers);
                case "deliveries": return typeof(deliveries);
                case "reviews": return typeof(reviews);
                default: throw new InvalidOperationException("Неизвестная таблица базы данных.");
            }
        }

        private static List<PropertyInfo> GetScalarProperties(Type entityType)
        {
            return entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && IsScalarType(p.PropertyType) && p.Name != "password_hash")
                .OrderBy(p => p.Name == "id" ? 0 : 1)
                .ThenBy(p => p.MetadataToken)
                .ToList();
        }

        private static bool IsScalarType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan);
        }

        private List<object> LoadRowsFromContext(string table)
        {
            switch (table)
            {
                case "roles": Core.Context.roles.Load(); return Core.Context.roles.Local.Cast<object>().ToList();
                case "users": Core.Context.users.Load(); return Core.Context.users.Local.Cast<object>().ToList();
                case "addresses": Core.Context.addresses.Load(); return Core.Context.addresses.Local.Cast<object>().ToList();
                case "restaurants": Core.Context.restaurants.Load(); return Core.Context.restaurants.Local.Cast<object>().ToList();
                case "categories": Core.Context.categories.Load(); return Core.Context.categories.Local.Cast<object>().ToList();
                case "dishes": Core.Context.dishes.Load(); return Core.Context.dishes.Local.Cast<object>().ToList();
                case "orders": Core.Context.orders.Load(); return Core.Context.orders.Local.Cast<object>().ToList();
                case "order_items": Core.Context.order_items.Load(); return Core.Context.order_items.Local.Cast<object>().ToList();
                case "payments": Core.Context.payments.Load(); return Core.Context.payments.Local.Cast<object>().ToList();
                case "couriers": Core.Context.couriers.Load(); return Core.Context.couriers.Local.Cast<object>().ToList();
                case "deliveries": Core.Context.deliveries.Load(); return Core.Context.deliveries.Local.Cast<object>().ToList();
                case "reviews": Core.Context.reviews.Load(); return Core.Context.reviews.Local.Cast<object>().ToList();
                default: throw new InvalidOperationException("Неизвестная таблица базы данных.");
            }
        }

        private void BuildGridColumns()
        {
            RecordsGrid.ItemsSource = null;
            RecordsGrid.Columns.Clear();
            foreach (var property in _properties)
            {
                var binding = new Binding(property.Name) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
                Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                DataGridColumn column;
                if (propertyType == typeof(bool))
                {
                    column = new DataGridCheckBoxColumn { Binding = binding };
                }
                else
                {
                    if (propertyType == typeof(DateTime)) binding.StringFormat = "dd.MM.yyyy HH:mm";
                    else if (propertyType == typeof(decimal)) binding.StringFormat = "N2";
                    column = new DataGridTextColumn { Binding = binding };
                }
                column.Header = GetFieldLabel(property.Name);
                column.IsReadOnly = property.Name == "id" || property.Name == "created_at";
                column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                RecordsGrid.Columns.Add(column);
            }
        }

        private void FillFilterChoices()
        {
            SearchColumnBox.Items.Clear();
            SearchColumnBox.Items.Add(new ComboBoxItem { Content = "Все поля", Tag = string.Empty, Foreground = Brushes.Black, Background = Brushes.White });
            foreach (var property in _properties)
                SearchColumnBox.Items.Add(new ComboBoxItem { Content = GetFieldLabel(property.Name), Tag = property.Name, Foreground = Brushes.Black, Background = Brushes.White });
            SearchColumnBox.SelectedIndex = 0;

            SortColumnBox.Items.Clear();
            foreach (var property in _properties)
                SortColumnBox.Items.Add(new ComboBoxItem { Content = GetFieldLabel(property.Name), Tag = property.Name, Foreground = Brushes.Black, Background = Brushes.White });
            SortColumnBox.SelectedIndex = 0;
        }

        private static string GetFieldLabel(string name)
        {
            switch (name)
            {
                case "id": return "ID";
                case "role_id": return "Роль (ID)";
                case "login": return "Логин";
                case "full_name": return "ФИО";
                case "phone": return "Телефон";
                case "email": return "Электронная почта";
                case "created_at": return "Дата создания";
                case "is_active": return "Активна";
                case "user_id": return "Пользователь (ID)";
                case "address_text": return "Адрес";
                case "latitude": return "Широта";
                case "longitude": return "Долгота";
                case "name": return "Название";
                case "restaurant_id": return "Ресторан (ID)";
                case "category_id": return "Категория (ID)";
                case "description": return "Описание";
                case "rating": return "Рейтинг / оценка";
                case "price": return "Цена";
                case "is_available": return "Доступно";
                case "address_id": return "Адрес (ID)";
                case "status": return "Статус";
                case "payment_method": return "Способ оплаты";
                case "total_amount": return "Сумма заказа";
                case "comment": return "Комментарий";
                case "order_id": return "Заказ (ID)";
                case "dish_id": return "Блюдо (ID)";
                case "quantity": return "Количество";
                case "amount": return "Сумма оплаты";
                case "paid_at": return "Дата оплаты";
                case "vehicle_type": return "Транспорт";
                case "courier_id": return "Курьер (ID)";
                case "assigned_at": return "Время назначения";
                case "picked_up_at": return "Время получения";
                case "delivered_at": return "Время доставки";
                default: return name.Replace('_', ' ');
            }
        }

        private string SelectedTableKey()
        {
            var item = TableSelector.SelectedItem as ComboBoxItem;
            return item == null ? null : Convert.ToString(item.Tag);
        }

        private void ApplyView()
        {
            if (_loadingTable || RecordsGrid == null) return;
            IEnumerable<object> result = _allRows;
            string search = (SearchBox.Text ?? string.Empty).Trim();
            var searchItem = SearchColumnBox.SelectedItem as ComboBoxItem;
            string searchProperty = searchItem == null ? string.Empty : Convert.ToString(searchItem.Tag);
            if (search.Length > 0)
            {
                var properties = string.IsNullOrEmpty(searchProperty)
                    ? _properties
                    : _properties.Where(p => p.Name == searchProperty).ToList();
                result = result.Where(row => properties.Any(property =>
                {
                    object value = property.GetValue(row, null);
                    return Convert.ToString(value, System.Globalization.CultureInfo.CurrentCulture)
                        .IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0;
                }));
            }

            var sortItem = SortColumnBox.SelectedItem as ComboBoxItem;
            string sortProperty = sortItem == null ? string.Empty : Convert.ToString(sortItem.Tag);
            PropertyInfo sort = _properties.FirstOrDefault(p => p.Name == sortProperty);
            if (sort != null)
            {
                var direction = SortDirectionBox.SelectedItem as ComboBoxItem;
                bool descending = direction != null && Convert.ToString(direction.Tag) == "desc";
                result = descending
                    ? result.OrderByDescending(row => sort.GetValue(row, null), ObjectComparer.Instance)
                    : result.OrderBy(row => sort.GetValue(row, null), ObjectComparer.Instance);
            }
            RecordsGrid.ItemsSource = result.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyView();
        }

        private void FilterSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyView();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Clear();
            SearchColumnBox.SelectedIndex = 0;
            SortColumnBox.SelectedIndex = 0;
            SortDirectionBox.SelectedIndex = 0;
            ApplyView();
        }

        private void AddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!ResolvePendingChanges("добавить запись", false)) return;
            object entity;
            string error;
            if (!TryCreateNewEntity(SelectedTableKey(), out entity, out error))
            {
                MessageBox.Show(error, "Нельзя добавить запись", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            AddEntityToContext(SelectedTableKey(), entity);
            LoadSelectedTable();
            RecordsGrid.SelectedItem = _allRows.FirstOrDefault(x => ReferenceEquals(x, entity));
            if (RecordsGrid.SelectedItem != null)
                RecordsGrid.ScrollIntoView(RecordsGrid.SelectedItem);
        }

        private bool TryCreateNewEntity(string table, out object entity, out string error)
        {
            entity = null;
            error = string.Empty;
            string suffix = DateTime.Now.ToString("yyMMddHHmmssfff");
            switch (table)
            {
                case "roles":
                    entity = new roles { name = "НоваяРоль" + suffix };
                    return true;
                case "users":
                    entity = new users
                    {
                        role_id = 0,
                        login = string.Empty,
                        full_name = string.Empty,
                        phone = string.Empty,
                        email = string.Empty,
                        password_hash = string.Empty,
                        created_at = DateTime.Now,
                        is_active = true
                    };
                    return true;
                case "addresses":
                {
                    var user = Core.Context.users.FirstOrDefault(x => x.is_active);
                    if (user == null) { error = "Сначала добавьте пользователя."; return false; }
                    entity = new addresses { user_id = user.id, address_text = "Новый адрес", latitude = null, longitude = null };
                    return true;
                }
                case "restaurants":
                    entity = new restaurants { name = "Новый ресторан " + suffix, address_text = "Укажите адрес", phone = null, description = null, rating = null, is_active = true };
                    return true;
                case "categories":
                {
                    var restaurant = Core.Context.restaurants.FirstOrDefault();
                    if (restaurant == null) { error = "Сначала добавьте ресторан."; return false; }
                    entity = new categories { restaurant_id = restaurant.id, name = "Новая категория " + suffix };
                    return true;
                }
                case "dishes":
                {
                    var category = Core.Context.categories.FirstOrDefault();
                    if (category == null) { error = "Сначала добавьте категорию меню."; return false; }
                    entity = new dishes { category_id = category.id, name = "Новое блюдо " + suffix, description = null, price = 0m, is_available = true };
                    return true;
                }
                case "orders":
                {
                    var user = (from u in Core.Context.users
                                join a in Core.Context.addresses on u.id equals a.user_id
                                where u.is_active
                                select new { User = u, Address = a }).FirstOrDefault();
                    var restaurant = Core.Context.restaurants.FirstOrDefault(x => x.is_active);
                    if (user == null || restaurant == null) { error = "Для добавления заказа нужны активный пользователь с адресом и активный ресторан."; return false; }
                    entity = new orders { user_id = user.User.id, restaurant_id = restaurant.id, address_id = user.Address.id, status = "Новый", payment_method = "Наличные", total_amount = 0m, created_at = DateTime.Now, comment = null };
                    return true;
                }
                case "order_items":
                {
                    var orderList = Core.Context.orders.ToList();
                    var dishList = Core.Context.dishes.Where(x => x.is_available).ToList();
                    var categoryList = Core.Context.categories.ToList();
                    var itemList = Core.Context.order_items.ToList();
                    foreach (var order in orderList)
                    {
                        var dish = dishList.FirstOrDefault(d => categoryList.Any(c => c.id == d.category_id && c.restaurant_id == order.restaurant_id)
                            && !itemList.Any(i => i.order_id == order.id && i.dish_id == d.id));
                        if (dish != null)
                        {
                            entity = new order_items { order_id = order.id, dish_id = dish.id, quantity = 1, price = dish.price };
                            return true;
                        }
                    }
                    error = "Не найден заказ и доступное блюдо одного ресторана без повторяющейся позиции.";
                    return false;
                }
                case "payments":
                {
                    var order = Core.Context.orders.FirstOrDefault(o => !Core.Context.payments.Any(p => p.order_id == o.id));
                    if (order == null) { error = "Нет заказов без оплаты."; return false; }
                    entity = new payments { order_id = order.id, amount = order.total_amount, status = "Ожидает", paid_at = null };
                    return true;
                }
                case "couriers":
                {
                    var courierRole = Core.Context.roles.FirstOrDefault(x => x.name == "Courier");
                    if (courierRole == null) { error = "В базе нет роли Courier."; return false; }
                    var user = Core.Context.users.FirstOrDefault(u => u.role_id == courierRole.id && !Core.Context.couriers.Any(c => c.user_id == u.id));
                    if (user == null) { error = "Нет пользователей с ролью Courier без профиля курьера."; return false; }
                    entity = new couriers { user_id = user.id, vehicle_type = "Велосипед", is_active = true };
                    return true;
                }
                case "deliveries":
                {
                    var order = Core.Context.orders.FirstOrDefault(o => !Core.Context.deliveries.Any(d => d.order_id == o.id));
                    if (order == null) { error = "Нет заказов без записи доставки."; return false; }
                    entity = new deliveries { order_id = order.id, courier_id = null, status = "Ожидает курьера", assigned_at = null, picked_up_at = null, delivered_at = null };
                    return true;
                }
                case "reviews":
                {
                    var order = Core.Context.orders.FirstOrDefault(o => o.status == "Доставлен" && !Core.Context.reviews.Any(r => r.order_id == o.id));
                    if (order == null) { error = "Нет доставленных заказов без отзыва."; return false; }
                    entity = new reviews { user_id = order.user_id, restaurant_id = order.restaurant_id, order_id = order.id, rating = 5, comment = null, created_at = DateTime.Now };
                    return true;
                }
                default:
                    error = "Неизвестная таблица.";
                    return false;
            }
        }

        private static void AddEntityToContext(string table, object entity)
        {
            switch (table)
            {
                case "roles": Core.Context.roles.Add((roles)entity); break;
                case "users": Core.Context.users.Add((users)entity); break;
                case "addresses": Core.Context.addresses.Add((addresses)entity); break;
                case "restaurants": Core.Context.restaurants.Add((restaurants)entity); break;
                case "categories": Core.Context.categories.Add((categories)entity); break;
                case "dishes": Core.Context.dishes.Add((dishes)entity); break;
                case "orders": Core.Context.orders.Add((orders)entity); break;
                case "order_items": Core.Context.order_items.Add((order_items)entity); break;
                case "payments": Core.Context.payments.Add((payments)entity); break;
                case "couriers": Core.Context.couriers.Add((couriers)entity); break;
                case "deliveries": Core.Context.deliveries.Add((deliveries)entity); break;
                case "reviews": Core.Context.reviews.Add((reviews)entity); break;
                default: throw new InvalidOperationException("Неизвестная таблица.");
            }
        }

        private bool SetPasswordsForNewUsers()
        {
            var usersWithoutPassword = Core.Context.ChangeTracker.Entries<users>()
                .Where(entry => entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.password_hash))
                .Select(entry => entry.Entity)
                .ToList();

            foreach (users item in usersWithoutPassword)
            {
                var dialog = new NewUserPasswordWindow();
                Window owner = Window.GetWindow(this);
                if (owner != null) dialog.Owner = owner;
                if (dialog.ShowDialog() != true) return false;

                string error;
                if (!Helpers.IsValidPassword(dialog.Password, out error))
                {
                    MessageBox.Show(error, "Некорректный пароль", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                item.password_hash = Helpers.HashPassword(dialog.Password);
            }

            return true;
        }

        private sealed class NewUserPasswordWindow : Window
        {
            private readonly PasswordBox _passwordBox;
            private readonly PasswordBox _confirmBox;

            public string Password { get; private set; }

            public NewUserPasswordWindow()
            {
                Title = "Пароль новой учётной записи";
                Width = 360;
                Height = 230;
                ResizeMode = ResizeMode.NoResize;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                Background = Brushes.White;
                Foreground = Brushes.Black;

                var panel = new StackPanel { Margin = new Thickness(16) };
                panel.Children.Add(new TextBlock
                {
                    Text = "Введите пароль для новой учётной записи:",
                    Margin = new Thickness(0, 0, 0, 6),
                    Foreground = Brushes.Black
                });

                _passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(5) };
                panel.Children.Add(_passwordBox);
                panel.Children.Add(new TextBlock
                {
                    Text = "Повторите пароль:",
                    Margin = new Thickness(0, 0, 0, 6),
                    Foreground = Brushes.Black
                });
                _confirmBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(5) };
                panel.Children.Add(_confirmBox);

                var buttons = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var saveButton = new Button { Content = "Сохранить", Width = 90, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(6) };
                saveButton.Click += SaveButton_Click;
                var cancelButton = new Button { Content = "Отмена", Width = 90, Padding = new Thickness(6) };
                cancelButton.Click += (sender, args) => DialogResult = false;
                buttons.Children.Add(saveButton);
                buttons.Children.Add(cancelButton);
                panel.Children.Add(buttons);
                Content = panel;
            }

            private void SaveButton_Click(object sender, RoutedEventArgs e)
            {
                if (string.IsNullOrEmpty(_passwordBox.Password))
                {
                    MessageBox.Show(this, "Введите пароль.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (_passwordBox.Password != _confirmBox.Password)
                {
                    MessageBox.Show(this, "Пароли не совпадают.", "Проверка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Password = _passwordBox.Password;
                DialogResult = true;
            }
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentChanges(true, true);
        }

        private bool SaveCurrentChanges(bool showMessage, bool reloadAfterSave)
        {
            RecordsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            RecordsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            List<string> errors = ValidateChangedRows();
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, errors), "Проверьте введённые данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!SetPasswordsForNewUsers()) return false;

            var orderIds = new HashSet<int>();
            foreach (var entry in Core.Context.ChangeTracker.Entries().Where(e => e.Entity is order_items && e.State != EntityState.Unchanged && e.State != EntityState.Detached).ToList())
            {
                var item = (order_items)entry.Entity;
                if (item.order_id > 0) orderIds.Add(item.order_id);
                if (entry.State == EntityState.Modified)
                {
                    try
                    {
                        int oldOrderId = Convert.ToInt32(entry.OriginalValues["order_id"]);
                        if (oldOrderId > 0) orderIds.Add(oldOrderId);
                    }
                    catch { }
                }
            }

            try
            {
                using (var transaction = Core.Context.Database.BeginTransaction())
                {
                    Core.Context.SaveChanges();
                    foreach (int orderId in orderIds)
                    {
                        decimal total = Core.Context.order_items.Where(x => x.order_id == orderId)
                            .Select(x => (decimal?)(x.quantity * x.price)).Sum() ?? 0m;
                        var order = Core.Context.orders.FirstOrDefault(x => x.id == orderId);
                        if (order != null) order.total_amount = total;
                        foreach (var payment in Core.Context.payments.Where(x => x.order_id == orderId).ToList()) payment.amount = total;
                    }
                    Core.Context.SaveChanges();
                    transaction.Commit();
                }
                if (showMessage)
                    MessageBox.Show("Изменения сохранены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                if (reloadAfterSave) LoadSelectedTable();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить изменения. Проверьте формат значений, внешние ключи и ограничения таблицы.\n\n" + ex.GetBaseException().Message,
                    "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private List<string> ValidateChangedRows()
        {
            var result = new List<string>();
            var entries = Core.Context.ChangeTracker.Entries()
                .Where(e => IsCurrentTableEntity(e.Entity) && e.State != EntityState.Unchanged && e.State != EntityState.Detached && e.State != EntityState.Deleted)
                .ToList();
            foreach (var entry in entries)
            {
                object entity = entry.Entity;
                string error = ValidateEntity(entity);
                if (!string.IsNullOrEmpty(error)) result.Add(GetFieldLabelForEntity(entity) + ": " + error);
            }
            return result;
        }

        private bool IsCurrentTableEntity(object entity)
        {
            if (string.IsNullOrEmpty(_loadedTableKey) || entity == null) return false;
            Type expected = GetEntityType(_loadedTableKey);
            return expected == entity.GetType() || expected.IsAssignableFrom(entity.GetType());
        }

        private string GetFieldLabelForEntity(object entity)
        {
            if (entity is users) return "Пользователь";
            if (entity is roles) return "Роль";
            if (entity is addresses) return "Адрес";
            if (entity is restaurants) return "Ресторан";
            if (entity is categories) return "Категория";
            if (entity is dishes) return "Блюдо";
            if (entity is orders) return "Заказ";
            if (entity is order_items) return "Позиция заказа";
            if (entity is payments) return "Оплата";
            if (entity is couriers) return "Курьер";
            if (entity is deliveries) return "Доставка";
            if (entity is reviews) return "Отзыв";
            return "Запись";
        }

        private string ValidateEntity(object entity)
        {
            if (entity is roles)
            {
                var item = (roles)entity;
                return string.IsNullOrWhiteSpace(item.name) || item.name.Length > 50 ? "Укажите название роли длиной до 50 символов." : string.Empty;
            }
            if (entity is users)
            {
                var item = (users)entity;
                if (!Helpers.IsValidLogin(item.login)) return "Логин должен содержать латинские буквы, цифры, дефис или подчёркивание.";
                if (!Helpers.IsValidFIO(item.full_name)) return "Введите корректные ФИО.";
                if (!Helpers.IsValidPhone(item.phone)) return "Введите корректный телефон.";
                if (!Core.Context.roles.Any(x => x.id == item.role_id)) return "Выбранная роль не существует.";
                if (Core.Context.users.Any(x => x.id != item.id && x.login == item.login)) return "Такой логин уже существует.";
                if (Core.Context.users.Any(x => x.id != item.id && x.phone == item.phone)) return "Такой телефон уже существует.";
                if (!string.IsNullOrWhiteSpace(item.email) && !Helpers.IsValidEmail(item.email)) return "Введите корректный адрес электронной почты.";
                if (string.IsNullOrWhiteSpace(item.password_hash) && Core.Context.Entry(item).State != EntityState.Added)
                    return "Не задан хеш пароля.";
                return string.Empty;
            }
            if (entity is addresses)
            {
                var item = (addresses)entity;
                if (!Core.Context.users.Any(x => x.id == item.user_id)) return "Пользователь не существует.";
                if (string.IsNullOrWhiteSpace(item.address_text)) return "Адрес обязателен.";
                if (item.latitude.HasValue && (item.latitude < -90m || item.latitude > 90m)) return "Широта должна быть в диапазоне от -90 до 90.";
                if (item.longitude.HasValue && (item.longitude < -180m || item.longitude > 180m)) return "Долгота должна быть в диапазоне от -180 до 180.";
                return string.Empty;
            }
            if (entity is restaurants)
            {
                var item = (restaurants)entity;
                if (string.IsNullOrWhiteSpace(item.name) || string.IsNullOrWhiteSpace(item.address_text)) return "Название и адрес обязательны.";
                if (item.rating.HasValue && (item.rating < 0m || item.rating > 5m)) return "Рейтинг должен быть от 0 до 5.";
                if (!string.IsNullOrWhiteSpace(item.phone) && !Helpers.IsValidPhone(item.phone)) return "Введите корректный телефон.";
                return string.Empty;
            }
            if (entity is categories)
            {
                var item = (categories)entity;
                if (!Core.Context.restaurants.Any(x => x.id == item.restaurant_id)) return "Ресторан не существует.";
                if (string.IsNullOrWhiteSpace(item.name)) return "Название категории обязательно.";
                if (Core.Context.categories.Any(x => x.id != item.id && x.restaurant_id == item.restaurant_id && x.name == item.name)) return "Такая категория уже есть у выбранного ресторана.";
                return string.Empty;
            }
            if (entity is dishes)
            {
                var item = (dishes)entity;
                if (!Core.Context.categories.Any(x => x.id == item.category_id)) return "Категория не существует.";
                if (string.IsNullOrWhiteSpace(item.name)) return "Название блюда обязательно.";
                if (item.price < 0) return "Цена не может быть отрицательной.";
                return string.Empty;
            }
            if (entity is orders)
            {
                var item = (orders)entity;
                if (!Core.Context.users.Any(x => x.id == item.user_id) || !Core.Context.restaurants.Any(x => x.id == item.restaurant_id) || !Core.Context.addresses.Any(x => x.id == item.address_id)) return "Проверьте пользователя, ресторан и адрес по ID.";
                if (!new[] { "Новый", "Принят", "Готовится", "Готов", "Передан курьеру", "Доставляется", "Доставлен", "Отменён" }.Contains(item.status)) return "Недопустимый статус заказа.";
                if (!new[] { "Карта", "Наличные", "SberPay" }.Contains(item.payment_method)) return "Недопустимый способ оплаты.";
                if (item.total_amount < 0) return "Сумма заказа не может быть отрицательной.";
                return string.Empty;
            }
            if (entity is order_items)
            {
                var item = (order_items)entity;
                if (!Core.Context.orders.Any(x => x.id == item.order_id)) return "Заказ не существует.";
                var dish = Core.Context.dishes.FirstOrDefault(x => x.id == item.dish_id);
                if (dish == null) return "Блюдо не существует.";
                var category = Core.Context.categories.FirstOrDefault(x => x.id == dish.category_id);
                var order = Core.Context.orders.FirstOrDefault(x => x.id == item.order_id);
                if (category == null || order == null || category.restaurant_id != order.restaurant_id) return "Блюдо должно принадлежать ресторану этого заказа.";
                if (item.quantity <= 0 || item.price < 0) return "Количество должно быть больше нуля, цена не может быть отрицательной.";
                if (Core.Context.order_items.Any(x => x.id != item.id && x.order_id == item.order_id && x.dish_id == item.dish_id)) return "Это блюдо уже есть в составе выбранного заказа.";
                return string.Empty;
            }
            if (entity is payments)
            {
                var item = (payments)entity;
                if (!Core.Context.orders.Any(x => x.id == item.order_id)) return "Заказ не существует.";
                if (item.amount < 0) return "Сумма оплаты не может быть отрицательной.";
                if (!new[] { "Ожидает", "Оплачено", "Ошибка", "Возврат" }.Contains(item.status)) return "Недопустимый статус оплаты.";
                if (Core.Context.payments.Any(x => x.id != item.id && x.order_id == item.order_id)) return "Для заказа уже существует оплата.";
                return string.Empty;
            }
            if (entity is couriers)
            {
                var item = (couriers)entity;
                var user = Core.Context.users.FirstOrDefault(x => x.id == item.user_id);
                if (user == null) return "Пользователь не существует.";
                var role = Core.Context.roles.FirstOrDefault(x => x.id == user.role_id);
                if (role == null || role.name != "Courier") return "У пользователя должна быть роль Courier.";
                if (Core.Context.couriers.Any(x => x.id != item.id && x.user_id == item.user_id)) return "Для этого пользователя уже создан профиль курьера.";
                return string.Empty;
            }
            if (entity is deliveries)
            {
                var item = (deliveries)entity;
                if (!Core.Context.orders.Any(x => x.id == item.order_id)) return "Заказ не существует.";
                if (item.courier_id.HasValue && !Core.Context.couriers.Any(x => x.id == item.courier_id.Value)) return "Курьер не существует.";
                if (!new[] { "Ожидает курьера", "Назначена", "Забрана", "В пути", "Доставлена", "Отменена" }.Contains(item.status)) return "Недопустимый статус доставки.";
                if (Core.Context.deliveries.Any(x => x.id != item.id && x.order_id == item.order_id)) return "Для заказа уже существует запись доставки.";
                return string.Empty;
            }
            if (entity is reviews)
            {
                var item = (reviews)entity;
                var order = Core.Context.orders.FirstOrDefault(x => x.id == item.order_id);
                if (order == null) return "Заказ не существует.";
                if (order.status != "Доставлен") return "Оставлять отзыв можно только после доставки.";
                if (order.user_id != item.user_id || order.restaurant_id != item.restaurant_id) return "Пользователь и ресторан должны соответствовать заказу.";
                if (item.rating < 1 || item.rating > 5) return "Оценка должна быть от 1 до 5.";
                if (Core.Context.reviews.Any(x => x.id != item.id && x.order_id == item.order_id)) return "Для этого заказа уже есть отзыв.";
                return string.Empty;
            }
            return string.Empty;
        }

        private bool HasPendingChangesForCurrentTable()
        {
            return Core.Context.ChangeTracker.Entries().Any(e => IsCurrentTableEntity(e.Entity)
                && e.State != EntityState.Unchanged && e.State != EntityState.Detached);
        }

        private List<System.Data.Entity.Infrastructure.DbEntityEntry> GetPendingEntriesForCurrentTable()
        {
            return Core.Context.ChangeTracker.Entries().Where(e => IsCurrentTableEntity(e.Entity)
                && e.State != EntityState.Unchanged && e.State != EntityState.Detached).ToList();
        }

        private bool ResolvePendingChanges(string action, bool reloadAfterSave)
        {
            RecordsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            RecordsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            var pending = GetPendingEntriesForCurrentTable();
            if (pending.Count == 0) return true;
            MessageBoxResult choice = MessageBox.Show(
                "Есть несохранённые изменения в текущей таблице. Сохранить их перед тем, как " + action + "?\n\nДа — сохранить. Нет — отменить изменения. Отмена — вернуться к редактированию.",
                "Несохранённые изменения", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (choice == MessageBoxResult.Yes) return SaveCurrentChanges(false, reloadAfterSave);
            if (choice == MessageBoxResult.No)
            {
                DiscardEntries(pending);
                return true;
            }
            return false;
        }

        private static void DiscardEntries(IEnumerable<System.Data.Entity.Infrastructure.DbEntityEntry> entries)
        {
            foreach (var entry in entries.ToList())
            {
                try
                {
                    if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
                    else if (entry.State == EntityState.Modified) entry.Reload();
                    else if (entry.State == EntityState.Deleted) entry.State = EntityState.Unchanged;
                }
                catch { }
            }
        }

        private void RestoreTableSelection()
        {
            _loadingTable = true;
            foreach (ComboBoxItem item in TableSelector.Items)
            {
                if (Convert.ToString(item.Tag) == _loadedTableKey)
                {
                    TableSelector.SelectedItem = item;
                    break;
                }
            }
            _loadingTable = false;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (!ResolvePendingChanges("обновить данные", false)) return;
            ClearTrackedEntities();
            LoadSelectedTable();
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            RecordsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            RecordsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            if (HasPendingChangesForCurrentTable())
            {
                if (!ResolvePendingChanges("удалить выбранные записи", false)) return;
                LoadSelectedTable();
                MessageBox.Show("Изменения обработаны. Выберите строки для удаления ещё раз.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<object> selected = RecordsGrid.SelectedItems.Cast<object>().ToList();
            string table = SelectedTableKey();
            if (selected.Count == 0)
            {
                MessageBox.Show("Сначала выберите одну или несколько строк.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (table == "users" && AppSession.CurrentUserId.HasValue && selected.Cast<users>().Any(x => x.id == AppSession.CurrentUserId.Value))
            {
                MessageBox.Show("Нельзя удалить учётную запись, под которой выполнен вход.", "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (table == "roles" && selected.Cast<roles>().Any(r => Core.Context.users.Any(u => u.role_id == r.id)))
            {
                MessageBox.Show("Нельзя удалить роль, пока она назначена пользователю. Сначала переназначьте пользователя.", "Роль используется", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить выбранные записи (" + selected.Count + ") из таблицы «" + CurrentViewTitle + "»?\n\nСвязанные данные будут обработаны в той же транзакции.",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                using (var transaction = Core.Context.Database.BeginTransaction())
                {
                    foreach (object item in selected)
                        DeleteRowWithCascade(table, GetId(item), item);
                    transaction.Commit();
                }
                ClearTrackedEntities();
                LoadSelectedTable();
                MessageBox.Show("Выбранные записи удалены.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ClearTrackedEntities();
                LoadSelectedTable();
                MessageBox.Show("Не удалось удалить записи. Операция отменена.\n\n" + ex.GetBaseException().Message,
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static int GetId(object item)
        {
            if (item is roles) return ((roles)item).id;
            if (item is users) return ((users)item).id;
            if (item is addresses) return ((addresses)item).id;
            if (item is restaurants) return ((restaurants)item).id;
            if (item is categories) return ((categories)item).id;
            if (item is dishes) return ((dishes)item).id;
            if (item is orders) return ((orders)item).id;
            if (item is order_items) return ((order_items)item).id;
            if (item is payments) return ((payments)item).id;
            if (item is couriers) return ((couriers)item).id;
            if (item is deliveries) return ((deliveries)item).id;
            if (item is reviews) return ((reviews)item).id;
            throw new InvalidOperationException("Не удалось получить ID записи.");
        }

        private static void DeleteRowWithCascade(string table, int id, object entity)
        {
            switch (table)
            {
                case "roles":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.roles WHERE id = @p0", id);
                    break;
                case "users":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.reviews WHERE user_id = @p0", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE user_id = @p0 OR address_id IN (SELECT id FROM dbo.addresses WHERE user_id = @p0)", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.couriers WHERE user_id = @p0", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.addresses WHERE user_id = @p0", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.users WHERE id = @p0", id);
                    break;
                case "addresses":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE address_id = @p0", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.addresses WHERE id = @p0", id);
                    break;
                case "restaurants":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.reviews WHERE restaurant_id = @p0", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE restaurant_id = @p0 OR id IN (SELECT oi.order_id FROM dbo.order_items oi INNER JOIN dbo.dishes d ON d.id = oi.dish_id INNER JOIN dbo.categories c ON c.id = d.category_id WHERE c.restaurant_id = @p0)", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.restaurants WHERE id = @p0", id);
                    break;
                case "categories":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE id IN (SELECT oi.order_id FROM dbo.order_items oi INNER JOIN dbo.dishes d ON d.id = oi.dish_id WHERE d.category_id = @p0)", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.categories WHERE id = @p0", id);
                    break;
                case "dishes":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE id IN (SELECT order_id FROM dbo.order_items WHERE dish_id = @p0)", id);
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.dishes WHERE id = @p0", id);
                    break;
                case "orders":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE id = @p0", id);
                    break;
                case "order_items":
                {
                    int orderId = ((order_items)entity).order_id;
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.order_items WHERE id = @p0", id);
                    int remaining = Core.Context.Database.SqlQuery<int>("SELECT COUNT(*) FROM dbo.order_items WHERE order_id = @p0", orderId).Single();
                    if (remaining == 0)
                    {
                        Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.orders WHERE id = @p0", orderId);
                    }
                    else
                    {
                        Core.Context.Database.ExecuteSqlCommand("UPDATE dbo.orders SET total_amount = ISNULL((SELECT SUM(quantity * price) FROM dbo.order_items WHERE order_id = @p0), 0) WHERE id = @p0", orderId);
                        Core.Context.Database.ExecuteSqlCommand("UPDATE dbo.payments SET amount = (SELECT total_amount FROM dbo.orders WHERE id = @p0) WHERE order_id = @p0", orderId);
                    }
                    break;
                }
                case "payments":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.payments WHERE id = @p0", id);
                    break;
                case "couriers":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.couriers WHERE id = @p0", id);
                    break;
                case "deliveries":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.deliveries WHERE id = @p0", id);
                    break;
                case "reviews":
                    Core.Context.Database.ExecuteSqlCommand("DELETE FROM dbo.reviews WHERE id = @p0", id);
                    break;
                default:
                    throw new InvalidOperationException("Неизвестная таблица.");
            }
        }

        private static void ClearTrackedEntities()
        {
            foreach (var entry in Core.Context.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
        }

        private void DiagramButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new DiagrammPage());
        }

        private sealed class ObjectComparer : IComparer<object>
        {
            public static readonly ObjectComparer Instance = new ObjectComparer();
            public int Compare(object first, object second)
            {
                if (ReferenceEquals(first, second)) return 0;
                if (first == null) return -1;
                if (second == null) return 1;
                if (first is IComparable && first.GetType() == second.GetType()) return ((IComparable)first).CompareTo(second);
                return string.Compare(Convert.ToString(first), Convert.ToString(second), StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}

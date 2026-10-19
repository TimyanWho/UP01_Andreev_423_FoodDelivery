using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class UserPage : Page
    {
        private bool _loading;
        private List<DishView> _allDishes = new List<DishView>();
        private List<OrderView> _allOrders = new List<OrderView>();
        private List<addresses> _addresses = new List<addresses>();
        private readonly List<CartItem> _cart = new List<CartItem>();

        public UserPage()
        {
            InitializeComponent();
            _loading = true;
            try
            {
                LoadCatalog();
                LoadAddresses();
                LoadOrders();
                LoadReviews();
                OrderStatusBox.SelectedIndex = 0;
                OrderSortBox.SelectedIndex = 0;
                DishSortBox.SelectedIndex = 0;
                PaymentBox.SelectedIndex = 0;
                ReviewRatingBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные кабинета пользователя.\n\n" + ex.GetBaseException().Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _loading = false; }
            UpdateCatalogView();
            UpdateOrdersView();
            UpdateCartView();
        }

        private int? CurrentUserId { get { return AppSession.CurrentUserId; } }

        private void LoadCatalog()
        {
            var restaurants = Core.Context.restaurants.Where(x => x.is_active).OrderBy(x => x.name).ToList();
            var categoryById = Core.Context.categories.ToList().ToDictionary(x => x.id);
            var restaurantNames = restaurants.ToDictionary(x => x.id, x => x.name);
            _allDishes = Core.Context.dishes.Where(x => x.is_available).ToList().Select(d =>
            {
                categories category;
                if (!categoryById.TryGetValue(d.category_id, out category)) return null;
                string restaurantName;
                if (!restaurantNames.TryGetValue(category.restaurant_id, out restaurantName)) return null;
                return new DishView
                {
                    Id = d.id,
                    RestaurantId = category.restaurant_id,
                    RestaurantName = restaurantName,
                    CategoryName = category.name,
                    Name = d.name,
                    Description = d.description ?? string.Empty,
                    Price = d.price
                };
            }).Where(x => x != null).ToList();

            var options = new List<RestaurantOption> { new RestaurantOption(null, "Все рестораны") };
            options.AddRange(restaurants.Select(x => new RestaurantOption(x.id, x.name)));
            RestaurantBox.ItemsSource = options;
            RestaurantBox.SelectedIndex = 0;
        }

        private void UpdateCatalogView()
        {
            if (_loading || !IsInitialized || DishesGrid == null) return;
            IEnumerable<DishView> query = _allDishes;
            var restaurant = RestaurantBox.SelectedItem as RestaurantOption;
            if (restaurant != null && restaurant.Id.HasValue)
                query = query.Where(x => x.RestaurantId == restaurant.Id.Value);

            string search = (DishSearchBox.Text ?? string.Empty).Trim();
            if (search.Length > 0)
                query = query.Where(x => (x.Name + " " + x.Description + " " + x.CategoryName + " " + x.RestaurantName)
                    .IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0);

            switch (DishSortBox.SelectedIndex)
            {
                case 1: query = query.OrderBy(x => x.Price).ThenBy(x => x.Name); break;
                case 2: query = query.OrderByDescending(x => x.Price).ThenBy(x => x.Name); break;
                default: query = query.OrderBy(x => x.Name); break;
            }
            DishesGrid.ItemsSource = query.ToList();
        }

        private void RestaurantBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateCatalogView(); }
        private void DishSearchBox_TextChanged(object sender, TextChangedEventArgs e) { UpdateCatalogView(); }
        private void DishSortBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateCatalogView(); }

        private void RefreshCatalog_Click(object sender, RoutedEventArgs e)
        {
            try { _loading = true; LoadCatalog(); }
            catch (Exception ex) { MessageBox.Show("Не удалось обновить меню.\n\n" + ex.GetBaseException().Message); }
            finally { _loading = false; }
            UpdateCatalogView();
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            var selected = DishesGrid.SelectedItem as DishView;
            if (selected == null)
            {
                MessageBox.Show("Сначала выберите блюдо в меню.", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int quantity;
            if (!int.TryParse(QuantityBox.Text, out quantity) || quantity < 1 || quantity > 99)
            {
                MessageBox.Show("Количество должно быть целым числом от 1 до 99.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_cart.Count > 0 && _cart.Any(x => x.RestaurantId != selected.RestaurantId))
            {
                MessageBox.Show("Один заказ может содержать блюда только одного ресторана. Завершите текущий заказ или очистите корзину перед выбором другого ресторана.",
                    "Разные рестораны", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var item = _cart.FirstOrDefault(x => x.DishId == selected.Id);
            if (item == null)
            {
                _cart.Add(new CartItem { DishId = selected.Id, RestaurantId = selected.RestaurantId, Name = selected.Name, Price = selected.Price, Quantity = quantity });
            }
            else
            {
                if (item.Quantity + quantity > 99)
                {
                    MessageBox.Show("Общее количество одного блюда не должно превышать 99.");
                    return;
                }
                item.Quantity += quantity;
            }
            QuantityBox.Text = "1";
            UpdateCartView();
            MessageBox.Show("Блюдо добавлено в корзину.", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveCartItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = CartGrid.SelectedItem as CartItem;
            if (selected == null) { MessageBox.Show("Выберите блюдо в корзине."); return; }
            _cart.Remove(selected);
            UpdateCartView();
        }

        private void UpdateCartView()
        {
            if (CartGrid == null || CartTotalText == null) return;
            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = _cart.ToList();
            CartTotalText.Text = "Итого: " + _cart.Sum(x => x.LineTotal).ToString("N2") + " ₽";
        }

        private void OrderAddressBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void PlaceOrder_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserId.HasValue) { MessageBox.Show("Сессия пользователя завершена. Выполните вход повторно."); return; }
            if (_cart.Count == 0) { MessageBox.Show("Корзина пуста."); return; }
            var address = OrderAddressBox.SelectedItem as AddressOption;
            if (address == null) { MessageBox.Show("Добавьте адрес доставки на вкладке «Мои адреса» и выберите его."); return; }
            if (!_addresses.Any(x => x.id == address.Id && x.user_id == CurrentUserId.Value))
            {
                MessageBox.Show("Можно выбрать только адрес из своей учётной записи.", "Проверка безопасности", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var paymentItem = PaymentBox.SelectedItem as ComboBoxItem;
            string paymentMethod = paymentItem == null ? "Карта" : Convert.ToString(paymentItem.Content);
            string comment = (OrderCommentBox.Text ?? string.Empty).Trim();
            if (comment.Length > 500) { MessageBox.Show("Комментарий не должен превышать 500 символов."); return; }

            if (MessageBox.Show("Оформить заказ на сумму " + _cart.Sum(x => x.LineTotal).ToString("N2") + " ₽?",
                "Подтверждение заказа", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using (var transaction = Core.Context.Database.BeginTransaction())
            {
                var tracked = new List<object>();
                try
                {
                    var order = new orders
                    {
                        user_id = CurrentUserId.Value,
                        restaurant_id = _cart[0].RestaurantId,
                        address_id = address.Id,
                        status = "Новый",
                        payment_method = paymentMethod,
                        total_amount = _cart.Sum(x => x.LineTotal),
                        created_at = DateTime.Now,
                        comment = comment.Length == 0 ? null : comment
                    };
                    Core.Context.orders.Add(order);
                    tracked.Add(order);
                    Core.Context.SaveChanges();

                    foreach (var item in _cart)
                    {
                        var orderItem = new order_items { order_id = order.id, dish_id = item.DishId, quantity = item.Quantity, price = item.Price };
                        Core.Context.order_items.Add(orderItem);
                        tracked.Add(orderItem);
                    }
                    var payment = new payments { order_id = order.id, amount = order.total_amount, status = "Ожидает", paid_at = null };
                    var delivery = new deliveries { order_id = order.id, courier_id = null, status = "Ожидает курьера", assigned_at = null, picked_up_at = null, delivered_at = null };
                    Core.Context.payments.Add(payment);
                    Core.Context.deliveries.Add(delivery);
                    tracked.Add(payment);
                    tracked.Add(delivery);
                    Core.Context.SaveChanges();
                    transaction.Commit();

                    _cart.Clear();
                    OrderCommentBox.Clear();
                    UpdateCartView();
                    LoadOrders();
                    LoadReviews();
                    MessageBox.Show("Заказ №" + order.id + " создан. После назначения курьера статус обновится в разделе «Мои заказы».",
                        "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { }
                    foreach (object entity in tracked)
                    {
                        try { Core.Context.Entry(entity).State = EntityState.Detached; } catch { }
                    }
                    MessageBox.Show("Не удалось оформить заказ. Проверьте доступность выбранного блюда и ограничения БД.\n\n" + ex.GetBaseException().Message,
                        "Ошибка заказа", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadAddresses()
        {
            if (!CurrentUserId.HasValue) return;
            _addresses = Core.Context.addresses.Where(x => x.user_id == CurrentUserId.Value).OrderBy(x => x.address_text).ToList();
            AddressesGrid.ItemsSource = null;
            AddressesGrid.ItemsSource = _addresses;
            var options = _addresses.Select(x => new AddressOption(x.id, x.address_text)).ToList();
            OrderAddressBox.ItemsSource = options;
            if (options.Count > 0) OrderAddressBox.SelectedIndex = 0;
            else OrderAddressBox.SelectedIndex = -1;
        }

        private void AddAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentUserId.HasValue) return;
            string addressText = (AddressInput.Text ?? string.Empty).Trim();
            if (addressText.Length < 5 || addressText.Length > 300)
            {
                MessageBox.Show("Введите полный адрес длиной от 5 до 300 символов.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                Core.Context.addresses.Add(new addresses { user_id = CurrentUserId.Value, address_text = addressText, latitude = null, longitude = null });
                Core.Context.SaveChanges();
                AddressInput.Clear();
                LoadAddresses();
                MessageBox.Show("Адрес сохранён.", "Адреса", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Не удалось сохранить адрес.\n\n" + ex.GetBaseException().Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void DeleteAddress_Click(object sender, RoutedEventArgs e)
        {
            var selected = AddressesGrid.SelectedItem as addresses;
            if (selected == null) { MessageBox.Show("Выберите адрес для удаления."); return; }
            if (Core.Context.orders.Any(x => x.address_id == selected.id))
            {
                MessageBox.Show("Этот адрес уже используется в заказах. Удаление запрещено, чтобы не нарушить историю заказов.", "Адрес используется", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить выбранный адрес?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                Core.Context.addresses.Remove(selected);
                Core.Context.SaveChanges();
                LoadAddresses();
            }
            catch (Exception ex) { MessageBox.Show("Не удалось удалить адрес.\n\n" + ex.GetBaseException().Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadOrders()
        {
            if (!CurrentUserId.HasValue) return;
            var restaurants = Core.Context.restaurants.ToList().ToDictionary(x => x.id, x => x.name);
            var addresses = Core.Context.addresses.ToList().ToDictionary(x => x.id, x => x.address_text);
            var userOrders = Core.Context.orders.Where(x => x.user_id == CurrentUserId.Value).ToList();
            _allOrders = userOrders.Select(x => new OrderView
            {
                Id = x.id,
                Restaurant = Lookup(restaurants, x.restaurant_id),
                CreatedAt = x.created_at,
                Status = x.status,
                TotalAmount = x.total_amount,
                Address = Lookup(addresses, x.address_id),
                PaymentMethod = x.payment_method
            }).OrderByDescending(x => x.CreatedAt).ToList();
            UpdateOrdersView();
        }

        private void UpdateOrdersView()
        {
            if (_loading || !IsInitialized || OrdersGrid == null || OrderStatusBox == null || OrderSortBox == null) return;
            IEnumerable<OrderView> query = _allOrders;
            string search = (OrderSearchBox.Text ?? string.Empty).Trim();
            if (search.Length > 0)
                query = query.Where(x => (x.Restaurant + " " + x.Address + " " + x.Status + " " + x.PaymentMethod)
                    .IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0);
            if (OrderStatusBox.SelectedIndex > 0)
            {
                var selected = OrderStatusBox.SelectedItem as ComboBoxItem;
                string status = selected == null ? string.Empty : Convert.ToString(selected.Content);
                query = query.Where(x => x.Status == status);
            }
            switch (OrderSortBox.SelectedIndex)
            {
                case 1: query = query.OrderBy(x => x.CreatedAt); break;
                case 2: query = query.OrderByDescending(x => x.TotalAmount); break;
                case 3: query = query.OrderBy(x => x.TotalAmount); break;
                default: query = query.OrderByDescending(x => x.CreatedAt); break;
            }
            OrdersGrid.ItemsSource = query.ToList();
        }

        private void OrderSearchBox_TextChanged(object sender, TextChangedEventArgs e) { UpdateOrdersView(); }
        private void OrderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateOrdersView(); }
        private void ClearOrderFilters_Click(object sender, RoutedEventArgs e)
        {
            OrderSearchBox.Clear();
            OrderStatusBox.SelectedIndex = 0;
            OrderSortBox.SelectedIndex = 0;
            UpdateOrdersView();
        }
        private void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            try { LoadOrders(); LoadReviews(); }
            catch (Exception ex) { MessageBox.Show("Не удалось обновить заказы.\n\n" + ex.GetBaseException().Message); }
        }

        private void CancelSelectedOrder_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrdersGrid.SelectedItem as OrderView;
            if (selected == null) { MessageBox.Show("Выберите заказ в списке."); return; }
            if (!CurrentUserId.HasValue) return;
            try
            {
                var order = Core.Context.orders.FirstOrDefault(x => x.id == selected.Id && x.user_id == CurrentUserId.Value);
                if (order == null) { MessageBox.Show("Заказ не найден."); return; }
                if (order.status != "Новый" && order.status != "Принят")
                {
                    MessageBox.Show("Отменить можно только новый или принятый заказ до назначения курьера.", "Отмена заказа", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var delivery = Core.Context.deliveries.FirstOrDefault(x => x.order_id == order.id);
                if (delivery != null && delivery.courier_id.HasValue)
                {
                    MessageBox.Show("Курьер уже назначен. Для сохранения истории заказ нельзя отменить из кабинета пользователя.", "Отмена заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (MessageBox.Show("Отменить заказ №" + order.id + "?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                order.status = "Отменён";
                if (delivery != null) delivery.status = "Отменена";
                Core.Context.SaveChanges();
                LoadOrders();
                LoadReviews();
            }
            catch (Exception ex) { MessageBox.Show("Не удалось отменить заказ.\n\n" + ex.GetBaseException().Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadReviews()
        {
            if (!CurrentUserId.HasValue) return;
            var userOrders = Core.Context.orders.Where(x => x.user_id == CurrentUserId.Value).ToList();
            var reviews = Core.Context.reviews.Where(x => x.user_id == CurrentUserId.Value).OrderByDescending(x => x.created_at).ToList();
            var reviewedOrderIds = new HashSet<int>(reviews.Select(x => x.order_id));
            var restaurantNames = Core.Context.restaurants.ToList().ToDictionary(x => x.id, x => x.name);
            var eligible = userOrders.Where(x => x.status == "Доставлен" && !reviewedOrderIds.Contains(x.id))
                .OrderByDescending(x => x.created_at)
                .Select(x => new ReviewOrderOption(x.id, x.restaurant_id, "Заказ №" + x.id + " — " + Lookup(restaurantNames, x.restaurant_id) + " (" + x.created_at.ToString("dd.MM.yyyy") + ")"))
                .ToList();
            ReviewOrderBox.ItemsSource = eligible;
            ReviewOrderBox.SelectedIndex = eligible.Count > 0 ? 0 : -1;
            ReviewsGrid.ItemsSource = reviews.Select(x => new ReviewView
            {
                OrderId = x.order_id,
                RestaurantName = Lookup(restaurantNames, x.restaurant_id),
                Rating = x.rating,
                Comment = x.comment ?? string.Empty,
                CreatedAt = x.created_at
            }).ToList();
        }

        private void AddReview_Click(object sender, RoutedEventArgs e)
        {
            var order = ReviewOrderBox.SelectedItem as ReviewOrderOption;
            if (!CurrentUserId.HasValue || order == null)
            {
                MessageBox.Show("Нет доставленных заказов без отзыва. Оставить отзыв можно после завершения доставки.", "Отзывы", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string comment = (ReviewCommentBox.Text ?? string.Empty).Trim();
            if (comment.Length > 1000) { MessageBox.Show("Комментарий не должен превышать 1000 символов."); return; }
            try
            {
                if (Core.Context.reviews.Any(x => x.order_id == order.OrderId))
                {
                    MessageBox.Show("Для этого заказа отзыв уже оставлен.");
                    LoadReviews();
                    return;
                }
                Core.Context.reviews.Add(new reviews
                {
                    user_id = CurrentUserId.Value,
                    restaurant_id = order.RestaurantId,
                    order_id = order.OrderId,
                    rating = Math.Max(1, 5 - ReviewRatingBox.SelectedIndex),
                    comment = comment.Length == 0 ? null : comment,
                    created_at = DateTime.Now
                });
                Core.Context.SaveChanges();
                ReviewCommentBox.Clear();
                LoadReviews();
                MessageBox.Show("Спасибо за отзыв!", "Отзывы", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Не удалось сохранить отзыв.\n\n" + ex.GetBaseException().Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private static string Lookup(Dictionary<int, string> values, int id)
        {
            string value;
            return values.TryGetValue(id, out value) ? value : string.Empty;
        }

        private sealed class RestaurantOption
        {
            public int? Id { get; private set; }
            public string Name { get; private set; }
            public RestaurantOption(int? id, string name) { Id = id; Name = name; }
        }
        private sealed class AddressOption
        {
            public int Id { get; private set; }
            public string AddressText { get; private set; }
            public AddressOption(int id, string text) { Id = id; AddressText = text; }
        }
        private sealed class DishView
        {
            public int Id { get; set; }
            public int RestaurantId { get; set; }
            public string RestaurantName { get; set; }
            public string CategoryName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
        }
        private sealed class CartItem
        {
            public int DishId { get; set; }
            public int RestaurantId { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal LineTotal { get { return Price * Quantity; } }
        }
        private sealed class OrderView
        {
            public int Id { get; set; }
            public string Restaurant { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; }
            public decimal TotalAmount { get; set; }
            public string Address { get; set; }
            public string PaymentMethod { get; set; }
        }
        private sealed class ReviewOrderOption
        {
            public int OrderId { get; private set; }
            public int RestaurantId { get; private set; }
            public string Title { get; private set; }
            public ReviewOrderOption(int orderId, int restaurantId, string title) { OrderId = orderId; RestaurantId = restaurantId; Title = title; }
        }
        private sealed class ReviewView
        {
            public int OrderId { get; set; }
            public string RestaurantName { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}

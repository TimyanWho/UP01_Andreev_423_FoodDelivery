using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class CourierPage : Page
    {
        private couriers _courier;
        private List<CourierDeliveryView> _rows = new List<CourierDeliveryView>();

        public CourierPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadDeliveries();
        }

        private couriers GetCourier()
        {
            if (!AppSession.CurrentUserId.HasValue) return null;
            return Core.Context.couriers.FirstOrDefault(x => x.user_id == AppSession.CurrentUserId.Value && x.is_active);
        }

        private void LoadDeliveries()
        {
            try
            {
                _courier = GetCourier();
                if (_courier == null)
                {
                    DeliveriesGrid.ItemsSource = null;
                    CourierInfoText.Text = "Для этой учётной записи не найден активный профиль курьера. Обратитесь к администратору, чтобы он добавил запись в таблицу couriers.";
                    return;
                }

                var deliveryRows = Core.Context.deliveries.ToList()
                    .Where(x => x.courier_id == _courier.id || (!x.courier_id.HasValue && x.status == "Ожидает курьера"))
                    .OrderBy(x => x.status == "Доставлена" ? 1 : 0).ThenBy(x => x.id).ToList();
                var orderIds = new HashSet<int>(deliveryRows.Select(x => x.order_id));
                var orderById = Core.Context.orders.ToList().Where(x => orderIds.Contains(x.id)).ToDictionary(x => x.id);
                var userIds = new HashSet<int>(orderById.Values.Select(x => x.user_id));
                var restaurantIds = new HashSet<int>(orderById.Values.Select(x => x.restaurant_id));
                var addressIds = new HashSet<int>(orderById.Values.Select(x => x.address_id));
                var names = Core.Context.users.ToList().Where(x => userIds.Contains(x.id)).ToDictionary(x => x.id, x => x.full_name);
                var restaurantNames = Core.Context.restaurants.ToList().Where(x => restaurantIds.Contains(x.id)).ToDictionary(x => x.id, x => x.name);
                var addresses = Core.Context.addresses.ToList().Where(x => addressIds.Contains(x.id)).ToDictionary(x => x.id, x => x.address_text);

                _rows = deliveryRows.Select(d =>
                {
                    orders order;
                    if (!orderById.TryGetValue(d.order_id, out order)) return null;
                    return new CourierDeliveryView
                    {
                        DeliveryId = d.id,
                        OrderId = order.id,
                        Customer = Lookup(names, order.user_id),
                        Restaurant = Lookup(restaurantNames, order.restaurant_id),
                        Address = Lookup(addresses, order.address_id),
                        DeliveryStatus = d.status,
                        OrderStatus = order.status,
                        Amount = order.total_amount,
                        CourierId = d.courier_id
                    };
                }).Where(x => x != null).ToList();

                DeliveriesGrid.ItemsSource = null;
                DeliveriesGrid.ItemsSource = _rows;
                CourierInfoText.Text = "Ваш профиль курьера №" + _courier.id + ". Свободных доставок: " + _rows.Count(x => !x.CourierId.HasValue) +
                    "; ваших активных и завершённых: " + _rows.Count(x => x.CourierId == _courier.id) + ".";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить доставки.\n\n" + ex.GetBaseException().Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) { LoadDeliveries(); }

        private void TakeDelivery_Click(object sender, RoutedEventArgs e)
        {
            var selected = DeliveriesGrid.SelectedItem as CourierDeliveryView;
            if (selected == null) { MessageBox.Show("Выберите доставку."); return; }
            if (!CanTake(selected)) { MessageBox.Show("Эта доставка уже назначена другому курьеру или не ожидает назначения."); return; }
            UpdateDelivery(selected.DeliveryId, delivery =>
            {
                if (delivery.courier_id.HasValue || delivery.status != "Ожидает курьера") throw new InvalidOperationException("Доставка уже недоступна.");
                delivery.courier_id = _courier.id;
                delivery.status = "Назначена";
                delivery.assigned_at = DateTime.Now;
                var order = Core.Context.orders.FirstOrDefault(x => x.id == delivery.order_id);
                if (order != null) order.status = "Передан курьеру";
            });
        }

        private void PickupDelivery_Click(object sender, RoutedEventArgs e)
        {
            var selected = DeliveriesGrid.SelectedItem as CourierDeliveryView;
            if (selected == null) { MessageBox.Show("Выберите доставку."); return; }
            if (selected.CourierId != _courier?.id || selected.DeliveryStatus != "Назначена")
            {
                MessageBox.Show("Отметить получение можно только у назначенной вам доставки.");
                return;
            }
            UpdateDelivery(selected.DeliveryId, delivery =>
            {
                if (delivery.courier_id != _courier.id || delivery.status != "Назначена") throw new InvalidOperationException("Статус доставки уже изменился.");
                delivery.status = "Забрана";
                delivery.picked_up_at = DateTime.Now;
                var order = Core.Context.orders.FirstOrDefault(x => x.id == delivery.order_id);
                if (order != null) order.status = "Доставляется";
            });
        }

        private void CompleteDelivery_Click(object sender, RoutedEventArgs e)
        {
            var selected = DeliveriesGrid.SelectedItem as CourierDeliveryView;
            if (selected == null) { MessageBox.Show("Выберите доставку."); return; }
            if (selected.CourierId != _courier?.id || (selected.DeliveryStatus != "Забрана" && selected.DeliveryStatus != "В пути"))
            {
                MessageBox.Show("Завершить можно только доставку, которую вы забрали в ресторане.");
                return;
            }
            if (MessageBox.Show("Подтвердить, что заказ доставлен клиенту?", "Завершение доставки", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            UpdateDelivery(selected.DeliveryId, delivery =>
            {
                if (delivery.courier_id != _courier.id || (delivery.status != "Забрана" && delivery.status != "В пути"))
                    throw new InvalidOperationException("Статус доставки уже изменился.");
                delivery.status = "Доставлена";
                delivery.delivered_at = DateTime.Now;
                var order = Core.Context.orders.FirstOrDefault(x => x.id == delivery.order_id);
                if (order != null) order.status = "Доставлен";
            });
        }

        private bool CanTake(CourierDeliveryView selected)
        {
            return _courier != null && !selected.CourierId.HasValue && selected.DeliveryStatus == "Ожидает курьера";
        }

        private void UpdateDelivery(int deliveryId, Action<deliveries> update)
        {
            try
            {
                if (_courier == null) _courier = GetCourier();
                if (_courier == null) { MessageBox.Show("Активный профиль курьера не найден."); return; }
                var delivery = Core.Context.deliveries.FirstOrDefault(x => x.id == deliveryId);
                if (delivery == null) { MessageBox.Show("Доставка не найдена. Обновите список."); return; }
                update(delivery);
                Core.Context.SaveChanges();
                LoadDeliveries();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось изменить статус доставки. Обновите список и попробуйте ещё раз.\n\n" + ex.GetBaseException().Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Lookup(Dictionary<int, string> values, int id)
        {
            string value;
            return values.TryGetValue(id, out value) ? value : string.Empty;
        }

        private sealed class CourierDeliveryView
        {
            public int DeliveryId { get; set; }
            public int OrderId { get; set; }
            public string Customer { get; set; }
            public string Restaurant { get; set; }
            public string Address { get; set; }
            public string DeliveryStatus { get; set; }
            public string OrderStatus { get; set; }
            public decimal Amount { get; set; }
            public int? CourierId { get; set; }
        }
    }
}

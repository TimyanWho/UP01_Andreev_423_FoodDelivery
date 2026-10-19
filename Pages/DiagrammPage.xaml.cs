using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;

namespace UP01_Andreev_423_FoodDelivery.Pages
{
    public partial class DiagrammPage : Page
    {
        private readonly Chart _chart;
        private List<OrderReport> _orders = new List<OrderReport>();
        private bool _initializing;

        public DiagrammPage()
        {
            InitializeComponent();
            _chart = new Chart { Dock = System.Windows.Forms.DockStyle.Fill };
            _chart.ChartAreas.Add(new ChartArea("Main"));
            _chart.Legends.Add(new Legend("Legend"));
            ChartHost.Child = _chart;

            _initializing = true;
            CmbDiagram.ItemsSource = new[] { "Столбчатая", "Круговая", "Линейная" };
            CmbDiagram.SelectedIndex = 0;
            try
            {
                LoadReportRows();

                CmbRestaurant.ItemsSource = BuildRestaurantOptions();
                CmbRestaurant.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные для диаграммы и отчёта.\n\n" + ex.GetBaseException().Message,
                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _initializing = false;
            }
            UpdateChart(null, null);
        }

        private List<RestaurantOption> BuildRestaurantOptions()
        {
            var list = new List<RestaurantOption> { new RestaurantOption(null, "Все рестораны") };
            var restaurants = Core.Context.restaurants.OrderBy(x => x.name).ToList();
            list.AddRange(restaurants.Select(x => new RestaurantOption(x.id, x.name)));
            return list;
        }

        private void LoadReportRows()
        {
            var users = Core.Context.users.ToList().ToDictionary(x => x.id, x => x.full_name);
            var restaurants = Core.Context.restaurants.ToList().ToDictionary(x => x.id, x => x.name);
            var addresses = Core.Context.addresses.ToList().ToDictionary(x => x.id, x => x.address_text);

            _orders = Core.Context.orders.ToList().Select(x => new OrderReport
            {
                Id = x.id,
                UserName = Find(users, x.user_id),
                RestaurantId = x.restaurant_id,
                RestaurantName = Find(restaurants, x.restaurant_id),
                Address = Find(addresses, x.address_id),
                Status = x.status,
                PaymentMethod = x.payment_method,
                CreatedAt = x.created_at,
                TotalAmount = x.total_amount
            }).OrderBy(x => x.RestaurantName).ThenBy(x => x.CreatedAt).ToList();
        }

        private void UpdateChart(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing || _chart == null || CmbRestaurant == null || CmbDiagram == null) return;
            try
            {
                List<OrderReport> filtered = GetFilteredOrders();
                var grouped = filtered.GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? "Без статуса" : x.Status)
                    .Select(g => new { Status = g.Key, Total = g.Sum(x => x.TotalAmount), Count = g.Count() })
                    .OrderBy(x => x.Status).ToList();

                _chart.Series.Clear();
                _chart.Titles.Clear();
                var option = CmbRestaurant.SelectedItem as RestaurantOption;
                string title = option == null || !option.Id.HasValue
                    ? "Заказы по статусам"
                    : "Заказы ресторана «" + option.Name + "»";
                _chart.Titles.Add(title);
                _chart.ChartAreas[0].AxisX.Interval = 1;

                if (grouped.Count == 0)
                {
                    ReportSummary.Text = "Нет заказов для выбранного ресторана.";
                    return;
                }

                var series = new Series("Сумма заказов") { IsValueShownAsLabel = true };
                string type = Convert.ToString(CmbDiagram.SelectedItem);
                series.ChartType = type == "Круговая" ? SeriesChartType.Pie : type == "Линейная" ? SeriesChartType.Line : SeriesChartType.Column;
                foreach (var item in grouped) series.Points.AddXY(item.Status, item.Total);
                _chart.Series.Add(series);
                ReportSummary.Text = "Заказов: " + filtered.Count + "    Общая сумма: " + filtered.Sum(x => x.TotalAmount).ToString("N2") + " ₽";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось построить диаграмму.\n\n" + ex.GetBaseException().Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private List<OrderReport> GetFilteredOrders()
        {
            var option = CmbRestaurant.SelectedItem as RestaurantOption;
            if (option == null || !option.Id.HasValue) return _orders.ToList();
            return _orders.Where(x => x.RestaurantId == option.Id.Value).ToList();
        }

        private string GetReportFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FoodDeliveryReports");
            Directory.CreateDirectory(folder);
            return folder;
        }

        private void ExportWord_Click(object sender, RoutedEventArgs e)
        {
            dynamic word = null;
            dynamic document = null;
            bool leaveOpen = false;
            try
            {
                List<OrderReport> orders = GetFilteredOrders();
                if (orders.Count == 0) { MessageBox.Show("Нет заказов для экспорта с выбранным фильтром."); return; }

                Type wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null) throw new InvalidOperationException("Microsoft Word не установлен или не зарегистрирован в Windows.");
                string stem = Path.Combine(GetReportFolder(), "FoodGo_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                word = Activator.CreateInstance(wordType);
                word.Visible = false;
                document = word.Documents.Add();

                dynamic title = document.Paragraphs.Add();
                title.Range.Text = "Отчёт по заказам сервиса доставки еды FoodGo";
                title.Range.Font.Size = 18;
                title.Range.Font.Bold = 1;
                title.Range.ParagraphFormat.Alignment = 1; 
                title.Range.InsertParagraphAfter();

                dynamic date = document.Paragraphs.Add();
                date.Range.Text = "Сформирован: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                date.Range.InsertParagraphAfter();

                dynamic range = document.Paragraphs.Add().Range;
                dynamic table = document.Tables.Add(range, orders.Count + 1, 7);
                table.Borders.InsideLineStyle = 1; 
                table.Borders.OutsideLineStyle = 1;
                string[] headers = { "№", "Дата", "Клиент", "Ресторан", "Статус", "Сумма", "Адрес" };
                for (int c = 0; c < headers.Length; c++)
                {
                    table.Cell(1, c + 1).Range.Text = headers[c];
                    table.Cell(1, c + 1).Range.Bold = 1;
                }
                for (int i = 0; i < orders.Count; i++)
                {
                    OrderReport x = orders[i];
                    int r = i + 2;
                    table.Cell(r, 1).Range.Text = x.Id.ToString();
                    table.Cell(r, 2).Range.Text = x.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    table.Cell(r, 3).Range.Text = Safe(x.UserName);
                    table.Cell(r, 4).Range.Text = Safe(x.RestaurantName);
                    table.Cell(r, 5).Range.Text = Safe(x.Status);
                    table.Cell(r, 6).Range.Text = x.TotalAmount.ToString("N2");
                    table.Cell(r, 7).Range.Text = Safe(x.Address);
                }

                dynamic summary = document.Paragraphs.Add();
                summary.Range.Text = "Всего заказов: " + orders.Count + ". Общая сумма: " + orders.Sum(x => x.TotalAmount).ToString("N2") + " ₽.";
                summary.Range.Font.Bold = 1;
                document.SaveAs2(stem + ".docx", 12); 
                document.ExportAsFixedFormat(stem + ".pdf", 17); 
                word.Visible = true;
                leaveOpen = true;
                MessageBox.Show("Отчёты созданы:\n" + stem + ".docx\n" + stem + ".pdf", "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (document != null) { try { document.Close(false); } catch { } }
                if (word != null) { try { word.Quit(); } catch { } }
                MessageBox.Show("Не удалось создать DOCX/PDF. Для этого экспорта требуется установленный Microsoft Word.\n\n" + ex.GetBaseException().Message,
                    "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                
                if (!leaveOpen && document != null) { try { document = null; } catch { } }
                if (!leaveOpen && word != null) { try { word = null; } catch { } }
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            dynamic excel = null;
            dynamic workbook = null;
            bool leaveOpen = false;
            try
            {
                List<OrderReport> orders = GetFilteredOrders();
                if (orders.Count == 0) { MessageBox.Show("Нет заказов для экспорта с выбранным фильтром."); return; }

                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null) throw new InvalidOperationException("Microsoft Excel не установлен или не зарегистрирован в Windows.");
                string path = Path.Combine(GetReportFolder(), "FoodGo_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
                excel = Activator.CreateInstance(excelType);
                excel.Visible = false;
                excel.SheetsInNewWorkbook = 2;
                workbook = excel.Workbooks.Add();
                dynamic sheet = workbook.Worksheets[1];
                sheet.Name = "Заказы";

                string[] headers = { "ID", "Дата", "Клиент", "Ресторан", "Статус", "Сумма", "Адрес", "Оплата" };
                for (int c = 0; c < headers.Length; c++) sheet.Cells[1, c + 1] = headers[c];
                sheet.Range[sheet.Cells[1, 1], sheet.Cells[1, headers.Length]].Font.Bold = true;
                int row = 2;
                foreach (OrderReport x in orders)
                {
                    sheet.Cells[row, 1] = x.Id;
                    sheet.Cells[row, 2] = x.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                    sheet.Cells[row, 3] = Safe(x.UserName);
                    sheet.Cells[row, 4] = Safe(x.RestaurantName);
                    sheet.Cells[row, 5] = Safe(x.Status);
                    sheet.Cells[row, 6] = (double)x.TotalAmount;
                    sheet.Cells[row, 7] = Safe(x.Address);
                    sheet.Cells[row, 8] = Safe(x.PaymentMethod);
                    row++;
                }
                sheet.Columns.AutoFit();

                dynamic summary = workbook.Worksheets[2];
                summary.Name = "Общий итог";
                summary.Cells[1, 1] = "Показатель";
                summary.Cells[1, 2] = "Значение";
                summary.Cells[1, 1].Font.Bold = true;
                summary.Cells[1, 2].Font.Bold = true;
                summary.Cells[2, 1] = "Количество заказов";
                summary.Cells[2, 2] = orders.Count;
                summary.Cells[3, 1] = "Общая сумма";
                summary.Cells[3, 2] = (double)orders.Sum(x => x.TotalAmount);
                summary.Cells[4, 1] = "Средний чек";
                summary.Cells[4, 2] = (double)orders.Average(x => x.TotalAmount);
                summary.Columns.AutoFit();
                workbook.SaveAs(path, 51); 
                excel.Visible = true;
                leaveOpen = true;
                MessageBox.Show("Excel-файл сохранён:\n" + path, "Экспорт завершён", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (workbook != null) { try { workbook.Close(false); } catch { } }
                if (excel != null) { try { excel.Quit(); } catch { } }
                MessageBox.Show("Не удалось создать Excel-файл. Для этого экспорта требуется установленный Microsoft Excel.\n\n" + ex.GetBaseException().Message,
                    "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (!leaveOpen && workbook != null) { try { workbook = null; } catch { } }
                if (!leaveOpen && excel != null) { try { excel = null; } catch { } }
            }
        }

        private static string Safe(string value) { return value ?? string.Empty; }
        private static string Find(Dictionary<int, string> values, int id)
        {
            string value;
            return values.TryGetValue(id, out value) ? value : string.Empty;
        }

        private sealed class RestaurantOption
        {
            public int? Id { get; private set; }
            public string Name { get; private set; }
            public RestaurantOption(int? id, string name) { Id = id; Name = name; }
            public override string ToString() { return Name; }
        }

        private sealed class OrderReport
        {
            public int Id { get; set; }
            public int RestaurantId { get; set; }
            public string UserName { get; set; }
            public string RestaurantName { get; set; }
            public string Address { get; set; }
            public string Status { get; set; }
            public string PaymentMethod { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
}

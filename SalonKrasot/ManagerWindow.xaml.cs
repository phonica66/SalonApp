using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SalonApp
{
    public partial class ManagerWindow : Window
    {
        private string currentUsername;
        private int currentUserId;
        private int unreadNotificationsCount = 0;

        public ManagerWindow(string username)
        {
            InitializeComponent();
            currentUsername = username;
            currentUserId = WpfDatabase.GetUserId(username);

            LoadAllData();
        }

        private void LoadAllData()
        {
            try
            {
                LoadNotifications();
                LoadPurchases();
                LoadStatistics();
                LoadDropdownData();

                TxtStatus.Text = "Все данные загружены успешно";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Ошибка загрузки данных";
            }
        }

        // =============== УВЕДОМЛЕНИЯ ===============

        private void LoadNotifications()
        {
            try
            {
                var notifications = WpfDatabase.GetUnreadNotifications(currentUserId);
                DgNotifications.ItemsSource = notifications.DefaultView;

                unreadNotificationsCount = notifications.Rows.Count;
                TxtNotificationCount.Text = $"📧 {unreadNotificationsCount} непрочитанных";

                if (unreadNotificationsCount > 0)
                {
                    TxtStatus.Text = $"У вас {unreadNotificationsCount} новых уведомлений!";

                    // Показываем первым делом вкладку уведомлений, если есть новые
                    var tabControl = (TabControl)this.FindName("TabControl"); // Нужно будет добавить x:Name в XAML
                    if (tabControl != null && tabControl.Items.Count > 0)
                    {
                        tabControl.SelectedIndex = 0; // Выбираем вкладку уведомлений
                    }
                }
                else
                {
                    TxtStatus.Text = "Новых уведомлений нет";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshNotifications_Click(object sender, RoutedEventArgs e)
        {
            LoadNotifications();
        }

        private void BtnMarkAsRead_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DgNotifications.SelectedItem == null)
                {
                    MessageBox.Show("Выберите уведомление для отметки как прочитанное!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedRow = DgNotifications.SelectedItem as DataRowView;
                var notificationId = Convert.ToInt32(selectedRow["ID"]);

                if (WpfDatabase.MarkNotificationAsRead(notificationId))
                {
                    MessageBox.Show("Уведомление отмечено как прочитанное", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadNotifications(); // Перезагружаем список
                    TxtStatus.Text = "Уведомление прочитано";
                }
                else
                {
                    MessageBox.Show("Ошибка отметки уведомления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCheckNotifications_Click(object sender, RoutedEventArgs e)
        {
            LoadNotifications();

            if (unreadNotificationsCount > 0)
            {
                MessageBox.Show($"У вас {unreadNotificationsCount} новых уведомлений!\n\nПерейдите на вкладку 'Уведомления' для просмотра.", 
                               "Новые уведомления", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Новых уведомлений нет", "Уведомления", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // =============== ЗАКУПКИ ===============

        private void LoadPurchases()
        {
            try
            {
                var purchases = WpfDatabase.GetPurchases();
                DgPurchases.ItemsSource = purchases.DefaultView;
                TxtStatus.Text = $"Загружено {purchases.Rows.Count} закупок";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки закупок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddPurchase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CmbMaterials.SelectedValue == null)
                {
                    MessageBox.Show("Выберите материал!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var quantityText = TxtQuantity.Text.Trim();
                var costText = TxtCostPerUnit.Text.Trim();
                var supplier = TxtSupplier.Text.Trim();

                if (string.IsNullOrEmpty(quantityText) || string.IsNullOrEmpty(costText) || string.IsNullOrEmpty(supplier))
                {
                    MessageBox.Show("Заполните все обязательные поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtQuantity.Focus();
                    return;
                }

                if (!decimal.TryParse(costText, out decimal costPerUnit) || costPerUnit <= 0)
                {
                    MessageBox.Show("Введите корректную цену!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtCostPerUnit.Focus();
                    return;
                }

                var materialId = Convert.ToInt32(CmbMaterials.SelectedValue);
                var invoiceNumber = TxtInvoiceNumber.Text.Trim();
                var notes = TxtNotes.Text.Trim();

                var totalCost = quantity * costPerUnit;
                var confirmResult = MessageBox.Show($"Подтвердить закупку:\n\n" +
                                                   $"Материал: {CmbMaterials.Text}\n" +
                                                   $"Количество: {quantity}\n" +
                                                   $"Цена за единицу: {costPerUnit:N2} ₽\n" +
                                                   $"Общая стоимость: {totalCost:N2} ₽\n" +
                                                   $"Поставщик: {supplier}", 
                                                   "Подтверждение закупки", 
                                                   MessageBoxButton.YesNo, 
                                                   MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    if (WpfDatabase.AddPurchase(materialId, quantity, costPerUnit, supplier, currentUserId, invoiceNumber, notes))
                    {
                        MessageBox.Show("Закупка успешно добавлена!\nОстатки материала обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Очищаем форму
                        CmbMaterials.SelectedIndex = -1;
                        TxtQuantity.Clear();
                        TxtCostPerUnit.Clear();
                        TxtSupplier.Clear();
                        TxtInvoiceNumber.Clear();
                        TxtNotes.Clear();

                        // Обновляем данные
                        LoadPurchases();
                        LoadStatistics();
                        LoadDropdownData(); // Обновляем список материалов с новыми остатками

                        TxtStatus.Text = $"Закупка на сумму {totalCost:N2} ₽ добавлена";
                    }
                    else
                    {
                        MessageBox.Show("Ошибка добавления закупки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления закупки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshPurchases_Click(object sender, RoutedEventArgs e)
        {
            LoadPurchases();
            LoadDropdownData(); // Обновляем также данные для выпадающих списков
        }

        // =============== СТАТИСТИКА ===============

        private void LoadStatistics()
        {
            try
            {
                var stats = WpfDatabase.GetFinancialStatistics();

                TxtTotalIncome.Text = $"{stats.TotalIncome:N0} ₽";
                TxtTotalExpenses.Text = $"{stats.TotalExpenses:N0} ₽";
                TxtProfit.Text = $"{stats.Profit:N0} ₽";

                TxtClientsCount.Text = stats.TotalClients.ToString();
                TxtAppointmentsCount.Text = stats.TotalAppointments.ToString();
                TxtCompletedCount.Text = stats.CompletedAppointments.ToString();

                // Меняем цвет прибыли в зависимости от значения
                if (stats.Profit > 0)
                {
                    TxtProfit.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)); // Зеленый
                }
                else if (stats.Profit < 0)
                {
                    TxtProfit.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Красный
                }
                else
                {
                    TxtProfit.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 39, 176)); // Фиолетовый
                }

                TxtStatus.Text = "Статистика обновлена";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshStatistics_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        // =============== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===============

        private void LoadDropdownData()
        {
            try
            {
                // Загружаем материалы
                var materials = WpfDatabase.GetMaterialsForDropdown();
                CmbMaterials.ItemsSource = materials;

                TxtStatus.Text = "Данные для списков обновлены";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных для списков: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =============== СОБЫТИЯ ===============

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Выйти из системы?", "Подтверждение", 
                                        MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = $"Добро пожаловать, {currentUsername}! Система готова к работе.";

            // Если есть непрочитанные уведомления, покажем предупреждение
            if (unreadNotificationsCount > 0)
            {
                MessageBox.Show($"У вас {unreadNotificationsCount} новых уведомлений от администратора!\n\n" +
                               "Проверьте вкладку 'Уведомления' для просмотра сообщений о заканчивающихся материалах.", 
                               "Внимание: новые уведомления!", 
                               MessageBoxButton.OK, 
                               MessageBoxImage.Information);
            }
        }
    }
}
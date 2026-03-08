using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SalonApp
{
    /// <summary>
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private string currentUsername;
        private int currentUserId;
        private int managerId;

        // Коллекции для привязки данных
        private ObservableCollection<ClientItem> clientItems;
        private ObservableCollection<AppointmentItem> appointmentItems;
        private ObservableCollection<MaterialItem> materialItems;
        private ObservableCollection<ComboItem> clientComboItems;
        private ObservableCollection<ComboItem> serviceComboItems;
        private ObservableCollection<ComboItem> materialComboItems;

        public AdminWindow(string username)
        {
            InitializeComponent();
            currentUsername = username;
            currentUserId = GetUserId(username);
            managerId = GetManagerId();

            InitializeCollections();
            LoadAllData();

            // Устанавливаем дату на завтра по умолчанию
            DpAppointmentDate.SelectedDate = DateTime.Today.AddDays(1);
        }

        private void InitializeCollections()
        {
            clientItems = new ObservableCollection<ClientItem>();
            appointmentItems = new ObservableCollection<AppointmentItem>();
            materialItems = new ObservableCollection<MaterialItem>();
            clientComboItems = new ObservableCollection<ComboItem>();
            serviceComboItems = new ObservableCollection<ComboItem>();
            materialComboItems = new ObservableCollection<ComboItem>();

            // Привязываем коллекции к элементам интерфейса
            DgClients.ItemsSource = clientItems;
            DgAppointments.ItemsSource = appointmentItems;
            DgMaterials.ItemsSource = materialItems;
            CmbClients.ItemsSource = clientComboItems;
            CmbServices.ItemsSource = serviceComboItems;
            CmbMaterialsForNotification.ItemsSource = materialComboItems;
        }

        // =============== ЗАГРУЗКА ДАННЫХ ===============

        private void LoadAllData()
        {
            try
            {
                LoadClients();
                LoadAppointments();
                LoadMaterials();
                LoadComboBoxData();
                LoadStatistics();

                TxtStatus.Text = "Все данные успешно загружены";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Ошибка загрузки данных";
            }
        }

        private void LoadClients()
        {
            try
            {
                clientItems.Clear();
                var clients = WpfDatabase.GetClients();

                foreach (DataRow row in clients.Rows)
                {
                    clientItems.Add(new ClientItem
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        Name = row["Имя"].ToString(),
                        Phone = row["Телефон"].ToString(),
                        Email = row["Email"].ToString(),
                        CreatedDate = Convert.ToDateTime(row["Дата регистрации"]).ToString("dd.MM.yyyy")
                    });
                }

                TxtStatus.Text = $"Загружено {clientItems.Count} клиентов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                appointmentItems.Clear();
                var appointments = WpfDatabase.GetAppointments();

                foreach (DataRow row in appointments.Rows)
                {
                    appointmentItems.Add(new AppointmentItem
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        ClientName = row["Клиент"].ToString(),
                        ClientPhone = row["Телефон"].ToString(),
                        ServiceName = row["Услуга"].ToString(),
                        Price = row["Цена"].ToString(),
                        DateTimeString = Convert.ToDateTime(row["Дата и время"]).ToString("dd.MM.yyyy HH:mm"),
                        Status = row["Статус"].ToString()
                    });
                }

                TxtStatus.Text = $"Загружено {appointmentItems.Count} записей";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMaterials()
        {
            try
            {
                materialItems.Clear();
                var materials = WpfDatabase.GetMaterials();

                foreach (DataRow row in materials.Rows)
                {
                    materialItems.Add(new MaterialItem
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        Name = row["Материал"].ToString(),
                        CurrentStock = Convert.ToInt32(row["Остаток"]),
                        MinStock = Convert.ToInt32(row["Мин. остаток"]),
                        Unit = row["Единица"].ToString(),
                        Category = row["Категория"].ToString(),
                        Supplier = row["Поставщик"].ToString(),
                        CostPerUnit = row["Цена за ед."].ToString(),
                        Status = row["Статус"].ToString()
                    });
                }

                TxtStatus.Text = $"Загружено {materialItems.Count} материалов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки материалов: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                // Загружаем клиентов для ComboBox
                clientComboItems.Clear();
                var clientsForCombo = WpfDatabase.GetClientsForDropdown();
                foreach (var item in clientsForCombo)
                {
                    clientComboItems.Add(new ComboItem { ID = item.Key, Display = item.Value });
                }

                // Загружаем услуги для ComboBox
                serviceComboItems.Clear();
                var servicesForCombo = WpfDatabase.GetServicesForDropdown();
                foreach (var item in servicesForCombo)
                {
                    serviceComboItems.Add(new ComboItem { ID = item.Key, Display = item.Value });
                }

                // Загружаем материалы для ComboBox
                materialComboItems.Clear();
                var materialsForCombo = WpfDatabase.GetMaterialsForDropdown();
                foreach (var item in materialsForCombo)
                {
                    materialComboItems.Add(new ComboItem { ID = item.Key, Display = item.Value });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных для списков: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                var stats = WpfDatabase.GetFinancialStatistics();

                TxtTotalIncome.Text = $"{stats.TotalIncome:N0} ₽";
                TxtCompletedAppointments.Text = stats.CompletedAppointments.ToString();
                TxtTotalClients.Text = stats.TotalClients.ToString();

                TxtStatus.Text = "Статистика обновлена";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =============== КЛИЕНТЫ ===============

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = TxtClientName.Text.Trim();
                var phone = TxtClientPhone.Text.Trim();
                var email = TxtClientEmail.Text.Trim();

                // Проверяем имя
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Введите имя клиента! Это поле обязательно.", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtClientName.Focus();
                    return;
                }

                // Проверяем телефон
                if (string.IsNullOrWhiteSpace(phone) || phone == "8" || phone == "")
                {
                    MessageBox.Show("Введите телефон клиента! Это поле обязательно.", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtClientPhone.Focus();
                    return;
                }

                // Проверяем email
                if (string.IsNullOrWhiteSpace(email))
                {
                    MessageBox.Show("Введите email клиента! Это поле обязательно.", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtClientEmail.Focus();
                    return;
                }

                // Валидация телефона
                string validatedPhone = phone;
                try
                {
                    WpfDatabase.ValidatePhone(ref validatedPhone);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в номере телефона:\n{ex.Message}", "Некорректный телефон",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtClientPhone.Focus();
                    return;
                }

                // Валидация email
                try
                {
                    WpfDatabase.ValidateEmail(ref email);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка в email:\n{ex.Message}", "Некорректный email",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtClientEmail.Focus();
                    return;
                }

                // Попытка добавить клиента
                if (WpfDatabase.AddClient(name, validatedPhone, email))
                {
                    MessageBox.Show($"Клиент успешно добавлен!\n\nИмя: {name}\nТелефон: {validatedPhone}\nEmail: {email}",
                                   "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очищаем поля
                    TxtClientName.Clear();
                    TxtClientPhone.Text = "8";
                    TxtClientEmail.Clear();

                    // Обновляем данные
                    LoadClients();
                    LoadComboBoxData();

                    TxtStatus.Text = $"Клиент '{name}' успешно добавлен";
                }
                else
                {
                    MessageBox.Show("Ошибка добавления клиента.\nВозможно, такой телефон или email уже существует.",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неизвестная ошибка: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedClient = DgClients.SelectedItem as ClientItem;
                if (selectedClient == null)
                {
                    MessageBox.Show("Выберите клиента для удаления!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirmResult = MessageBox.Show(
                    $"Удалить клиента '{selectedClient.Name}'?\n\nВнимание: это действие нельзя отменить!",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    // Запрашиваем причину удаления
                    var reasonDialog = new ReasonInputDialogR("Укажите причину удаления клиента:");
                    if (reasonDialog.ShowDialog() == true)
                    {
                        var reason = reasonDialog.Reason;

                        if (string.IsNullOrWhiteSpace(reason))
                        {
                            MessageBox.Show("Причина удаления обязательна!", "Внимание",
                                           MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (WpfDatabase.DeleteClientWithReason(selectedClient.ID, reason, currentUserId))
                        {
                            MessageBox.Show($"Клиент '{selectedClient.Name}' удален.\nПричина: {reason}",
                                           "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadClients();
                            LoadComboBoxData();
                            TxtStatus.Text = $"Клиент '{selectedClient.Name}' удален";
                        }
                        else
                        {
                            MessageBox.Show("Ошибка удаления клиента!", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =============== ЗАПИСИ ===============

        private void BtnAddAppointment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedClient = CmbClients.SelectedItem as ComboItem;
                var selectedService = CmbServices.SelectedItem as ComboItem;

                if (selectedClient == null)
                {
                    MessageBox.Show("Выберите клиента!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (selectedService == null)
                {
                    MessageBox.Show("Выберите услугу!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DpAppointmentDate.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату записи!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var timeText = TxtAppointmentTime.Text.Trim();
                if (string.IsNullOrEmpty(timeText))
                {
                    MessageBox.Show("Введите время записи!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(timeText, out TimeSpan appointmentTime))
                {
                    MessageBox.Show("Неверный формат времени! Используйте формат ЧЧ:ММ (например, 14:30)",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var appointmentDateTime = DpAppointmentDate.SelectedDate.Value.Add(appointmentTime);

                if (appointmentDateTime <= DateTime.Now)
                {
                    MessageBox.Show("Время записи должно быть в будущем!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (appointmentDateTime > DateTime.Now.AddMonths(2))
                {
                    MessageBox.Show("Запись можно сделать не более чем на 2 месяца вперёд!",
                                   "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (WpfDatabase.AddAppointment(selectedClient.ID, selectedService.ID, appointmentDateTime))
                {
                    MessageBox.Show("Запись успешно создана!", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    CmbClients.SelectedIndex = -1;
                    CmbServices.SelectedIndex = -1;
                    DpAppointmentDate.SelectedDate = DateTime.Today.AddDays(1);
                    TxtAppointmentTime.Text = "10:00";

                    LoadAppointments();

                    TxtStatus.Text = "Новая запись создана";
                }
                else
                {
                    MessageBox.Show("Ошибка создания записи!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания записи: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CmbClients.IsDropDownOpen == false)
                CmbClients.IsDropDownOpen = true;

            var textBox = (TextBox)e.OriginalSource;
            string filter = textBox.Text.Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(filter)
                ? clientComboItems
                : new ObservableCollection<ComboItem>(
                    clientComboItems.Where(c => c.Display.ToLower().Contains(filter))
                );

            CmbClients.ItemsSource = filtered;

            // Сохраняем введённый текст
            CmbClients.Text = textBox.Text;
            textBox.SelectionStart = textBox.Text.Length;
        }

        private async void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedAppointment = DgAppointments.SelectedItem as AppointmentItem;
                if (selectedAppointment == null)
                {
                    MessageBox.Show("Выберите запись для изменения статуса!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var statuses = new[] { "Новая", "Подтверждена", "Выполнена", "Отменена" };
                var statusDialog = new StatusSelectionDialog(statuses, selectedAppointment.Status);
                if (statusDialog.ShowDialog() != true) return;

                var newStatus = statusDialog.SelectedStatus;

                DateTime? appointmentDate = null;
                TimeSpan? appointmentTime = null;
                string cancellationReason = null;

                // ПОДТВЕРЖДЕНА — берём из DateTime
                if (newStatus == "Подтверждена")
                {
                    if (string.IsNullOrWhiteSpace(selectedAppointment.DateTimeString) ||
                        !DateTime.TryParse(selectedAppointment.DateTimeString, out var dt))
                    {
                        MessageBox.Show("Некорректная дата/время в записи!", "Ошибка",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    appointmentDate = dt.Date;
                    appointmentTime = dt.TimeOfDay;
                }
                // ОТМЕНЕНА — запрашиваем причину
                else if (newStatus == "Отменена")
                {
                    var reasonDialog = new CancellationReasonDialog();
                    if (reasonDialog.ShowDialog() != true)
                    {
                        MessageBox.Show("Отмена отменена.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    cancellationReason = reasonDialog.Reason;
                    if (string.IsNullOrWhiteSpace(cancellationReason))
                    {
                        MessageBox.Show("Укажите причину отмены!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Обновляем в локальной БД
                if (!WpfDatabase.UpdateAppointmentStatus(selectedAppointment.ID, newStatus))
                {
                    MessageBox.Show("Ошибка изменения статуса в базе данных!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Отправляем на сервер
                await ServerNotifier.NotifyAppointmentStatusAsync(
                    appointmentId: selectedAppointment.ID,
                    newStatus: newStatus switch
                    {
                        "Подтверждена" => "Подтверждена",
                        "Отменена" => "Отменена",
                        "Выполнена" => "Выполнена",
                        "Новая" => "new",
                        _ => newStatus
                    },
                    appointmentDate: appointmentDate,
                    appointmentTime: appointmentTime,
                    cancellationReason: cancellationReason
                );

                // Обновляем UI
                MessageBox.Show($"Статус изменён на '{newStatus}'", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);

                LoadAppointments();
                LoadStatistics();
                TxtStatus.Text = $"Статус записи изменён на '{newStatus}'";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =============== МАТЕРИАЛЫ ===============

        private void BtnSendNotification_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMaterial = CmbMaterialsForNotification.SelectedItem as ComboItem;
                if (selectedMaterial == null)
                {
                    MessageBox.Show("Выберите материал!", "Внимание",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (managerId == -1)
                {
                    MessageBox.Show("Менеджер не найден в системе!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var customMessage = TxtNotificationMessage.Text.Trim();

                if (WpfDatabase.SendLowStockNotification(selectedMaterial.ID, currentUserId, managerId, customMessage))
                {
                    MessageBox.Show("Уведомление отправлено менеджеру!", "Успех",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    // Сбрасываем форму
                    CmbMaterialsForNotification.SelectedIndex = -1;
                    TxtNotificationMessage.Text = "Срочно нужна закупка этого материала!";

                    TxtStatus.Text = "Уведомление отправлено менеджеру";
                }
                else
                {
                    MessageBox.Show("Ошибка отправки уведомления!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки уведомления: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =============== ОБНОВЛЕНИЕ ДАННЫХ ===============

        private void BtnRefreshClients_Click(object sender, RoutedEventArgs e)
        {
            LoadClients();
            LoadComboBoxData();
        }

        private void BtnRefreshAppointments_Click(object sender, RoutedEventArgs e)
        {
            LoadAppointments();
        }

        private void BtnRefreshMaterials_Click(object sender, RoutedEventArgs e)
        {
            LoadMaterials();
            LoadComboBoxData();
        }

        private void BtnRefreshStatistics_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

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

        // =============== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===============

        private int GetUserId(string username)
        {
            return WpfDatabase.GetUserId(username);
        }

        private int GetManagerId()
        {
            return WpfDatabase.GetManagerId();
        }
    }

    // =============== КЛАССЫ ДАННЫХ ===============

    public class ClientItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string CreatedDate { get; set; }
    }


        public class AppointmentItem
        {
            public int ID { get; set; }
            public string ClientName { get; set; }
            public string ClientPhone { get; set; }
            public string ServiceName { get; set; }
            public string Price { get; set; }
            public string DateTimeString { get; set; }  // "2025-11-15 14:30"
            public string Status { get; set; }

            // УДОБНЫЕ СВОЙСТВА (только для чтения)
            public DateTime? ParsedDateTime
            {
                get
                {
                    if (DateTime.TryParse(DateTimeString, out DateTime parsed))
                        return parsed;
                    return null;
                }
            }

            public DateTime? AppointmentDate =>
                    ParsedDateTime?.Date;

            public TimeSpan? AppointmentTime =>
                ParsedDateTime?.TimeOfDay;
        }

        public class MaterialItem
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public string Unit { get; set; }
        public string Category { get; set; }
        public string Supplier { get; set; }
        public string CostPerUnit { get; set; }
        public string Status { get; set; }
    }

    public class ComboItem
    {
        public int ID { get; set; }
        public string Display { get; set; }
    }

    // =============== ДИАЛОГОВЫЕ ОКНА ===============

    public partial class ReasonInputDialogR : Window
    {
        public string Reason { get; private set; }

        public ReasonInputDialogR(string title)
        {
            Width = 450;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Title = "Укажите причину";

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 20, 20, 15)
            };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            var reasonBox = new TextBox
            {
                Name = "TxtReason",
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(20, 0, 20, 15)
            };
            Grid.SetRow(reasonBox, 1);
            grid.Children.Add(reasonBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "✅ Подтвердить",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White,
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                Reason = reasonBox.Text.Trim();
                if (!string.IsNullOrEmpty(Reason))
                {
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Введите причину!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Padding = new Thickness(20, 8, 20, 8),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }

    public partial class StatusSelectionDialog : Window
    {
        public string SelectedStatus { get; private set; }

        public StatusSelectionDialog(string[] statuses, string currentStatus)
        {
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Title = "Изменение статуса";

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var currentStatusText = new TextBlock
            {
                Text = $"Текущий статус: {currentStatus}",
                FontSize = 14,
                Margin = new Thickness(20, 20, 20, 10)
            };
            Grid.SetRow(currentStatusText, 0);
            grid.Children.Add(currentStatusText);

            var label = new Label
            {
                Content = "Выберите новый статус:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(15, 0, 20, 0)
            };
            Grid.SetRow(label, 1);
            grid.Children.Add(label);

            var statusCombo = new ComboBox
            {
                ItemsSource = statuses,
                SelectedItem = currentStatus,
                Height = 35,
                FontSize = 14,
                Margin = new Thickness(20, 5, 20, 20)
            };
            Grid.SetRow(statusCombo, 2);
            grid.Children.Add(statusCombo);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "✅ Изменить",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
                IsDefault = true
            };
            okButton.Click += (s, e) =>
            {
                if (statusCombo.SelectedItem != null)
                {
                    SelectedStatus = statusCombo.SelectedItem.ToString();
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Выберите новый статус!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Padding = new Thickness(20, 8, 20, 8),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}
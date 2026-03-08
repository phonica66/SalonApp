using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SalonApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            try
        {
                // Инициализация базы данных
                WpfDatabase.Initialize();
                TxtConnectionStatus.Text = "✅ База данных готова";

                // Пробуем подключиться к серверу (опционально)
                var connected = await WpfDatabase.ConnectToServerAsync();
                if (connected)
                {
                    TxtConnectionStatus.Text = "🌐 Подключен к серверу";
                }
                else
                {
                    TxtConnectionStatus.Text = "🔗 Локальный режим";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                TxtConnectionStatus.Text = "❌ Ошибка инициализации";
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await PerformLogin();
        }

        private async void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await PerformLogin();
            }
        }

        private async Task PerformLogin()
        {
            try
            {
                var username = TxtUsername.Text.Trim();
                var password = TxtPassword.Password.Trim();

                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("Введите логин!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtUsername.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите пароль!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPassword.Focus();
                    return;
                }

                // Блокируем кнопку входа
                BtnLogin.IsEnabled = false;
                BtnLogin.Content = "🔄 Проверка...";

                // Выполняем авторизацию
                var role = await WpfDatabase.LoginAsync(username, password);

                if (!string.IsNullOrEmpty(role))
                {
                    // Успешная авторизация
                    MessageBox.Show($"Добро пожаловать!\nВы вошли как: {role}", "Успешный вход", 
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    // Открываем соответствующее окно
                    Window targetWindow = null;

                    switch (role.ToLower())
                    {
                        case "администратор":
                        case "admin":
                            targetWindow = new AdminWindow(username);
                            break;

                        case "менеджер":
                        case "manager":
                            targetWindow = new ManagerWindow(username);
                            break;
                    }

                    if (targetWindow != null)
                    {
                        targetWindow.Show();
                        this.Close();
                    }
                }
                else
                {
                    // Ошибка авторизации
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка входа", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtPassword.Clear();
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Разблокируем кнопку входа
                BtnLogin.IsEnabled = true;
                BtnLogin.Content = "🚀 ВОЙТИ В СИСТЕМУ";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtUsername.Focus();
        }
    }
}
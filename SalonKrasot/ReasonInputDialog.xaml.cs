using System.Windows;

namespace SalonApp
{
    public partial class ReasonInputDialog : Window
    {
        // Свойство для получения введённого текста
        public string ReasonText => TxtReason.Text.Trim();

        public ReasonInputDialog(string title = "Укажите причину удаления")
        {
            InitializeComponent();
            TxtTitle.Text = title;
            UpdateWatermarkVisibility();
        }

        // При изменении текста — обновляем видимость водяного знака
        private void TxtReason_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateWatermarkVisibility();
        }

        // Проверяем, нужно ли показать водяной знак
        private void UpdateWatermarkVisibility()
        {
            TxtWatermark.Visibility = string.IsNullOrWhiteSpace(TxtReason.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        // Кнопка подтверждения
        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ReasonText))
            {
                MessageBox.Show("Пожалуйста, введите причину перед подтверждением.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true; // закрывает окно с результатом "ОК"
        }

        // Кнопка отмены
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // просто закрывает окно
        }
    }
}

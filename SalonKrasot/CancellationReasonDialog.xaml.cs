using System.Collections.Generic;
using System.Windows;

namespace SalonApp
{
        public partial class CancellationReasonDialog : Window
        {
            public string Reason { get; private set; }

            public CancellationReasonDialog()
            {
                InitializeComponent(); // Теперь работает!
            }

            private void Ok_Click(object sender, RoutedEventArgs e)
            {
                Reason = ReasonTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }

            private void Cancel_Click(object sender, RoutedEventArgs e)
            {
                DialogResult = false;
                Close();
            }
        }
}

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SalonApp
{
    public static class ServerNotifier
    {
        private static readonly string? serverUrl = Environment.GetEnvironmentVariable("SALON_SERVER_URL");

        public static async Task NotifyAppointmentStatusAsync(
        int appointmentId,
        string newStatus,
        DateTime? appointmentDate = null,
        TimeSpan? appointmentTime = null,
        string cancellationReason = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serverUrl))
                    return;

                using (HttpClient client = new HttpClient())
                {
                    string url = $"{serverUrl}/api/appointments/{appointmentId}/status";

                    var payload = new
                    {
                        status = newStatus,
                        appointmentDate = appointmentDate?.ToString("yyyy-MM-dd"),
                        appointmentTime = appointmentTime?.ToString(@"hh\:mm"),
                        cancellationReason = cancellationReason
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync(url, content);
                    string result = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[SERVER RESPONSE] {result}");

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Ошибка сервера:\n{result}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка соединения:\n{ex.Message}", "Ошибка сети", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

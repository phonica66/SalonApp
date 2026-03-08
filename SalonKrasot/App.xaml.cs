using SalonApp;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SalonKrasot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                SimpleBackup.Start();
                WpfDatabase.Initialize();
                System.Console.WriteLine("[App] OK");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
                Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SimpleBackup.Stop();
        }
    }

}

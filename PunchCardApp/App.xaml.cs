using System.Windows;

namespace PunchCardApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            MessageBox.Show("123", "Session Ending", MessageBoxButton.YesNo);
        }
    }
}
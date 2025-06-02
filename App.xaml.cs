using System.Windows;

namespace ModernTimerWidget
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Запуск только одной копии приложения
            var current = System.Diagnostics.Process.GetCurrentProcess();
            var procs = System.Diagnostics.Process.GetProcessesByName(current.ProcessName);
            if (procs.Length > 1)
            {
                MessageBox.Show("Приложение уже запущено!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                Current.Shutdown();
            }
        }
    }
}

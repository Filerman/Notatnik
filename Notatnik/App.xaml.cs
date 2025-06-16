using System;
using System.Windows;
using System.Windows.Threading;

namespace Notatnik
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Wystąpił błąd:\n{e.Exception.Message}\n\nWięcej szczegółów w Output Window.",
                "Błąd aplikacji",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            System.Diagnostics.Debug.WriteLine(e.Exception);

            e.Handled = true;
        }
    }
}

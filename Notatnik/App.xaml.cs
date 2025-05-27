using System;
using System.Windows;
using System.Windows.Threading;

namespace Notatnik
{
    public partial class App : Application
    {
        public App()
        {
            // łapiemy wszelkie nieobsłużone wyjątki na wątku UI
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Wystąpił błąd:\n{e.Exception.Message}\n\nWięcej szczegółów w Output Window.",
                "Błąd aplikacji",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // wypisz do debuggera pełne info
            System.Diagnostics.Debug.WriteLine(e.Exception);

            // jeśli ustawisz na true, aplikacja nie zakończy się natychmiast
            e.Handled = true;
        }
    }
}

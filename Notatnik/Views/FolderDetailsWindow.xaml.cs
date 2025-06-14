// FolderDetailsWindow.xaml.cs
using System;
using System.Windows;

namespace Notatnik.Views
{
    public partial class FolderDetailsWindow : Window
    {
        /// <summary> Delegat otrzymuje nazwę (po Trim) i zwraca true, gdy folder o takiej nazwie już jest w bazie. </summary>
        private readonly Func<string, bool> _nameExists;

        public string FolderName
        {
            get => TextBoxName.Text.Trim();    // Trim, by uniknąć końcowych spacji
            set => TextBoxName.Text = value;
        }

        /// <param name="nameExists">
        ///     delegat (np. lambda) który mówi, czy dana nazwa jest już zajęta;
        ///     przy edycji możesz przekazać lambdę pomijającą bieżący folder.
        /// </param>
        public FolderDetailsWindow(Func<string, bool> nameExists)
        {
            InitializeComponent();
            _nameExists = nameExists;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var name = FolderName;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Nazwa nie może być pusta.",
                                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_nameExists != null && _nameExists(name))
            {
                MessageBox.Show("Folder o takiej nazwie już istnieje.",
                                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

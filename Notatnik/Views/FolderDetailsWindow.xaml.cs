using System.Windows;

namespace Notatnik.Views
{
    public partial class FolderDetailsWindow : Window
    {
        public string FolderName { get; private set; }

        public FolderDetailsWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxName.Text))
            {
                MessageBox.Show("Nazwa nie może być pusta.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FolderName = TextBoxName.Text.Trim();
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

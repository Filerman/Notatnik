using System.Windows;

namespace Notatnik.Views
{
    public partial class FolderDetailsWindow : Window
    {
        public string FolderName
        {
            get => TextBoxName.Text;
            set => TextBoxName.Text = value;
        }

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

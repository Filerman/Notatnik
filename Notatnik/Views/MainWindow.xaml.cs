using System.Windows;
using System.Windows.Controls;
using Notatnik.ViewModels;
using Notatnik.Models;

namespace Notatnik.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Ustawienie DataContext w code-behind (można też w XAML)
            DataContext = new MainViewModel();
        }

        // Handler nazwany zgodnie z XAML: TreeViewFolders_SelectedItemChanged
        private void TreeViewFolders_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedFolder = e.NewValue as Folder;
            }
        }
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Notatnik.Models;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        // Gdy użytkownik kliknie w wiersz folderu (z wyjątkiem checkboxa), zmieniamy SelectedFolder
        private void ListViewFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null) return;

            if (ListViewFolders.SelectedItems.Count == 1)
                ViewModel.SelectedFolder = ListViewFolders.SelectedItem as Folder;
        }

        // Nagłówkowy checkbox w kolumnie Folderów: zaznacz wszystkie foldery
        private void CheckAllFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = true;
        }

        // Nagłówkowy checkbox w kolumnie Folderów: odznacz wszystkie foldery
        private void CheckAllFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = false;
        }

        // Nagłówkowy checkbox w kolumnie Notatek: zaznacz wszystkie notatki
        private void CheckAllNotes_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = true;
        }

        // Nagłówkowy checkbox w kolumnie Notatek: odznacz wszystkie notatki
        private void CheckAllNotes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = false;
        }
    }
}

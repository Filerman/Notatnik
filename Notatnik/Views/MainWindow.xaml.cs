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
            // DataContext ustawia się już w XAML, nie nadpisujemy go ponownie
        }

        // Jeśli użytkownik zmienia zaznaczenie wierszy ręcznie: 
        // opcjonalnie możemy zaktualizować stan header‐checkboxa (czysto kosmetycznie).
        private void ListViewFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null) return;
            if (ListViewFolders.SelectedItems.Count == 1)
                ViewModel.SelectedFolder = ListViewFolders.SelectedItem as Folder;
        }

        // Nagłówek: zaznacz wszystkie foldery
        private void HeaderCheckboxFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = true;
        }

        // Nagłówek: odznacz wszystkie foldery
        private void HeaderCheckboxFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = false;
        }

        // Nagłówek: zaznacz wszystkie notatki
        private void HeaderCheckboxNotes_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = true;
        }

        // Nagłówek: odznacz wszystkie notatki
        private void HeaderCheckboxNotes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = false;
        }
    }
}

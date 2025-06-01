using System.Windows;
using System.Windows.Input;
using Notatnik.Models;
using Notatnik.ViewModels;
using Notatnik.Views;

namespace Notatnik.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            // DataContext jest ustawiany w XAML, nie nadpisujemy go tutaj.
        }

        #region Nagłówkowe checkboxy (Foldery)

        private void HeaderCheckboxFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = true;
        }

        private void HeaderCheckboxFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = false;
        }

        #endregion

        #region Nagłówkowe checkboxy (Notatki)

        private void HeaderCheckboxNotes_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = true;
        }

        private void HeaderCheckboxNotes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = false;
        }

        #endregion

        /// <summary>
        /// Obsługa dwukliku w liście notatek – otwiera NoteDetailsWindow w trybie edycji.
        /// </summary>
        private void ListViewNotes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel == null) return;

            // Pobranie zaznaczonej notatki
            var selectedNote = ViewModel.SingleSelectedNote;
            if (selectedNote == null) return;

            // Tworzymy ViewModel dla szczegółów notatki
            var noteVm = new NoteDetailsViewModel(selectedNote);

            // Otwieramy okno edycji (NoteDetailsWindow)
            var detailsWindow = new NoteDetailsWindow(noteVm)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            bool? result = detailsWindow.ShowDialog();

            // Jeżeli użytkownik zapisał (DialogResult == true), odświeżamy listę notatek
            if (result == true)
            {
                ViewModel.LoadNotes();
            }
        }
    }
}

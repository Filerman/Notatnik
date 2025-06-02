using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Notatnik.Data;
using Notatnik.Models;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class SearchWindow : Window
    {
        public SearchViewModel ViewModel { get; }

        public SearchWindow(AppDbContext dbContext, MainViewModel mainVm)
        {
            InitializeComponent();

            // Tworzymy ViewModel wyszukiwania, przekazując context bazy i referencję do MainViewModel
            ViewModel = new SearchViewModel(dbContext, mainVm);
            DataContext = ViewModel;
        }

        // Podwójne kliknięcie na wynik -> otwórz okno edycji notatki w głównym widoku
        private void ListViewResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.SelectedResult == null)
                return;

            // Przekazujemy wybraną notatkę do MainViewModel, by otworzyć edytor
            ViewModel.OpenSelectedNoteCommand.Execute(null);
        }
    }
}

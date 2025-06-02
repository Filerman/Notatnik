using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Notatnik.Commands;
using Notatnik.Data;
using Notatnik.Models;

namespace Notatnik.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly MainViewModel _mainVm;

        // Aktualny tekst wpisany w polu wyszukiwania
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    // Po zmianie tekstu możemy od razu odświeżyć możliwość wykonania komendy
                    _searchNotesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Kolekcja wyników wyszukiwania (uwaga: to lista Note, bo chcemy wyświetlać właściwość Snippet)
        public ObservableCollection<Note> SearchResults { get; } = new ObservableCollection<Note>();

        // Zaznaczona w ListView notatka
        private Note _selectedResult;
        public Note SelectedResult
        {
            get => _selectedResult;
            set
            {
                if (_selectedResult != value)
                {
                    _selectedResult = value;
                    OnPropertyChanged(nameof(SelectedResult));
                    _openSelectedNoteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Komendy
        private RelayCommand _searchNotesCommand;
        public ICommand SearchNotesCommand => _searchNotesCommand;

        private RelayCommand _clearCommand;
        public ICommand ClearCommand => _clearCommand;

        private RelayCommand _openSelectedNoteCommand;
        public ICommand OpenSelectedNoteCommand => _openSelectedNoteCommand;

        public SearchViewModel(AppDbContext db, MainViewModel mainVm)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            // Inicjalizacja komend
            _searchNotesCommand = new RelayCommand(_ => DoSearch(), _ => CanSearch());
            _clearCommand = new RelayCommand(_ => ClearSearch(), _ => !string.IsNullOrWhiteSpace(SearchText));
            _openSelectedNoteCommand = new RelayCommand(_ => OpenSelectedNote(), _ => SelectedResult != null);
        }

        /// <summary>
        /// Sprawdza, czy można wykonać wyszukiwanie: tekst nie może być pusty.
        /// </summary>
        private bool CanSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        /// <summary>
        /// Wykonuje wyszukiwanie: pobiera z bazy notatki, które w tytule, w treści lub w tagach pasują do SearchText.
        /// </summary>
        private void DoSearch()
        {
            SearchResults.Clear();

            // Zamieniamy na małe litery, by wyszukiwanie było nieczułe na wielkość
            var query = SearchText.Trim().ToLower();

            try
            {
                // Wyszukujemy:
                // 1) Tytuł zawiera query
                // 2) Content (dla Regular i LongFormat) zawiera query (to nie będzie idealne dla RTF, ale przeszukuje surowy XAML/tekst)
                // 3) Nazwa dowolnego taga przypisanego do notatki zawiera query
                var matches = _db.Notes
                                 .Include(n => n.Tags)
                                 .Include(n => n.ChecklistItems)
                                 .Where(n =>
                                     // 1) Tytuł
                                     EF.Functions.Like(n.Title.ToLower(), $"%{query}%")
                                     // LUB 2) Content (jeśli jest typ Regular albo LongFormat)
                                     || EF.Functions.Like(n.Content.ToLower(), $"%{query}%")
                                     // LUB 3) którykolwiek tag wpasowuje się w query
                                     || n.Tags.Any(t => t.Name.ToLower().Contains(query))
                                 )
                                 .OrderByDescending(n => n.ModifiedAt)
                                 .ToList();

                foreach (var note in matches)
                {
                    SearchResults.Add(note);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wyszukiwania:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Czyści pole wyszukiwania oraz wyniki.
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResults.Clear();
        }

        /// <summary>
        /// Otwiera wybraną notatkę w głównym widoku (przekazując kontrolę do MainViewModel).
        /// </summary>
        private void OpenSelectedNote()
        {
            if (SelectedResult == null) return;

            // Przekazujemy instancję notatki do MainViewModel, aby otworzyć ją w edycji
            // Można np. dodać w MainViewModel metodę, która przyjmuje Note i uruchamia edycję.
            // Załóżmy, że MainViewModel ma metodę “OpenNoteForEdit(Note note)”.
            _mainVm.OpenNoteForEdit(SelectedResult);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

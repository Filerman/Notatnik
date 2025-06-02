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
                    _searchNotesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Kolekcja wyników wyszukiwania (lista notatek)
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

            _searchNotesCommand = new RelayCommand(_ => DoSearch(), _ => CanSearch());
            _clearCommand = new RelayCommand(_ => ClearSearch(), _ => !string.IsNullOrWhiteSpace(SearchText));
            _openSelectedNoteCommand = new RelayCommand(_ => OpenSelectedNote(), _ => SelectedResult != null);
        }

        private bool CanSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchText);
        }

        private void DoSearch()
        {
            SearchResults.Clear();

            var query = SearchText.Trim().ToLower();

            try
            {
                var matches = _db.Notes
                                 .Include(n => n.Tags)
                                 .Include(n => n.ChecklistItems)
                                 .Where(n =>
                                     // 1) Tytuł
                                     EF.Functions.Like(n.Title.ToLower(), $"%{query}%")
                                     // LUB 2) Content (dla Regular i LongFormat)
                                     || EF.Functions.Like(n.Content.ToLower(), $"%{query}%")
                                     // LUB 3) którykolwiek tag
                                     || n.Tags.Any(t => t.Name.ToLower().Contains(query))
                                     // LUB 4) którykolwiek element checklisty
                                     || n.ChecklistItems.Any(ci => ci.Text.ToLower().Contains(query))
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

        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResults.Clear();
        }

        private void OpenSelectedNote()
        {
            if (SelectedResult == null) return;
            _mainVm.OpenNoteForEdit(SelectedResult);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

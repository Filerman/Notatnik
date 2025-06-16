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

        private bool _filterTitle = true;
        private bool _filterFolder = true;
        private bool _filterContent = true;
        private bool _filterTags = true;

        public bool SortByTitle
        {
            get => _filterTitle;
            set { _filterTitle = value; OnPropertyChanged(nameof(SortByTitle)); }
        }
        public bool SortByFolder
        {
            get => _filterFolder;
            set { _filterFolder = value; OnPropertyChanged(nameof(SortByFolder)); }
        }
        public bool SortByContent
        {
            get => _filterContent;
            set { _filterContent = value; OnPropertyChanged(nameof(SortByContent)); }
        }
        public bool SortByTags
        {
            get => _filterTags;
            set { _filterTags = value; OnPropertyChanged(nameof(SortByTags)); }
        }

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
                    _clearCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<Note> SearchResults { get; } = new();
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

        private readonly RelayCommand _searchNotesCommand;
        public ICommand SearchNotesCommand => _searchNotesCommand;

        private readonly RelayCommand _clearCommand;
        public ICommand ClearCommand => _clearCommand;

        private readonly RelayCommand _applyFilterCommand;
        public ICommand ApplySortingCommand => _applyFilterCommand;   // xaml już używa tej nazwy

        private readonly RelayCommand _openSelectedNoteCommand;
        public ICommand OpenSelectedNoteCommand => _openSelectedNoteCommand;

        public SearchViewModel(AppDbContext db, MainViewModel mainVm)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _mainVm = mainVm ?? throw new ArgumentNullException(nameof(mainVm));

            _searchNotesCommand = new RelayCommand(_ => DoSearch(), _ => CanSearch());
            _clearCommand = new RelayCommand(_ => ClearSearch(), _ => !string.IsNullOrWhiteSpace(SearchText));
            _applyFilterCommand = new RelayCommand(_ => DoSearch());          // zawsze dostępna
            _openSelectedNoteCommand = new RelayCommand(_ => OpenSelectedNote(), _ => SelectedResult != null);
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText);

        private void DoSearch()
        {
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            bool allOff = !SortByTitle && !SortByFolder && !SortByContent && !SortByTags;

            string q = SearchText.Trim().ToLower();

            try
            {
                var results = _db.Notes
                                 .Include(n => n.Tags)
                                 .Include(n => n.ChecklistItems)
                                 .Include(n => n.Folder)
                                 .Where(n =>
                                     ((SortByTitle || allOff) && EF.Functions.Like(n.Title.ToLower(), $"%{q}%"))
                                     ||
                                     ((SortByFolder || allOff) && n.Folder.Name.ToLower().Contains(q))
                                     ||
                                     ((SortByContent || allOff) && EF.Functions.Like(n.Content.ToLower(), $"%{q}%"))
                                     ||
                                     ((SortByTags || allOff) && n.Tags.Any(t => t.Name.ToLower().Contains(q)))
                                 )
                                 .OrderByDescending(n => n.ModifiedAt)
                                 .ToList();

                foreach (var note in results)
                    SearchResults.Add(note);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wyszukiwania:\n{ex.Message}",
                                "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResults.Clear();
        }

        private void OpenSelectedNote()
        {
            if (SelectedResult != null)
                _mainVm.OpenNoteForEdit(SelectedResult);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

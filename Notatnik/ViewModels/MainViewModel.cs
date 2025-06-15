using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Notatnik.Commands;
using Notatnik.Data;
using Notatnik.Models;
using Notatnik.Views;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Notatnik.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        public ObservableCollection<Folder> Folders { get; }
        public ObservableCollection<Note> Notes { get; }

        public ICommand SetSortCommand { get; }
        public ICommand AddTextNoteCommand { get; }
        public ICommand AddCheckboxNoteCommand { get; }
        public ICommand AddLongNoteCommand { get; }

        public ICommand EditNoteCommand { get; }

        public ICommand DeleteNoteCommand { get; }
        public ICommand DeleteMarkedNotesCommand { get; }

        public ICommand MoveNoteCommand { get; }
        public ICommand MoveMarkedNotesCommand { get; }
        public ICommand MoveMarkedOrSelectedNoteCommand => new RelayCommand(_ => MoveMarkedOrSelected(), _ => (Notes.Any(n => n.IsMarkedForDeletion) || SingleSelectedNote != null) && !Folders.Any(f => f.IsMarkedForDeletion));

        public ICommand CopyNoteCommand { get; }
        public ICommand CopyMarkedNotesCommand { get; }
        public ICommand CopyMarkedOrSelectedNotesCommand => new RelayCommand(_ => CopyMarkedOrSelected(), _ => (Notes.Any(n => n.IsMarkedForDeletion) || SingleSelectedNote != null) && !Folders.Any(f => f.IsMarkedForDeletion));

        public ICommand AddFolderCommand { get; }
        public ICommand AddSubfolderCommand { get; }
        public ICommand EditFolderCommand { get; }
        public ICommand DeleteFolderCommand { get; }
        public ICommand DeleteMarkedFoldersCommand { get; }

        public ICommand DeleteMarkedItemsCommand { get; }
        public ICommand DeleteMarkedOrSelectedItemsCommand => new RelayCommand(_ => DeleteMarkedOrSelected(), _ => Notes.Any(n => n.IsMarkedForDeletion) || SingleSelectedNote != null || Folders.Any(n => n.IsMarkedForDeletion) || SelectedFolder != null);

        public ICommand SearchCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand OpenChartsCommand { get; }

        private Note _singleSelectedNote;
        public Note SingleSelectedNote
        {
            get => _singleSelectedNote;
            set
            {
                if (_singleSelectedNote != value)
                {
                    _singleSelectedNote = value;
                    OnPropertyChanged(nameof(SingleSelectedNote));
                }
            }
        }

        private Folder _selectedFolder;
        public Folder SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (_selectedFolder != value)
                {
                    _selectedFolder = value;
                    OnPropertyChanged(nameof(SelectedFolder));
                    LoadNotes();
                }
            }
        }
        private string _sortField = "Title";
        private bool _ascending = true;
        public ICollectionView NotesView { get; }

        public MainViewModel()
        {
            _db = new AppDbContextFactory().CreateDbContext(null);

            Folders = new ObservableCollection<Folder>(
                _db.Folders.Include(f => f.Subfolders).ToList());

            Notes = new ObservableCollection<Note>();
            NotesView = CollectionViewSource.GetDefaultView(Notes);

            SetSortCommand = new RelayCommand(p => ApplySort(p as string));
            AddTextNoteCommand = new RelayCommand(_ => AddTextNote(), _ => SelectedFolder != null);
            AddCheckboxNoteCommand = new RelayCommand(_ => AddCheckboxNote(), _ => SelectedFolder != null);
            AddLongNoteCommand = new RelayCommand(_ => AddLongNote(), _ => SelectedFolder != null);

            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SingleSelectedNote != null);

            DeleteNoteCommand = new RelayCommand(_ => DeleteNote(SingleSelectedNote), _ => SingleSelectedNote != null);
            DeleteMarkedNotesCommand = new RelayCommand(_ => DeleteMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));

            MoveNoteCommand = new RelayCommand(p => MoveNote(p as Note), p => p is Note);
            MoveMarkedNotesCommand = new RelayCommand(_ => MoveMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));
            CopyNoteCommand = new RelayCommand(p => CopyNote(p as Note), p => p is Note);
            CopyMarkedNotesCommand = new RelayCommand(_ => CopyMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));

            AddFolderCommand = new RelayCommand(_ => AddFolder(null));
            AddSubfolderCommand = new RelayCommand(p => AddFolder(p as Folder));
            EditFolderCommand = new RelayCommand(_ => EditFolder(), _ => SelectedFolder != null);
            DeleteFolderCommand = new RelayCommand(_ => DeleteFolder(SelectedFolder), _ => SelectedFolder != null);
            DeleteMarkedFoldersCommand = new RelayCommand(_ => DeleteMarkedFolders(), _ => Folders.Any(f => f.IsMarkedForDeletion));

            DeleteMarkedItemsCommand = new RelayCommand(_ => DeleteMarkedItems(), _ => Notes.Any(n => n.IsMarkedForDeletion) || Folders.Any(f => f.IsMarkedForDeletion));

            SearchCommand = new RelayCommand(_ => OpenSearchWindow());
            PrintCommand = new RelayCommand(_ => Print());
            OpenChartsCommand = new RelayCommand(_ => ShowCharts());

            if (Folders.Any())
                SelectedFolder = Folders.First();
        }

        private bool IsTitleUnique(string title, int folderId, int? currentNoteId = null)
        {
            var normalized = title.Trim().ToLowerInvariant();

            var otherTitles = _db.Notes
                                 .Where(n => n.FolderId == folderId &&
                                             n.Id != (currentNoteId ?? 0))
                                 .Select(n => n.Title)
                                 .AsEnumerable()   
                                 .Select(t => t.Trim().ToLowerInvariant());

            return !otherTitles.Contains(normalized);
        }

        private void ApplySort(string field)
        {
            if (string.IsNullOrWhiteSpace(field)) return;

            if (_sortField == field)
                _ascending = !_ascending;
            else
            {
                _sortField = field;
                _ascending = true;
            }

            NotesView.SortDescriptions.Clear();

            NotesView.SortDescriptions.Add(
                new SortDescription(_sortField,
                    _ascending ? ListSortDirection.Ascending
                               : ListSortDirection.Descending));

            NotesView.SortDescriptions.Add(
                new SortDescription(nameof(Note.Id),
                    _ascending ? ListSortDirection.Ascending
                               : ListSortDirection.Descending));

            NotesView.Refresh();
        }

        private void LoadNotes()
        {
            Notes.Clear();
            if (SelectedFolder == null) return;

            var notesInFolder = _db.Notes
                                   .Where(n => n.FolderId == SelectedFolder.Id)
                                   .Include(n => n.ChecklistItems)
                                   .Include(n => n.Tags)
                                   .ToList();

            foreach (var note in notesInFolder)
            {
                note.IsMarkedForDeletion = false;
                Notes.Add(note);
            }

            ApplySort(_sortField);
        }

        private void AddTextNote()
        {
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = NoteType.Regular,
                Content = string.Empty
            };

            var vm = new NoteDetailsViewModel(note, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                if (!IsTitleUnique(note.Title, note.FolderId))
                {
                    MessageBox.Show($"W folderze istnieje już notatka o tytule „{note.Title}”.",
                                    "Duplikat tytułu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;  
                }
                _db.Notes.Add(note);
                _db.SaveChanges();
                LoadNotes();
            }

        }
        private void AddCheckboxNote()
        {
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = NoteType.CheckList,
                Content = string.Empty
            };

            var vm = new NoteDetailsViewModel(note, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                if (!IsTitleUnique(note.Title, note.FolderId))
                {
                    MessageBox.Show($"W folderze istnieje już notatka o tytule „{note.Title}”.",
                                    "Duplikat tytułu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _db.Notes.Add(note);
                _db.SaveChanges();
                LoadNotes();
            }
        }
        private void AddLongNote()
        {
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = NoteType.LongFormat,
                Content = string.Empty
            };

            var vm = new NoteDetailsViewModel(note, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                if (!IsTitleUnique(note.Title, note.FolderId))
                {
                    MessageBox.Show($"W folderze istnieje już notatka o tytule „{note.Title}”.",
                                    "Duplikat tytułu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _db.Notes.Add(note);
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void EditNote()
        {
            if (SingleSelectedNote == null) return;
            var noteToEdit = SingleSelectedNote;

            var vm = new NoteDetailsViewModel(noteToEdit, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                noteToEdit.ModifiedAt = DateTime.Now;
                if (!IsTitleUnique(noteToEdit.Title, noteToEdit.FolderId, noteToEdit.Id))
                {
                    MessageBox.Show($"W tym folderze jest już notatka o tytule „{noteToEdit.Title}”.",
                                    "Duplikat tytułu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;                    // nie zapisuj zmian
                }
                _db.SaveChanges();
                LoadNotes();
            }
        }
        public void OpenNoteForEdit(Note noteToEdit)
        {
            if (noteToEdit == null) return;

            SingleSelectedNote = Notes.FirstOrDefault(n => n.Id == noteToEdit.Id);

            var vm = new NoteDetailsViewModel(noteToEdit, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                noteToEdit.ModifiedAt = DateTime.Now;
                if (!IsTitleUnique(noteToEdit.Title, noteToEdit.FolderId, noteToEdit.Id))
                {
                    MessageBox.Show($"W tym folderze jest już notatka o tytule „{noteToEdit.Title}”.",
                                    "Duplikat tytułu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;                    // nie zapisuj zmian
                }
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void DeleteNote(Note note)
        {
            if (note == null) return;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć notatkę \"{note.Title}\"?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _db.Notes.Remove(note);
            _db.SaveChanges();
            Notes.Remove(note);
        }
        private void DeleteMarkedNotes()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!markedNotes.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedNotes.Count} zaznaczon{(markedNotes.Count == 1 ? "ą" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "e" : "ych"))} notat{(markedNotes.Count == 1 ? "kę" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "ki" : "ek"))}?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (wynik != MessageBoxResult.Yes) return;

            foreach (var note in markedNotes)
            {
                _db.Notes.Remove(note);
                Notes.Remove(note);
            }
            _db.SaveChanges();
        }
        
        private void MoveNote(Note noteToMove)
        {
            if (noteToMove == null) return;

            var dlg = new MoveNoteWindow(_db, noteToMove.FolderId)
            {
                Owner = Application.Current.MainWindow
            };

            if (dlg.ShowDialog() != true) return;

            var targetFolder = dlg.SelectedFolder;
            if (targetFolder == null || targetFolder.Id == noteToMove.FolderId) return;

            noteToMove.FolderId = targetFolder.Id;

            _db.SaveChanges();
            LoadNotes();
        }
        private void MoveMarkedNotes()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!markedNotes.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz przenieść {markedNotes.Count} zaznaczon{(markedNotes.Count == 1 ? "ą" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "e" : "ych"))} notat{(markedNotes.Count == 1 ? "kę" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "ki" : "ek"))}?",
                "Potwierdź przeniesienie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (wynik != MessageBoxResult.Yes) return;

            var dlg = new MoveNoteWindow(_db, markedNotes[0].FolderId)
            {
                Owner = Application.Current.MainWindow,
                Title = $"Przenieś notat{(markedNotes.Count == 1 ? "kę" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "ki" : "ek"))} do folderu"
            };

            if (dlg.ShowDialog() != true) return;

            var targetFolder = dlg.SelectedFolder;

            foreach (var note in markedNotes)
            {
                if (targetFolder == null || targetFolder.Id == note.FolderId) return;

                note.FolderId = targetFolder.Id;

                _db.SaveChanges();
                LoadNotes();
            }
        }
        private void MoveMarkedOrSelected()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (markedNotes.Any())
            {
                MoveMarkedNotes();
            }
            else if (SingleSelectedNote != null)
            {
                MoveNote(SingleSelectedNote);
            }
        }

        private void CopyNote(Note source)
        {
            if (source == null) return;

            var dlg = new MoveNoteWindow(_db, source.FolderId)
            {
                Owner = Application.Current.MainWindow,
                Title = "Skopiuj notatkę do folderu"
            };

            if (dlg.ShowDialog() != true) return;
            var target = dlg.SelectedFolder;
            if (target == null) return;

            var copy = new Note
            {
                Title = $"{source.Title} - kopia",
                Content = source.Content,
                Type = source.Type,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = target.Id,
                Tags = source.Tags.ToList()
            };

            foreach (var item in source.ChecklistItems)
                copy.ChecklistItems.Add(new ChecklistItem { Text = item.Text, IsChecked = item.IsChecked });

            _db.Notes.Add(copy);
            _db.SaveChanges();

            if (target.Id == SelectedFolder?.Id) LoadNotes();
        }
        private void CopyMarkedNotes()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!markedNotes.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz skopiować {markedNotes.Count} zaznaczon{(markedNotes.Count == 1 ? "ą" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "e" : "ych"))} notat{(markedNotes.Count == 1 ? "kę" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "ki" : "ek"))}?",
                "Potwierdź kopiowanie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (wynik != MessageBoxResult.Yes) return;

            var dlg = new MoveNoteWindow(_db, markedNotes[0].FolderId)
            {
                Owner = Application.Current.MainWindow,
                Title = $"Skopiuj notat{(markedNotes.Count == 1 ? "kę" : (markedNotes.Count > 1 && markedNotes.Count < 5 ? "ki" : "ek"))} do folderu"
            };

            if (dlg.ShowDialog() != true) return;
            var targetFolder = dlg.SelectedFolder;
            if (targetFolder == null) return;

            foreach (var note in markedNotes)
            {
                var copy = new Note
                {
                    Title = $"{note.Title} - kopia",
                    Content = note.Content,
                    Type = note.Type,
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now,
                    FolderId = targetFolder.Id,
                    Tags = note.Tags.ToList()
                };

                foreach (var item in note.ChecklistItems)
                    copy.ChecklistItems.Add(new ChecklistItem { Text = item.Text, IsChecked = item.IsChecked });

                _db.Notes.Add(copy);
            }

            _db.SaveChanges();
            if (targetFolder.Id == SelectedFolder?.Id) LoadNotes();
        }
        private void CopyMarkedOrSelected()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (markedNotes.Any())
            {
                CopyMarkedNotes();
            }
            else if (SingleSelectedNote != null)
            {
                CopyNote(SingleSelectedNote);
            }
        }

        private void AddFolder(Folder parentFolder)
        {
            var parentId = parentFolder?.Id;      

            bool NameExists(string name) =>
                _db.Folders.Any(f => f.ParentFolderId == parentId
                                  && f.Name.ToLower() == name.ToLower());

            var dlg = new FolderDetailsWindow(NameExists)
            {
                Title = parentFolder == null
                        ? "Dodaj nowy folder"
                        : $"Dodaj podfolder w „{parentFolder.Name}”"
            };
            if (dlg.ShowDialog() != true) return;

            var folder = new Folder { Name = dlg.FolderName, ParentFolderId = parentId };
            _db.Folders.Add(folder);
            _db.SaveChanges();

            if (parentFolder == null) Folders.Add(folder); 
            SelectedFolder = folder;
        }

        private void AddFolder() => AddFolder(SelectedFolder);
        private void EditFolder()
        {
            if (SelectedFolder == null) return;

            bool NameExists(string name) =>
                _db.Folders.Any(f =>
                      f.Id != SelectedFolder.Id &&
                      f.ParentFolderId == SelectedFolder.ParentFolderId &&
                      f.Name.ToLower() == name.ToLower());

            var dlg = new FolderDetailsWindow(NameExists)
            {
                FolderName = SelectedFolder.Name,
                Title = "Edytuj folder"
            };

            if (dlg.ShowDialog() == true)
            {
                SelectedFolder.Name = dlg.FolderName;
                SelectedFolder.OnPropertyChanged(nameof(SelectedFolder.Name));
                _db.SaveChanges();
            }
        }
        private void DeleteFolder(Folder folder)
        {
            if (folder == null) return;

            var ask = MessageBox.Show($"Czy na pewno usunąć „{folder.Name}”? Usunięte zostaną również wszystkie podfoldery i notatki.",
                                      "Potwierdź usunięcie",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Warning);

            if (ask != MessageBoxResult.Yes) return;

            DeleteFolderRecursive(folder);

            if (folder.ParentFolderId == null)
            {
                Folders.Remove(folder);
            }
            else
            {
                var parent = _db.Folders.Include(f => f.Subfolders)
                                       .FirstOrDefault(f => f.Id == folder.ParentFolderId);
                parent?.Subfolders.Remove(folder);
            }

            _db.SaveChanges();

            if (SelectedFolder == folder)
                SelectedFolder = null;
        }

        private void DeleteFolderRecursive(Folder folder)
        {
            var notes = _db.Notes.Where(n => n.FolderId == folder.Id).ToList();
            _db.Notes.RemoveRange(notes);

            var subfolders = _db.Folders.Include(f => f.Subfolders)
                                       .Where(f => f.ParentFolderId == folder.Id)
                                       .ToList();

            foreach (var subfolder in subfolders)
            {
                DeleteFolderRecursive(subfolder);
            }

            _db.Folders.Remove(folder);
        }

        private void DeleteMarkedFolders()
        {
            var markedFolders = GetAllFoldersRecursive(Folders)
                                .Where(f => f.IsMarkedForDeletion)
                                .ToList();

            if (!markedFolders.Any()) return;

            var wynik = MessageBox.Show($"Czy na pewno chcesz usunąć {markedFolders.Count} zaznaczon{(markedFolders.Count == 1 ? "y" : (markedFolders.Count > 1 && markedFolders.Count < 5 ? "e" : "ych"))} folder{(markedFolders.Count == 1 ? "" : (markedFolders.Count > 1 && markedFolders.Count < 5 ? "y" : "ów"))}?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (wynik != MessageBoxResult.Yes) return;

            var foldersByLevel = markedFolders.GroupBy(f => f.ParentFolderId)
                                            .OrderBy(g => g.Key == null ? 0 : 1);

            foreach (var group in foldersByLevel)
            {
                foreach (var folder in group.ToList())
                {
                    DeleteFolderRecursive(folder);

                    if (folder.ParentFolderId == null)
                    {
                        Folders.Remove(folder);
                    }
                }
            }

            _db.SaveChanges();

            if (!Folders.Contains(SelectedFolder))
                SelectedFolder = Folders.FirstOrDefault();
        }

        private void DeleteMarkedItems()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            var markedFolders = Folders.Where(f => f.IsMarkedForDeletion).ToList();

            if (!markedNotes.Any() && !markedFolders.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedNotes.Count} zaznaczon{(markedNotes.Count == 1 ? "ą" : "e")} notatkę(i) i {markedFolders.Count} zaznaczon{(markedFolders.Count == 1 ? "y" : "e")} folder(y) wraz z całą zawartością?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (wynik != MessageBoxResult.Yes) return;

            foreach (var note in markedNotes)
            {
                _db.Notes.Remove(note);
                Notes.Remove(note);
            }

            foreach (var folder in markedFolders)
            {
                DeleteFolderRecursive(folder);
                Folders.Remove(folder);
            }

            _db.SaveChanges();

            if (!Folders.Contains(SelectedFolder))
                SelectedFolder = Folders.FirstOrDefault();
        }
        private void DeleteMarkedOrSelected()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            var markedFolders = Folders.Where(f => f.IsMarkedForDeletion).ToList();

            if (markedNotes.Any() || markedFolders.Any())
            {
                DeleteMarkedItems();
            }
            else if (SingleSelectedNote != null)
            {
                DeleteNote(SingleSelectedNote);
            }
            else if (SelectedFolder != null)
            {
                DeleteFolder(SelectedFolder);
            }
        }

        private void OpenSearchWindow()
        {
            var searchWin = new SearchWindow(_db, this)
            {
                Owner = Application.Current.MainWindow
            };
            searchWin.Show();
        }
        private void Print()
        {
            if (SingleSelectedNote == null)
            {
                MessageBox.Show("Wybierz notatkę do wydrukowania.", "Drukowanie",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var doc = new System.Windows.Documents.FlowDocument(
                    new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(SingleSelectedNote.Content)));
                doc.Name = "NotePrintDocument";
                printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc)
                                          .DocumentPaginator, "Drukowanie notatki");
            }
        }

        private void ShowCharts()
        {
            var win = new ChartsWindow
            {
                DataContext = new ChartsViewModel() 
            };
            win.Show();
        }

        private IEnumerable<Folder> GetAllFoldersRecursive(IEnumerable<Folder> folders)
        {
            foreach (var folder in folders)
            {
                yield return folder;

                foreach (var subfolder in GetAllFoldersRecursive(folder.Subfolders))
                {
                    yield return subfolder;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
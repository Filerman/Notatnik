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
using Notatnik.Views;

namespace Notatnik.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ─────────────────────────────────────────── FIELDS
        private readonly AppDbContext _db;

        // ─────────────────────────────────────────── PROPERTIES
        public ObservableCollection<Folder> Folders { get; }
        public ObservableCollection<Note> Notes { get; }

        // Komendy główne
        public ICommand AddNoteCommand { get; }
        public ICommand EditNoteCommand { get; }
        public ICommand DeleteMarkedNotesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand DeleteMarkedFoldersCommand { get; }
        public ICommand EditFolderCommand { get; }
        public ICommand AddTextNoteCommand { get; }
        public ICommand AddCheckboxNoteCommand { get; }
        public ICommand AddLongNoteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand PrintCommand { get; }

        // Nowe komendy przenoszenia / kopiowania
        public ICommand MoveNoteCommand { get; }
        public ICommand CopyNoteCommand { get; }

        // Wybrany folder i pojedyncza notatka
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

        // ─────────────────────────────────────────── CTOR
        public MainViewModel()
        {
            _db = new AppDbContextFactory().CreateDbContext(null);

            // Załaduj foldery
            Folders = new ObservableCollection<Folder>(
                _db.Folders.Include(f => f.Subfolders).ToList());

            // Lista notatek pustą zapełnia LoadNotes()
            Notes = new ObservableCollection<Note>();

            #region inicjalizacja_komend
            AddNoteCommand = new RelayCommand(_ => AddNote(), _ => SelectedFolder != null);
            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SingleSelectedNote != null);
            DeleteMarkedNotesCommand = new RelayCommand(_ => DeleteMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));
            AddFolderCommand = new RelayCommand(_ => AddFolder());
            DeleteNoteCommand = new RelayCommand(_ => DeleteNote(SingleSelectedNote), _ => SingleSelectedNote != null);
            DeleteMarkedFoldersCommand = new RelayCommand(_ => DeleteMarkedFolders(), _ => Folders.Any(f => f.IsMarkedForDeletion));
            EditFolderCommand = new RelayCommand(_ => EditFolder(), _ => SelectedFolder != null);
            AddTextNoteCommand = new RelayCommand(_ => AddTextNote(), _ => SelectedFolder != null);
            AddCheckboxNoteCommand = new RelayCommand(_ => AddCheckboxNote(), _ => SelectedFolder != null);
            AddLongNoteCommand = new RelayCommand(_ => AddLongNote(), _ => SelectedFolder != null);
            SearchCommand = new RelayCommand(_ => OpenSearchWindow());
            PrintCommand = new RelayCommand(_ => Print());

            MoveNoteCommand = new RelayCommand(p => MoveNote(p as Note), p => p is Note);
            CopyNoteCommand = new RelayCommand(p => CopyNote(p as Note), p => p is Note);
            #endregion

            // Domyślny wybór pierwszego folderu
            if (Folders.Any())
                SelectedFolder = Folders.First();
        }

        // ─────────────────────────────────────────── MOVE NOTE
        private void MoveNote(Note noteToMove)
        {
            if (noteToMove == null) return;

            var dlg = new MoveNoteWindow(_db, noteToMove.FolderId)
            {
                Owner = Application.Current.MainWindow,
                Title = "Przenieś notatkę do folderu"
            };

            if (dlg.ShowDialog() != true) return;

            var target = dlg.SelectedFolder;
            if (target == null || target.Id == noteToMove.FolderId) return;

            noteToMove.FolderId = target.Id;
            noteToMove.ModifiedAt = DateTime.Now;

            _db.SaveChanges();
            LoadNotes();
        }

        // ─────────────────────────────────────────── COPY NOTE
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
            {
                copy.ChecklistItems.Add(new ChecklistItem
                {
                    Text = item.Text,
                    IsChecked = item.IsChecked
                });
            }

            _db.Notes.Add(copy);
            _db.SaveChanges();

            if (target.Id == SelectedFolder?.Id)
                LoadNotes();
        }

        // ─────────────────────────────────────────── LOAD NOTES
        private void LoadNotes()
        {
            Notes.Clear();
            if (SelectedFolder == null) return;

            var list = _db.Notes
                          .Where(n => n.FolderId == SelectedFolder.Id)
                          .Include(n => n.ChecklistItems)
                          .ToList();

            foreach (var n in list)
            {
                n.IsMarkedForDeletion = false;
                Notes.Add(n);
            }
        }

        // ─────────────────────────────────────────── ADD NOTE (wybór typu)
        private void AddNote()
        {
            var dlg = new SelectNoteTypeWindow();
            if (dlg.ShowDialog() != true) return;

            CreateAndEditNewNote(dlg.SelectedType);
        }

        private void AddTextNote() => CreateAndEditNewNote(NoteType.Regular);
        private void AddCheckboxNote() => CreateAndEditNewNote(NoteType.CheckList);
        private void AddLongNote() => CreateAndEditNewNote(NoteType.LongFormat);

        private void CreateAndEditNewNote(NoteType type)
        {
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = type,
                Content = string.Empty
            };

            var vm = new NoteDetailsViewModel(note, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                _db.Notes.Add(note);
                _db.SaveChanges();
                LoadNotes();
            }
        }

        // ─────────────────────────────────────────── EDIT / DELETE NOTE
        private void EditNote()
        {
            if (SingleSelectedNote == null) return;

            var vm = new NoteDetailsViewModel(SingleSelectedNote, _db);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                SingleSelectedNote.ModifiedAt = DateTime.Now;
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void DeleteNote(Note note)
        {
            if (note == null) return;

            var ask = MessageBox.Show($"Czy na pewno usunąć „{note.Title}”?",
                                      "Potwierdź usunięcie",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question);

            if (ask != MessageBoxResult.Yes) return;

            _db.Notes.Remove(note);
            _db.SaveChanges();
            Notes.Remove(note);
        }

        private void DeleteMarkedNotes()
        {
            var marked = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!marked.Any()) return;

            var ask = MessageBox.Show($"Usunąć {marked.Count} zaznaczon{(marked.Count == 1 ? "ą" : "e")} notatk{(marked.Count == 1 ? "ę" : "i")}?",
                                      "Potwierdź usunięcie",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Question);

            if (ask != MessageBoxResult.Yes) return;

            foreach (var n in marked)
            {
                _db.Notes.Remove(n);
                Notes.Remove(n);
            }
            _db.SaveChanges();
        }

        // ─────────────────────────────────────────── FOLDERS CRUD
        private void AddFolder()
        {
            var dlg = new FolderDetailsWindow();
            if (dlg.ShowDialog() != true) return;

            var folder = new Folder { Name = dlg.FolderName };
            _db.Folders.Add(folder);
            _db.SaveChanges();

            Folders.Add(folder);
            SelectedFolder = folder;
        }

        private void DeleteMarkedFolders()
        {
            var marked = Folders.Where(f => f.IsMarkedForDeletion).ToList();
            if (!marked.Any()) return;

            var ask = MessageBox.Show($"Usunąć {marked.Count} zaznaczon{(marked.Count == 1 ? "y" : "e")} folder(y) wraz z zawartością?",
                                      "Potwierdź usunięcie",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Warning);

            if (ask != MessageBoxResult.Yes) return;

            foreach (var f in marked)
            {
                DeleteFolderRecursive(f);
                Folders.Remove(f);
            }
            _db.SaveChanges();

            if (!Folders.Contains(SelectedFolder))
                SelectedFolder = Folders.FirstOrDefault();
        }

        private void DeleteFolderRecursive(Folder folder)
        {
            foreach (var n in _db.Notes.Where(n => n.FolderId == folder.Id).ToList())
                _db.Notes.Remove(n);

            foreach (var sub in _db.Folders.Where(f => f.ParentFolderId == folder.Id).ToList())
                DeleteFolderRecursive(sub);

            _db.Folders.Remove(folder);
        }

        private void EditFolder()
        {
            if (SelectedFolder == null) return;

            var dlg = new FolderDetailsWindow { FolderName = SelectedFolder.Name };
            if (dlg.ShowDialog() == true)
            {
                SelectedFolder.Name = dlg.FolderName;
                SelectedFolder.OnPropertyChanged(nameof(SelectedFolder.Name));
                _db.SaveChanges();
            }
        }

        // ─────────────────────────────────────────── SEARCH / PRINT
        private void OpenSearchWindow()
        {
            var win = new SearchWindow(_db, this)
            {
                Owner = Application.Current.MainWindow
            };
            win.Show();
        }

        public void OpenNoteForEdit(Note note)
        {
            if (note == null) return;

            SingleSelectedNote = Notes.FirstOrDefault(n => n.Id == note.Id);
            EditNote();
        }

        private void Print()
        {
            if (SingleSelectedNote == null)
            {
                MessageBox.Show("Wybierz notatkę do wydrukowania.",
                                "Drukowanie",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            var pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
            {
                var flow = new System.Windows.Documents.FlowDocument(
                    new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(SingleSelectedNote.Content)));

                pd.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)flow)
                                 .DocumentPaginator, "Drukowanie notatki");
            }
        }

        // ─────────────────────────────────────────── INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

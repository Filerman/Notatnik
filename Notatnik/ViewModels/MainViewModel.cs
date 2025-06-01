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
        private readonly AppDbContext _db;

        public ObservableCollection<Folder> Folders { get; }
        public ObservableCollection<Note> Notes { get; }

        public ICommand AddNoteCommand { get; }
        public ICommand EditNoteCommand { get; }
        public ICommand DeleteMarkedNotesCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand DeleteMarkedFoldersCommand { get; }

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
                    LoadNotes(); // przy każdej zmianie folderu ładujemy notatki
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

        public MainViewModel()
        {
            _db = new AppDbContextFactory().CreateDbContext(null);

            // 1) Wczytujemy wszystkie foldery (raz, z podfolderami jeżeli są)
            var folderList = _db.Folders
                                 .Include(f => f.Subfolders)
                                 .ToList();
            Folders = new ObservableCollection<Folder>(folderList);

            // 2) Inicjalizujemy kolekcję notatek (pusta na start)
            Notes = new ObservableCollection<Note>();

            // 3) Definiujemy komendy
            AddNoteCommand = new RelayCommand(_ => AddNote(), _ => SelectedFolder != null);
            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SingleSelectedNote != null);
            DeleteMarkedNotesCommand = new RelayCommand(_ => DeleteMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));
            AddFolderCommand = new RelayCommand(_ => AddFolder());
            DeleteMarkedFoldersCommand = new RelayCommand(_ => DeleteMarkedFolders(), _ => Folders.Any(f => f.IsMarkedForDeletion));

            // 4) Ustawiamy pierwszy folder jako domyślnie wybrany (jeśli istnieje)
            if (Folders.Any())
                SelectedFolder = Folders.First();
        }

        /// <summary>
        /// Ładuje z bazy notatki dla aktualnie wybranego folderu.
        /// Po pobraniu każdej notatki ustawiamy IsMarkedForDeletion = false w pamięci.
        /// </summary>
        // … (nagłówki i ususingi bez zmian)

        public void LoadNotes()
        {
            Notes.Clear();
            if (SelectedFolder == null) return;

            var notesInFolder = _db.Notes
                                   .Where(n => n.FolderId == SelectedFolder.Id)
                                   .Include(n => n.ChecklistItems)   // ← DOCIĄGAMY checklistę
                                   .ToList();

            foreach (var note in notesInFolder)
            {
                note.IsMarkedForDeletion = false;
                Notes.Add(note);
            }
        }


        private void AddNote()
        {
            var typeDlg = new SelectNoteTypeWindow();
            if (typeDlg.ShowDialog() != true)
                return;

            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = typeDlg.SelectedType,
                Content = string.Empty
            };

            var vm = new NoteDetailsViewModel(note);
            var win = new NoteDetailsWindow(vm)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool? result = win.ShowDialog();
            if (result == true)
            {
                // Po zapisaniu notatki przez NoteDetailsViewModel, odświeżamy listę
                LoadNotes();
            }
        }

        private void EditNote()
        {
            if (SingleSelectedNote == null)
                return;

            var noteToEdit = SingleSelectedNote;
            var vm = new NoteDetailsViewModel(noteToEdit);
            var win = new NoteDetailsWindow(vm)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            bool? result = win.ShowDialog();
            if (result == true)
            {
                noteToEdit.ModifiedAt = DateTime.Now;
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void DeleteMarkedNotes()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!markedNotes.Any())
                return;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedNotes.Count} zaznaczoną{(markedNotes.Count == 1 ? "ą" : "e")} notatkę(i)?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (var note in markedNotes)
            {
                _db.Notes.Remove(note);
                Notes.Remove(note);
            }
            _db.SaveChanges();
        }

        private void AddFolder()
        {
            var dlg = new FolderDetailsWindow();
            if (dlg.ShowDialog() != true)
                return;

            var folder = new Folder { Name = dlg.FolderName };
            _db.Folders.Add(folder);
            _db.SaveChanges();

            Folders.Add(folder);
            SelectedFolder = folder;
        }

        private void DeleteMarkedFolders()
        {
            var markedFolders = Folders.Where(f => f.IsMarkedForDeletion).ToList();
            if (!markedFolders.Any())
                return;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedFolders.Count} zaznaczony{(markedFolders.Count == 1 ? "y" : "e")} folder(y) wraz z całą zawartością?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            foreach (var folder in markedFolders)
            {
                DeleteFolderRecursive(folder);
                Folders.Remove(folder);
            }

            _db.SaveChanges();

            if (!Folders.Contains(SelectedFolder))
                SelectedFolder = Folders.FirstOrDefault();
        }

        private void DeleteFolderRecursive(Folder folder)
        {
            // Usuwamy najpierw wszystkie notatki z danego folderu
            var notesInFolder = _db.Notes.Where(n => n.FolderId == folder.Id).ToList();
            foreach (var n in notesInFolder)
            {
                _db.Notes.Remove(n);
            }

            // Następnie rekurencyjnie usuwamy podfoldery
            var subfolders = _db.Folders.Where(f => f.ParentFolderId == folder.Id).ToList();
            foreach (var sub in subfolders)
            {
                DeleteFolderRecursive(sub);
            }

            // Na końcu usuwamy sam folder
            _db.Folders.Remove(folder);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}

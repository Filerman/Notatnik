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
        public ICommand EditFolderCommand { get; }
        public ICommand AddCheckboxNoteCommand { get; }
        public ICommand AddLongNoteCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand PrintCommand { get; }

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

        public MainViewModel()
        {
            _db = new AppDbContextFactory().CreateDbContext(null);

            var folderList = _db.Folders.Include(f => f.Subfolders).ToList();
            Folders = new ObservableCollection<Folder>(folderList);

            Notes = new ObservableCollection<Note>();

            AddNoteCommand = new RelayCommand(_ => AddNote(), _ => SelectedFolder != null);
            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SingleSelectedNote != null);
            DeleteMarkedNotesCommand = new RelayCommand(_ => DeleteMarkedNotes(), _ => Notes.Any(n => n.IsMarkedForDeletion));
            AddFolderCommand = new RelayCommand(_ => AddFolder());
            DeleteMarkedFoldersCommand = new RelayCommand(_ => DeleteMarkedFolders(), _ => Folders.Any(f => f.IsMarkedForDeletion));
            AddCheckboxNoteCommand = new RelayCommand(_ => AddCheckboxNote(), _ => SelectedFolder != null);
            AddLongNoteCommand = new RelayCommand(_ => AddLongNote(), _ => SelectedFolder != null);
            SearchCommand = new RelayCommand(_ => Search());
            PrintCommand = new RelayCommand(_ => Print());
            EditFolderCommand = new RelayCommand(_ => EditFolder(), _ => SelectedFolder != null);

            if (Folders.Any())
                SelectedFolder = Folders.First();
            }

        private void LoadNotes()
        {
            Notes.Clear();
            if (SelectedFolder == null) return;

            var notesInFolder = _db.Notes
                                   .Where(n => n.FolderId == SelectedFolder.Id)
                                   .Include(n => n.ChecklistItems)
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
            if (typeDlg.ShowDialog() != true) return;

            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = typeDlg.SelectedType,
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
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void DeleteMarkedNotes()
        {
            var markedNotes = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            if (!markedNotes.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedNotes.Count} zaznaczoną{(markedNotes.Count == 1 ? "ą" : "e")} notatkę(i)?",
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
            var markedFolders = Folders.Where(f => f.IsMarkedForDeletion).ToList();
            if (!markedFolders.Any()) return;

            var wynik = MessageBox.Show(
                $"Czy na pewno chcesz usunąć {markedFolders.Count} zaznaczony{(markedFolders.Count == 1 ? "y" : "e")} folder(y) wraz z całą zawartością?",
                "Potwierdź usunięcie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (wynik != MessageBoxResult.Yes) return;

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
            var notesInFolder = _db.Notes.Where(n => n.FolderId == folder.Id).ToList();
            foreach (var n in notesInFolder)
            {
                _db.Notes.Remove(n);
            }

            var subfolders = _db.Folders.Where(f => f.ParentFolderId == folder.Id).ToList();
            foreach (var sub in subfolders)
            {
                DeleteFolderRecursive(sub);
            }

            _db.Folders.Remove(folder);
        }

        private void Search()
        {
            //var searchWindow = new SearchWindow(); // zaimplementuj to okno, jeśli jeszcze go nie masz
            //searchWindow.ShowDialog();
        }

        private void Print()
        {
            if (SingleSelectedNote == null)
            {
                MessageBox.Show("Wybierz notatkę do wydrukowania.", "Drukowanie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printDialog = new System.Windows.Controls.PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                var doc = new System.Windows.Documents.FlowDocument(
                    new System.Windows.Documents.Paragraph(
                        new System.Windows.Documents.Run(SingleSelectedNote.Content)));
                doc.Name = "NotePrintDocument";
                printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Drukowanie notatki");
            }
        }
        private void EditFolder()
        {
            if (SelectedFolder == null) return;

            var dlg = new FolderDetailsWindow
            {
                FolderName = SelectedFolder.Name
            };

            if (dlg.ShowDialog() == true)
            {
                SelectedFolder.Name = dlg.FolderName;
                SelectedFolder.OnPropertyChanged(nameof(SelectedFolder.Name));
                _db.SaveChanges();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

            var vm = new NoteDetailsViewModel(note);
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

            var vm = new NoteDetailsViewModel(noteToEdit);
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
            var marked = Notes.Where(n => n.IsMarkedForDeletion).ToList();
            foreach (var note in marked)
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
            var marked = Folders.Where(f => f.IsMarkedForDeletion).ToList();
            foreach (var folder in marked)
            {
                var notesInFolder = _db.Notes.Where(n => n.FolderId == folder.Id).ToList();
                foreach (var n in notesInFolder)
                    _db.Notes.Remove(n);

                _db.Folders.Remove(folder);
                Folders.Remove(folder);
            }
            _db.SaveChanges();
            if (!Folders.Contains(SelectedFolder))
                SelectedFolder = Folders.FirstOrDefault();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

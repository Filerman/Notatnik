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

        private Note _selectedNote;
        public Note SelectedNote
        {
            get => _selectedNote;
            set
            {
                if (_selectedNote != value)
                {
                    _selectedNote = value;
                    OnPropertyChanged(nameof(SelectedNote));
                }
            }
        }

        public ICommand AddNoteCommand { get; }
        public ICommand EditNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand DeleteFolderCommand { get; }

        public MainViewModel()
        {
            _db = new AppDbContextFactory().CreateDbContext(null);
            var folderList = _db.Folders
                                .Include(f => f.Subfolders)
                                .ToList();

            Folders = new ObservableCollection<Folder>(folderList);
            Notes = new ObservableCollection<Note>();

            AddNoteCommand = new RelayCommand(_ => AddNote(), _ => SelectedFolder != null);
            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SelectedNote != null);
            DeleteNoteCommand = new RelayCommand(_ => DeleteNote(), _ => SelectedNote != null);
            AddFolderCommand = new RelayCommand(_ => AddFolder());
            DeleteFolderCommand = new RelayCommand(_ => DeleteFolder(), _ => SelectedFolder != null);

            if (Folders.Any())
                SelectedFolder = Folders.First();
        }

        private void LoadNotes()
        {
            Notes.Clear();
            if (SelectedFolder == null) return;

            var notesInFolder = _db.Notes
                                   .Where(n => n.FolderId == SelectedFolder.Id)
                                   .ToList();
            foreach (var note in notesInFolder)
                Notes.Add(note);
        }

        private void AddNote()
        {
            // 1) Pokaż okno wyboru typu notatki
            var typeDlg = new SelectNoteTypeWindow();
            if (typeDlg.ShowDialog() != true)
                return;

            // 2) Utwórz notatkę z wybranym typem
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id,
                Type = typeDlg.SelectedType,  // ← NoteType z enum: CheckList, Regular, LongFormat :contentReference[oaicite:0]{index=0}
                Content = string.Empty      // <<< zapobiegamy nullowi
            };

            // 3) Pokaż okno edycji szczegółów
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
            var vm = new NoteDetailsViewModel(SelectedNote);
            var win = new NoteDetailsWindow(vm);
            if (win.ShowDialog() == true)
            {
                SelectedNote.ModifiedAt = DateTime.Now;
                _db.SaveChanges();
                LoadNotes();
            }
        }

        private void DeleteNote()
        {
            _db.Notes.Remove(SelectedNote);
            _db.SaveChanges();
            LoadNotes();
        }

        private void AddFolder()
        {
            var dlg = new FolderDetailsWindow();
            if (dlg.ShowDialog() == true)
            {
                var folder = new Folder { Name = dlg.FolderName };
                _db.Folders.Add(folder);
                _db.SaveChanges();
                Folders.Add(folder);
                SelectedFolder = folder;
            }
        }

        private void DeleteFolder()
        {
            if (SelectedFolder == null) return;
            _db.Folders.Remove(SelectedFolder);
            _db.SaveChanges();
            Folders.Remove(SelectedFolder);
            SelectedFolder = Folders.FirstOrDefault();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

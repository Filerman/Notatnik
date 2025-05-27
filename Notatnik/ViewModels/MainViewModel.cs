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

        public MainViewModel()
        {
            // 1) Inicjalizacja kontekstu i wczytanie folderów
            _db = new AppDbContextFactory().CreateDbContext(null);
            var folderList = _db.Folders
                                .Include(f => f.Subfolders)
                                .ToList();

            // 2) Jeśli w bazie nie ma folderów, dodaj jeden „Domyślny”
            if (!folderList.Any())
            {
                var root = new Folder { Name = "Domyślny" };
                _db.Folders.Add(root);
                _db.SaveChanges();
                folderList.Add(root);
            }

            // 3) Utworzenie kolekcji i inicjalizacja Notes
            Folders = new ObservableCollection<Folder>(folderList);
            Notes = new ObservableCollection<Note>();

            // 4) Inicjalizacja komend (AddNote wymaga wybranego folderu)
            AddNoteCommand = new RelayCommand(_ => AddNote(), _ => SelectedFolder != null);
            EditNoteCommand = new RelayCommand(_ => EditNote(), _ => SelectedNote != null);
            DeleteNoteCommand = new RelayCommand(_ => DeleteNote(), _ => SelectedNote != null);

            // 5) Ustawienie domyślnego folderu (to wywoła LoadNotes, ale Notes już istnieje)
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
            var note = new Note
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                FolderId = SelectedFolder.Id
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

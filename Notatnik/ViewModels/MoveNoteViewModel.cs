using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Notatnik.Commands;
using Notatnik.Data;
using Notatnik.Models;

namespace Notatnik.ViewModels
{
    public class MoveNoteViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;

        public ObservableCollection<Folder> RootFolders { get; }

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
                }
            }
        }

        public RelayCommand OkCommand { get; }
        public RelayCommand CancelCommand { get; }

        public MoveNoteViewModel(AppDbContext db, int currentFolderId)
        {
            _db = db;

            RootFolders = new ObservableCollection<Folder>(
                _db.Folders
                   .Where(f => f.ParentFolderId == null)
                   .Include(f => f.Subfolders)
                   .OrderBy(f => f.Name)
                   .ToList());

            SelectedFolder = _db.Folders.Find(currentFolderId);

            OkCommand = new RelayCommand(
                w => ((Window)w).DialogResult = true,
                _ => SelectedFolder != null);

            CancelCommand = new RelayCommand(
                w => ((Window)w).DialogResult = false);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}

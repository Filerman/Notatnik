using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notatnik.Models
{
    public class Folder : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? ParentFolderId { get; set; }
        public Folder ParentFolder { get; set; }

        public ICollection<Folder> Subfolders { get; set; } = new ObservableCollection<Folder>();
        public ICollection<Note> Notes { get; set; } = new ObservableCollection<Note>();

        private bool _isMarkedForDeletion = false;
        private bool _isSelected;


        [NotMapped]
        public bool IsMarkedForDeletion
        {
            get => _isMarkedForDeletion;
            set
            {
                if (_isMarkedForDeletion != value)
                {
                    _isMarkedForDeletion = value;
                    OnPropertyChanged(nameof(IsMarkedForDeletion));
                }
            }
        }
        [NotMapped]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System.Collections.Generic;
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
        public List<Folder> Subfolders { get; set; } = new List<Folder>();

        public List<Note> Notes { get; set; } = new List<Note>();

        private bool _isMarkedForDeletion = false;

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

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

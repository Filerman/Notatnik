using System.Collections.Generic;

namespace Notatnik.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // parent-child dla hierarchii
        public int? ParentFolderId { get; set; }
        public Folder ParentFolder { get; set; }
        public ICollection<Folder> Subfolders { get; set; } = new List<Folder>();

        // notatki w folderze
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}

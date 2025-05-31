using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notatnik.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? ParentFolderId { get; set; }
        public Folder ParentFolder { get; set; }
        public List<Folder> Subfolders { get; set; } = new List<Folder>();

        public List<Note> Notes { get; set; } = new List<Note>();

        [NotMapped]
        public bool IsMarkedForDeletion { get; set; } = false;
    }
}

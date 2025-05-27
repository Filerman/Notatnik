using System;
using System.Collections.Generic;

namespace Notatnik.Models
{
    public enum NoteType
    {
        CheckList,
        Regular,
        LongFormat
    }

    public class Note
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public NoteType Type { get; set; }

        // relacja do Folder
        public int FolderId { get; set; }
        public Folder Folder { get; set; }

        // relacja wiele-do-wielu do Tag
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        // **pozycje checklisty** (tylko jeśli Type == CheckList)
        public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
    }
}

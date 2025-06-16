using System.ComponentModel.DataAnnotations.Schema;

namespace Notatnik.Models
{
    public class ChecklistItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsChecked { get; set; }
        public int NoteId { get; set; }
        public Note Note { get; set; }
    }
}

using System.Collections.Generic;

namespace Notatnik.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // do jakich notatek jest przypisany
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}

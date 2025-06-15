using System.Collections.Generic;

namespace Notatnik.Models
{
    public class Tag // test
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}

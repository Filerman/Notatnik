using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;

namespace Notatnik.Models
{
    public enum NoteType
    {
        CheckList,
        Regular,
        LongFormat
    }

    public class Note : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NoteType Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public int FolderId { get; set; }
        public Folder Folder { get; set; }

        public List<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
        public List<Tag> Tags { get; set; } = new List<Tag>();

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

        [NotMapped]
        public string Snippet
        {
            get
            {
                switch (Type)
                {
                    case NoteType.CheckList:
                        if (ChecklistItems != null && ChecklistItems.Any())
                            return string.Join(", ", ChecklistItems.Select(i => i.Text));
                        return string.Empty;

                    case NoteType.LongFormat:
                        if (!string.IsNullOrEmpty(Content))
                        {
                            try
                            {
                                var flowDoc = (FlowDocument)XamlReader.Parse(Content);
                                var textRange = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd);
                                var plain = textRange.Text.Trim().Replace("\r", "").Replace("\n", " ");
                                if (plain.Length <= 100) return plain;
                                return plain.Substring(0, 100) + ".";
                            }
                            catch
                            {
                                return string.Empty;
                            }
                        }
                        return string.Empty;

                    case NoteType.Regular:
                        if (!string.IsNullOrEmpty(Content))
                        {
                            var plain = Content.Trim().Replace("\r", "").Replace("\n", " ");
                            if (plain.Length <= 100) return plain;
                            return plain.Substring(0, 100) + ".";
                        }
                        return string.Empty;

                    default:
                        return string.Empty;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

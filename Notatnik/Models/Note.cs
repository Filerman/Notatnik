using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
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

        /// <summary>
        /// Dla LongFormat trzymamy w Content ciąg XAML (FlowDocument) lub pakiet XAML.
        /// </summary>
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

        /// <summary>
        /// Zwraca podgląd notatki: 
        /// - dla CheckList → lista pozycji,
        /// - dla Regular → pierwsze 100 znaków Content,
        /// - dla LongFormat → próbuje sparsować Content jako XAML (<FlowDocument>…) lub XamlPackage, a następnie wyciąga pierwsze 100 znaków plain-text.
        /// </summary>
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
                        if (string.IsNullOrEmpty(Content))
                            return string.Empty;

                        // 1) Spróbuj jako czysty XAML (FlowDocument …)
                        try
                        {
                            var trimmed = Content.TrimStart();
                            if (trimmed.StartsWith("<FlowDocument", StringComparison.OrdinalIgnoreCase))
                            {
                                var doc = (FlowDocument)XamlReader.Parse(Content);
                                return ExtractPlainText(doc);
                            }
                        }
                        catch
                        {
                            // nie powiodło się parsowanie jako czysty XAML
                        }

                        // 2) Spróbuj jako pakiet XAML (XamlPackage)
                        try
                        {
                            byte[] data = Encoding.UTF8.GetBytes(Content);
                            using var ms = new MemoryStream(data);
                            var tempDoc = new FlowDocument();
                            var rng = new TextRange(tempDoc.ContentStart, tempDoc.ContentEnd);
                            rng.Load(ms, DataFormats.XamlPackage);
                            return ExtractPlainText(tempDoc);
                        }
                        catch
                        {
                            // nawet jako pakiet XAML się nie udało
                        }

                        return string.Empty;

                    case NoteType.Regular:
                        if (!string.IsNullOrEmpty(Content))
                        {
                            var plain = Content.Trim()
                                               .Replace("\r", "")
                                               .Replace("\n", " ");
                            return plain.Length <= 100 ? plain : plain.Substring(0, 100) + ".";
                        }
                        return string.Empty;

                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Pobiera plain text z FlowDocument, obcina do 100 znaków.
        /// </summary>
        private string ExtractPlainText(FlowDocument doc)
        {
            try
            {
                var tr = new TextRange(doc.ContentStart, doc.ContentEnd);
                var plain = tr.Text?.Trim().Replace("\r", "").Replace("\n", " ") ?? string.Empty;
                return plain.Length <= 100 ? plain : plain.Substring(0, 100) + ".";
            }
            catch
            {
                return string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

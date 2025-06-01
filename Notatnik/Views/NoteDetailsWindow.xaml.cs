using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using Notatnik.Models;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class NoteDetailsWindow : Window
    {
        private readonly NoteDetailsViewModel _vm;

        public NoteDetailsWindow(NoteDetailsViewModel vm)
        {
            InitializeComponent();

            _vm = vm;
            DataContext = vm;

            // Jeśli ViewModel wywoła RequestClose, zamykamy okno
            _vm.RequestClose += OnVmRequestClose;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Jeżeli edytujemy istniejącą notatkę typu LongFormat, załaduj jej zawartość do RichTextBoxa
            if (_vm.Note.Type == NoteType.LongFormat &&
                !string.IsNullOrEmpty(_vm.Note.Content))
            {
                var range = new TextRange(RichTextEditor.Document.ContentStart,
                                          RichTextEditor.Document.ContentEnd);
                try
                {
                    // Zakładamy, że Note.Content zawiera XAML zapamiętany jako UTF8 string
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_vm.Note.Content));
                    range.Load(ms, DataFormats.Xaml);
                }
                catch
                {
                    // Jeśli nie uda się sparsować XAML-a, zostawiamy pusty dokument
                }
            }
        }

        private void OnVmRequestClose(object sender, bool dialogResult)
        {
            if (dialogResult)
            {
                // Przy zapisie: jeśli LongFormat, odczytaj zawartość RichTextBoxa do Note.Content
                if (_vm.Note.Type == NoteType.LongFormat)
                {
                    var range = new TextRange(RichTextEditor.Document.ContentStart,
                                              RichTextEditor.Document.ContentEnd);
                    using var ms = new MemoryStream();
                    range.Save(ms, DataFormats.Xaml);
                    _vm.Note.Content = Encoding.UTF8.GetString(ms.ToArray());
                }
                // Dla Regular i CheckList nie trzeba nic więcej robić—
                // wszystko zostało zsynchronizowane w ViewModelu
            }

            DialogResult = dialogResult;
            Close();
        }

        /// <summary>
        /// Zwiększa wcięcie wszystkich akapitów w zaznaczeniu o 20px.
        /// Jeżeli nie ma zaznaczenia, zmienia wcięcie akapitu, w którym jest kursor.
        /// </summary>
        private void IncreaseIndentation_Click(object sender, RoutedEventArgs e)
        {
            RichTextEditor.Focus();
            var selection = RichTextEditor.Selection;

            if (selection.IsEmpty)
            {
                if (RichTextEditor.CaretPosition.Paragraph is Paragraph singlePara)
                {
                    singlePara.TextIndent += 20;
                }
                return;
            }

            var selStart = selection.Start;
            var selEnd = selection.End;

            foreach (Block block in RichTextEditor.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    // Sprawdź, czy para znajduje się w zaznaczeniu:
                    if (para.ContentEnd.CompareTo(selStart) > 0 &&
                        para.ContentStart.CompareTo(selEnd) < 0)
                    {
                        para.TextIndent += 20;
                    }
                }
            }
        }

        /// <summary>
        /// Zmniejsza wcięcie wszystkich akapitów w zaznaczeniu o 20px (do minimum 0).
        /// </summary>
        private void DecreaseIndentation_Click(object sender, RoutedEventArgs e)
        {
            RichTextEditor.Focus();
            var selection = RichTextEditor.Selection;

            if (selection.IsEmpty)
            {
                if (RichTextEditor.CaretPosition.Paragraph is Paragraph singlePara)
                {
                    singlePara.TextIndent = Math.Max(0, singlePara.TextIndent - 20);
                }
                return;
            }

            var selStart = selection.Start;
            var selEnd = selection.End;

            foreach (Block block in RichTextEditor.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    if (para.ContentEnd.CompareTo(selStart) > 0 &&
                        para.ContentStart.CompareTo(selEnd) < 0)
                    {
                        para.TextIndent = Math.Max(0, para.TextIndent - 20);
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _vm.RequestClose -= OnVmRequestClose;
        }
    }
}

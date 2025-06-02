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

            _vm.RequestClose += OnVmRequestClose;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Jeśli edytujemy LongFormat i mamy już jakąś zawartość – załaduj ją do RichTextEditor
            if (_vm.Note.Type == NoteType.LongFormat
                && !string.IsNullOrEmpty(_vm.Note.Content))
            {
                var range = new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd);
                try
                {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_vm.Note.Content));
                    range.Load(ms, DataFormats.Xaml);
                }
                catch
                {
                    // Jeżeli XAML jest uszkodzony, pozostaw pusty dokument
                }
            }
        }

        private void OnVmRequestClose(object sender, bool dialogResult)
        {
            if (dialogResult)
            {
                // Przy zapisie: jeśli LongFormat → odczytaj XAML z RichTextBoxa
                if (_vm.Note.Type == NoteType.LongFormat)
                {
                    /*var range = new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd);
                    using var ms = new MemoryStream();
                    range.Save(ms, DataFormats.Xaml);
                    _vm.Note.Content = Encoding.UTF8.GetString(ms.ToArray());*/

                    var range = new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd);
                    using (var ms = new MemoryStream())
                    {
                        range.Save(ms, DataFormats.Xaml);  // Zapisz jako XAML (cały FlowDocument)
                        ms.Position = 0;
                        using (var reader = new StreamReader(ms))
                        {
                            _vm.Note.Content = reader.ReadToEnd();  // Zapisz do Content w modelu
                        }
                    }

                }
            }

            DialogResult = dialogResult;
            Close();
        }

        /// <summary>
        /// Zwiększa wcięcie wszystkich akapitów znajdujących się w zaznaczeniu o 20px.
        /// </summary>
        private void IncreaseIndentation_Click(object sender, RoutedEventArgs e)
        {
            // Najpierw ustawiamy fokus w RichTextEditor, żeby mieć aktualne zaznaczenie
            RichTextEditor.Focus();

            var selection = RichTextEditor.Selection;
            if (selection.IsEmpty)
            {
                // Jeśli nie ma zaznaczenia, po prostu weź akapit z kursora
                if (RichTextEditor.CaretPosition.Paragraph is Paragraph singlePara)
                {
                    singlePara.TextIndent += 20;
                }
                return;
            }

            // Pobieramy początek i koniec zaznaczenia
            TextPointer selStart = selection.Start;
            TextPointer selEnd = selection.End;

            // Iterujemy po wszystkich blokach w dokumencie, szukając paragrafów
            foreach (Block block in RichTextEditor.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    // Sprawdzamy, czy ten paragraf wchodzi w zakres zaznaczenia:
                    // Warunek „intersekcji”:
                    // para.ContentEnd > selStart  AND  para.ContentStart < selEnd
                    if (para.ContentEnd.CompareTo(selStart) > 0
                        && para.ContentStart.CompareTo(selEnd) < 0)
                    {
                        para.TextIndent += 20;
                    }
                }
            }
        }

        /// <summary>
        /// Zmniejsza wcięcie wszystkich akapitów w zaznaczeniu o 20px (maksymalnie do 0).
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

            TextPointer selStart = selection.Start;
            TextPointer selEnd = selection.End;

            foreach (Block block in RichTextEditor.Document.Blocks)
            {
                if (block is Paragraph para)
                {
                    if (para.ContentEnd.CompareTo(selStart) > 0
                        && para.ContentStart.CompareTo(selEnd) < 0)
                    {
                        para.TextIndent = Math.Max(0, para.TextIndent - 20);
                    }
                }
            }
        }
    }
}

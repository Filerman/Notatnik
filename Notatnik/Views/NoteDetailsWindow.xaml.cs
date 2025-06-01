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

            // Subskrybujemy zdarzenie zamknięcia z VM
            _vm.RequestClose += OnVmRequestClose;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Jeżeli edytujemy notatkę typu LongFormat i Content nie jest pusty,
            // spróbuj załadować go jako czysty XAML. Jeśli nie wyjdzie, spróbuj jako XamlPackage.
            if (_vm.Note.Type == NoteType.LongFormat &&
                !string.IsNullOrEmpty(_vm.Note.Content))
            {
                var contentStr = _vm.Note.Content;
                var trimmed = contentStr.TrimStart();

                // 1) Spróbuj wczytać jako czysty XAML
                if (trimmed.StartsWith("<FlowDocument", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var range = new TextRange(RichTextEditor.Document.ContentStart,
                                                  RichTextEditor.Document.ContentEnd);
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(contentStr));
                        range.Load(ms, DataFormats.Xaml);
                        return;
                    }
                    catch
                    {
                        RichTextEditor.Document = new FlowDocument();
                        return;
                    }
                }

                // 2) Spróbuj wczytać jako pakiet XAML (XamlPackage)
                try
                {
                    var range = new TextRange(RichTextEditor.Document.ContentStart,
                                              RichTextEditor.Document.ContentEnd);
                    using var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(contentStr));
                    range.Load(ms2, DataFormats.XamlPackage);
                }
                catch
                {
                    RichTextEditor.Document = new FlowDocument();
                }
            }
        }

        private void OnVmRequestClose(object sender, bool dialogResult)
        {
            if (dialogResult && _vm.Note.Type == NoteType.LongFormat)
            {
                // Pobierz z RichTextBoxa i spróbuj zapisać jako czysty XAML
                var range = new TextRange(RichTextEditor.Document.ContentStart,
                                          RichTextEditor.Document.ContentEnd);
                using var ms = new MemoryStream();
                try
                {
                    range.Save(ms, DataFormats.Xaml);
                    _vm.Note.Content = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch
                {
                    // Jeżeli z jakiegoś powodu zapis jako czysty XAML się nie powiedzie,
                    // spróbuj zapisać jako pakiet XAML
                    try
                    {
                        using var ms2 = new MemoryStream();
                        range.Save(ms2, DataFormats.XamlPackage);
                        _vm.Note.Content = Encoding.UTF8.GetString(ms2.ToArray());
                    }
                    catch
                    {
                        // Jeśli nawet to się nie uda, zostaw Content bez zmian
                    }
                }
            }

            DialogResult = dialogResult;
            Close();
        }

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
                    if (para.ContentEnd.CompareTo(selStart) > 0 &&
                        para.ContentStart.CompareTo(selEnd) < 0)
                    {
                        para.TextIndent += 20;
                    }
                }
            }
        }

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

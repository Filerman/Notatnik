using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
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

            // Obsługa zapisu sformatowanej treści przy zamknięciu
            _vm.RequestClose += OnVmRequestClose;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Jeśli edytujemy LongFormat i mamy już jakąś zawartość – załaduj ją
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
                    // nie udało się załadować XAML-a – zostaw pusty dokument
                }
            }
        }

        private void OnVmRequestClose(object sender, bool dialogResult)
        {
            if (dialogResult)
            {
                // Przed zapisaniem DTO do bazy: wyciągnij XAML z RichTextBoxa
                if (_vm.Note.Type == NoteType.LongFormat)
                {
                    var range = new TextRange(RichTextEditor.Document.ContentStart, RichTextEditor.Document.ContentEnd);
                    using var ms = new MemoryStream();
                    range.Save(ms, DataFormats.Xaml);
                    _vm.Note.Content = Encoding.UTF8.GetString(ms.ToArray());
                }
                // RegularPanel i ChecklistPanel już mają dwukierunkowe bindingi na Content i ChecklistItems
            }

            // Zamknij okno jako dialog
            DialogResult = dialogResult;
            Close();
        }
    }
}

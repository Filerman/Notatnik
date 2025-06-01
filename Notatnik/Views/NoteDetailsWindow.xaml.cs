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

        public NoteDetailsWindow(NoteDetailsViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            DataContext = _vm;
            _vm.RequestClose += Vm_RequestClose;

            if (_vm.Note.Type == NoteType.LongFormat && !string.IsNullOrEmpty(_vm.Note.Content))
            {
                try
                {
                    RichTextEditor.Document =
                        (FlowDocument)XamlReader.Parse(_vm.Note.Content);
                }
                catch { RichTextEditor.Document = new FlowDocument(); }
            }
        }

        /* ---------- zapisz / anuluj ---------- */

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Note.Type == NoteType.LongFormat)
            {
                var range = new TextRange(RichTextEditor.Document.ContentStart,
                                          RichTextEditor.Document.ContentEnd);
                using var ms = new MemoryStream();
                range.Save(ms, DataFormats.Xaml);
                _vm.Note.Content = Encoding.UTF8.GetString(ms.ToArray());
            }
            else if (_vm.Note.Type == NoteType.CheckList)
            {
                _vm.Note.ChecklistItems.RemoveAll(ci => string.IsNullOrWhiteSpace(ci.Text));
            }

            _vm.Save();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => _vm.Cancel();

        private void Vm_RequestClose(object? sender, bool ok)
        {
            DialogResult = ok;
            Close();
        }

        /* ---------- checklist ---------- */
        private void AddChecklistItem_Click(object sender, RoutedEventArgs e) =>
            _vm.AddChecklistItem();

        /* ---------- formatowanie ---------- */
        private void Bold_Click(object s, RoutedEventArgs e) =>
            EditingCommands.ToggleBold.Execute(null, RichTextEditor);

        private void Italic_Click(object s, RoutedEventArgs e) =>
            EditingCommands.ToggleItalic.Execute(null, RichTextEditor);

        private void Underline_Click(object s, RoutedEventArgs e) =>
            EditingCommands.ToggleUnderline.Execute(null, RichTextEditor);

        private void IncreaseFont_Click(object s, RoutedEventArgs e) =>
            ChangeFontSize(+2);

        private void DecreaseFont_Click(object s, RoutedEventArgs e) =>
            ChangeFontSize(-2);

        private void ChangeFontSize(double delta)
        {
            var sel = RichTextEditor.Selection;
            if (sel.IsEmpty) return;

            var current = sel.GetPropertyValue(TextElement.FontSizeProperty);
            double size = current == DependencyProperty.UnsetValue ? 12 : (double)current;
            sel.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Max(6, size + delta));
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _vm.RequestClose -= Vm_RequestClose;
        }
    }
}

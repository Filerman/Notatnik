using System.Windows;
using Notatnik.Models;

namespace Notatnik.Views
{
    public partial class SelectNoteTypeWindow : Window
    {
        public NoteType SelectedType { get; private set; }

        public SelectNoteTypeWindow()
        {
            InitializeComponent();
            SelectedType = NoteType.CheckList;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (RbCheckList.IsChecked == true)
                SelectedType = NoteType.CheckList;
            else if (RbRegular.IsChecked == true)
                SelectedType = NoteType.Regular;
            else if (RbLongFormat.IsChecked == true)
                SelectedType = NoteType.LongFormat;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

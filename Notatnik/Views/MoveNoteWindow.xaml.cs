using System.Windows;
using Notatnik.Data;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class MoveNoteWindow : Window
    {
        public MoveNoteWindow(AppDbContext db, int currentFolderId)
        {
            InitializeComponent();
            DataContext = new MoveNoteViewModel(db, currentFolderId);
        }

        public Models.Folder SelectedFolder =>
            (DataContext as MoveNoteViewModel)?.SelectedFolder;

        private void TreeView_SelectedItemChanged(object sender,
            RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is MoveNoteViewModel vm)
                vm.SelectedFolder = e.NewValue as Models.Folder;
        }
    }
}

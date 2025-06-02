using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Notatnik.Models;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListViewFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null) return;
            if (ListViewFolders.SelectedItems.Count == 1)
                ViewModel.SelectedFolder = ListViewFolders.SelectedItem as Folder;
        }

        private void HeaderCheckboxFolders_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = true;
        }

        private void HeaderCheckboxFolders_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var folder in ViewModel.Folders)
                folder.IsMarkedForDeletion = false;
        }

        private void HeaderCheckboxNotes_Checked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = true;
        }

        private void HeaderCheckboxNotes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            foreach (var note in ViewModel.Notes)
                note.IsMarkedForDeletion = false;
        }
        private void ListViewNotes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null && !(originalSource is ListViewItem))
            {
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }

            if (originalSource is ListViewItem)
            {
                if (vm.SingleSelectedNote != null && vm.EditNoteCommand.CanExecute(null))
                {
                    vm.EditNoteCommand.Execute(null);
                }
            }
        }

    }
}

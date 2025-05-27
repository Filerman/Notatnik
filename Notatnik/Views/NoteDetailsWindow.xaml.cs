using System.Windows;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class NoteDetailsWindow : Window
    {
        public NoteDetailsWindow(NoteDetailsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RequestClose += (s, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}

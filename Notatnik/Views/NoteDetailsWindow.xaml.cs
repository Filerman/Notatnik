using System.Windows;
using Notatnik.ViewModels;

namespace Notatnik.Views
{
    public partial class NoteDetailsWindow : Window
    {
        public NoteDetailsWindow(NoteDetailsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Subskrybujemy RequestClose, żeby zamknąć okno z odpowiednim DialogResult
            viewModel.RequestClose += ViewModel_RequestClose;
        }

        private void ViewModel_RequestClose(object sender, bool shouldSave)
        {
            // Jeśli shouldSave == true, użytkownik kliknął „Zapisz”; w przeciwnym razie „Anuluj”
            this.DialogResult = shouldSave;
            this.Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is NoteDetailsViewModel vm)
            {
                vm.RequestClose -= ViewModel_RequestClose;
            }
        }
    }
}

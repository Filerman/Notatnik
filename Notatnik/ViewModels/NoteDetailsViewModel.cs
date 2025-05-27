using System;
using System.ComponentModel;
using System.Windows.Input;
using Notatnik.Models;
using Notatnik.Commands;

namespace Notatnik.ViewModels
{
    public class NoteDetailsViewModel : INotifyPropertyChanged
    {
        public Note Note { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NoteDetailsViewModel(Note note)
        {
            Note = note;
            SaveCommand = new RelayCommand(o => OnRequestClose(true));
            CancelCommand = new RelayCommand(o => OnRequestClose(false));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // zdarzenie do zamknięcia okna
        public event EventHandler<bool> RequestClose;
        private void OnRequestClose(bool result) =>
            RequestClose?.Invoke(this, result);
    }
}

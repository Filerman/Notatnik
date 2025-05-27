using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Notatnik.Commands;
using Notatnik.Models;

namespace Notatnik.ViewModels
{
    public class NoteDetailsViewModel : INotifyPropertyChanged
    {
        public Note Note { get; }

        // kolekcja pozycji checklisty
        public ObservableCollection<ChecklistItem> ChecklistItems { get; }

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NoteDetailsViewModel(Note note)
        {
            Note = note;

            // inicjalizacja kolekcji
            ChecklistItems = new ObservableCollection<ChecklistItem>(note.Type == NoteType.CheckList
                ? note.ChecklistItems
                : new ChecklistItem[0]);

            // komendy
            AddItemCommand = new RelayCommand(_ => AddItem());
            RemoveItemCommand = new RelayCommand(o => RemoveItem(o as ChecklistItem), o => o is ChecklistItem);
            SaveCommand = new RelayCommand(_ => OnRequestClose(true));
            CancelCommand = new RelayCommand(_ => OnRequestClose(false));
        }

        private void AddItem()
        {
            var item = new ChecklistItem
            {
                Text = string.Empty,
                IsChecked = false,
                Note = Note
            };
            Note.ChecklistItems.Add(item);
            ChecklistItems.Add(item);
            OnPropertyChanged(nameof(ChecklistItems));
        }

        private void RemoveItem(ChecklistItem item)
        {
            if (item == null) return;
            Note.ChecklistItems.Remove(item);
            ChecklistItems.Remove(item);
            OnPropertyChanged(nameof(ChecklistItems));
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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Notatnik.Commands;
using Notatnik.Data;
using Notatnik.Models;

namespace Notatnik.ViewModels
{
    public class NoteDetailsViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        public Note Note { get; }

        public ObservableCollection<ChecklistItem> ChecklistItems { get; }
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTags { get; } = new ObservableCollection<string>();

        private string _newTagText;
        public string NewTagText
        {
            get => _newTagText;
            set
            {
                if (_newTagText != value)
                {
                    _newTagText = value;
                    OnPropertyChanged(nameof(NewTagText));
                    _addTagCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }

        private RelayCommand _addTagCommand;
        public ICommand AddTagCommand => _addTagCommand;

        public ICommand RemoveTagCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public NoteDetailsViewModel(Note note, AppDbContext db)
        {
            Note = note;
            _db = db;

            ChecklistItems = new ObservableCollection<ChecklistItem>(note.Type == NoteType.CheckList
                ? note.ChecklistItems
                : new ChecklistItem[0]);

            if (note.Tags != null)
            {
                foreach (var tag in note.Tags)
                {
                    Tags.Add(tag.Name);
                }
            }

            AddItemCommand = new RelayCommand(_ => AddItem());
            RemoveItemCommand = new RelayCommand(o => RemoveItem(o as ChecklistItem), o => o is ChecklistItem);

            _addTagCommand = new RelayCommand(_ => AddTag(), _ => CanAddTag());
            RemoveTagCommand = new RelayCommand(o => RemoveTag(o as string), o => o is string);

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

        private bool CanAddTag()
        {
            var tag = NewTagText?.Trim() ?? "";
            return !string.IsNullOrWhiteSpace(tag) && !tag.Contains(" ") && !Tags.Contains(tag);
        }

        private void AddTag()
        {
            var tag = NewTagText.Trim();

            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
                NewTagText = string.Empty;
            }
        }

        private void RemoveTag(string tag)
        {
            if (tag != null)
            {
                Tags.Remove(tag);
            }
        }

        public void SaveTagsToNote()
        {
            // Wczytaj wszystkie istniejące tagi z bazy
            var existingTags = _db.Tags.ToList();

            Note.Tags.Clear();

            foreach (var tagName in Tags)
            {
                // Szukaj po nazwie – unikniesz duplikatów
                var existingTag = existingTags.FirstOrDefault(t => t.Name == tagName);
                if (existingTag != null)
                {
                    Note.Tags.Add(existingTag);
                }
                else
                {
                    var newTag = new Tag { Name = tagName };
                    _db.Tags.Add(newTag); // dodaj do kontekstu
                    Note.Tags.Add(newTag);
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public event EventHandler<bool> RequestClose;
        private void OnRequestClose(bool result)
        {
            if (result)
            {
                SaveTagsToNote();
            }
            RequestClose?.Invoke(this, result);
        }
    }
}

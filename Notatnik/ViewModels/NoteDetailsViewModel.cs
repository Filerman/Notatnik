using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Documents; 
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
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

        private RelayCommand _removeAvailableTagCommand;
        public ICommand RemoveAvailableTagCommand => _removeAvailableTagCommand;

        public ICommand RemoveTagCommand { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note, AppDbContext db)
        {
            Note = note ?? throw new ArgumentNullException(nameof(note));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            _db.Entry(Note).Collection(n => n.Tags).Load();
            _db.Entry(Note).Collection(n => n.ChecklistItems).Load();

            ChecklistItems = new ObservableCollection<ChecklistItem>(
                note.Type == NoteType.CheckList
                    ? note.ChecklistItems
                    : Array.Empty<ChecklistItem>()
            );

            if (note.Tags != null)
            {
                foreach (var tag in note.Tags.OrderBy(t => t.Name))
                {
                    Tags.Add(tag.Name);
                }
            }

            var allTags = _db.Tags
                             .AsNoTracking()
                             .Select(t => t.Name)
                             .OrderBy(name => name)
                             .ToList();
            foreach (var tagName in allTags)
            {
                AvailableTags.Add(tagName);
            }

            AddItemCommand = new RelayCommand(_ => AddItem());
            RemoveItemCommand = new RelayCommand(o => RemoveItem(o as ChecklistItem), o => o is ChecklistItem);

            _addTagCommand = new RelayCommand(_ => AddTag(), _ => CanAddTag());
            _removeAvailableTagCommand = new RelayCommand(o => RemoveAvailableTag(o as string), o => o is string);

            RemoveTagCommand = new RelayCommand(o => RemoveTag(o as string), o => o is string);

            SaveCommand = new RelayCommand(_ =>
            {
                if (Validate())
                    OnRequestClose(true);
            });
            CancelCommand = new RelayCommand(_ => OnRequestClose(false));
        }

        private bool Validate()
        {
            var title = (Note.Title ?? "").Trim();
            if (title.Length == 0)
            {
                MessageBox.Show("Tytuł nie może być pusty.",
                                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (title.Length > 60)
            {
                MessageBox.Show("Tytuł nie może przekraczać 60 znaków.",
                                "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            switch (Note.Type)
            {
                case NoteType.Regular:
                    if (string.IsNullOrWhiteSpace(Note.Content))
                    {
                        MessageBox.Show("Notatka nie może być pusta.",
                                        "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    break;

                case NoteType.LongFormat:
            break;

                case NoteType.CheckList:
                    bool anyItem = ChecklistItems.Any(i => !string.IsNullOrWhiteSpace(i.Text));
                    if (!anyItem)
                    {
                        MessageBox.Show("Lista zadań nie zawiera żadnych pozycji.",
                                        "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    break;
            }

            return true;
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
            return !string.IsNullOrWhiteSpace(tag)
                   && !tag.Contains(" ")
                   && !Tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase);
        }

        private void AddTag()
        {
            var tag = NewTagText.Trim();

            if (!Tags.Contains(tag, StringComparer.InvariantCultureIgnoreCase))
            {
                Tags.Add(tag);

                if (!AvailableTags.Contains(tag, StringComparer.InvariantCultureIgnoreCase))
                {
                    AvailableTags.Add(tag);
                }
            }

            NewTagText = string.Empty;
        }

        private void RemoveTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (Tags.Contains(tag))
            {
                Tags.Remove(tag);
            }
        }

        private void RemoveAvailableTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return;

            var lowerName = tagName.ToLower();

            var tagEntity = _db.Tags
                               .Include(t => t.Notes)
                               .FirstOrDefault(t => t.Name.ToLower() == lowerName);

            if (tagEntity != null)
            {
                foreach (var note in tagEntity.Notes.ToList())
                {
                    note.Tags.Remove(tagEntity);
                }

                _db.Tags.Remove(tagEntity);
                _db.SaveChanges();
            }

            if (AvailableTags.Contains(tagName))
            {
                AvailableTags.Remove(tagName);
            }

            if (Tags.Contains(tagName))
            {
                Tags.Remove(tagName);
            }

            OnPropertyChanged(nameof(AvailableTags));
            OnPropertyChanged(nameof(Tags));
        }

        private void SaveTagsToNote()
        {
            var existingTags = _db.Tags.ToList();

            _db.Entry(Note).Collection(n => n.Tags).Load();
            Note.Tags.Clear();

            foreach (var tagName in Tags)
            {
                var exist = existingTags
                            .FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));

                if (exist != null)
                {
                    Note.Tags.Add(exist);
                }
                else
                {
                    var newTag = new Tag { Name = tagName };
                    _db.Tags.Add(newTag);
                    Note.Tags.Add(newTag);
                    existingTags.Add(newTag);
                }
            }

            _db.SaveChanges();
        }

        private void OnRequestClose(bool dialogResult)
        {
            if (dialogResult)
            {
                try
                {
                    SaveTagsToNote();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd przy zapisie tagów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            RequestClose?.Invoke(this, dialogResult);
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

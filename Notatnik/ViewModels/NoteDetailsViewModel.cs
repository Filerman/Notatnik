using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
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

        // Komendy dla checklisty:
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }

        // Komendy dla tagów:
        private RelayCommand _addTagCommand;
        public ICommand AddTagCommand => _addTagCommand;

        private RelayCommand _removeAvailableTagCommand;
        public ICommand RemoveAvailableTagCommand => _removeAvailableTagCommand;

        public ICommand RemoveTagCommand { get; }

        // Komendy Zapisz / Anuluj
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note, AppDbContext db)
        {
            Note = note ?? throw new ArgumentNullException(nameof(note));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            // 1) Upewnij się, że relacje Note.Tags i Note.ChecklistItems są załadowane
            _db.Entry(Note).Collection(n => n.Tags).Load();
            _db.Entry(Note).Collection(n => n.ChecklistItems).Load();

            // 2) Wczytanie checklisty (jeśli Note.Type == CheckList), w przeciwnym razie pusta lista
            ChecklistItems = new ObservableCollection<ChecklistItem>(
                note.Type == NoteType.CheckList
                    ? note.ChecklistItems
                    : Array.Empty<ChecklistItem>()
            );

            // 3) Wczytaj z Note istniejące tagi (same nazwy) do kolekcji Tags
            if (note.Tags != null)
            {
                foreach (var tag in note.Tags.OrderBy(t => t.Name))
                {
                    Tags.Add(tag.Name);
                }
            }

            // 4) Załaduj wszystkie dostępne tagi z bazy do AvailableTags (ComboBox)
            var allTags = _db.Tags
                             .AsNoTracking()
                             .Select(t => t.Name)
                             .OrderBy(name => name)
                             .ToList();
            foreach (var tagName in allTags)
            {
                AvailableTags.Add(tagName);
            }

            // 5) Inicjalizacja komend
            AddItemCommand = new RelayCommand(_ => AddItem());
            RemoveItemCommand = new RelayCommand(o => RemoveItem(o as ChecklistItem), o => o is ChecklistItem);

            _addTagCommand = new RelayCommand(_ => AddTag(), _ => CanAddTag());
            _removeAvailableTagCommand = new RelayCommand(o => RemoveAvailableTag(o as string), o => o is string);

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
            // warunki: niepusty, nie zawiera spacji, i nie ma w kolekcji Tags (ignorując wielkość liter)
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

                // Jeśli w AvailableTags jeszcze nie ma tej nazwy, dodajemy ją tam również:
                if (!AvailableTags.Contains(tag, StringComparer.InvariantCultureIgnoreCase))
                {
                    AvailableTags.Add(tag);
                    // Nowy tag zostanie w bazie utworzony dopiero przy SaveTagsToNote()
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

        /// <summary>
        /// Usuwa wybrany tag z całej aplikacji:
        /// 1) Jeśli istnieje w bazie – usuwa rekord Tag (a EF Core usunie powiązania w tabeli łączącej).
        /// 2) Usuwa go z AvailableTags.
        /// 3) Jeśli notatka miała go już przypisanego – usuwa z Note.Tags i z kolekcji Tags.
        /// </summary>
        private void RemoveAvailableTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return;

            // Konwertujemy raz na małe litery, aby można było porównać z kolumną w SQL
            var lowerName = tagName.ToLower();

            var tagEntity = _db.Tags
                               .Include(t => t.Notes)
                               .FirstOrDefault(t => t.Name.ToLower() == lowerName);

            if (tagEntity != null)
            {
                // Usuń powiązania NOTATKA ↔ TAG (jeśli istnieją)
                foreach (var note in tagEntity.Notes.ToList())
                {
                    note.Tags.Remove(tagEntity);
                }

                // Usuń sam tag z bazy
                _db.Tags.Remove(tagEntity);
                _db.SaveChanges();
            }

            // Usuń z AvailableTags
            if (AvailableTags.Contains(tagName))
            {
                AvailableTags.Remove(tagName);
            }

            // Usuń z przypisanych tagów tej notatki, jeśli tu był
            if (Tags.Contains(tagName))
            {
                Tags.Remove(tagName);
            }

            OnPropertyChanged(nameof(AvailableTags));
            OnPropertyChanged(nameof(Tags));
        }

        /// <summary>
        /// Przy zapisie notatki: tworzymy/aktualizujemy relacje wiele-do-wielu Note->Tag.
        /// </summary>
        private void SaveTagsToNote()
        {
            // 1) Załaduj wszystkie encje Tag z bazy (żeby nie robić kolejnych odpytań),
            //    tak by po nazwie móc znaleźć istniejący Tag.
            var existingTags = _db.Tags.ToList();

            // 2) Upewnij się, że Note.Tags jest załadowane
            _db.Entry(Note).Collection(n => n.Tags).Load();
            Note.Tags.Clear();

            // 3) Dla każdej nazwy w Tags:
            foreach (var tagName in Tags)
            {
                var exist = existingTags
                            .FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));

                if (exist != null)
                {
                    // Podpinamy istniejący
                    Note.Tags.Add(exist);
                }
                else
                {
                    // Tworzymy nowy
                    var newTag = new Tag { Name = tagName };
                    _db.Tags.Add(newTag);
                    Note.Tags.Add(newTag);
                    existingTags.Add(newTag);
                }
            }

            // 4) Zapisz w bazie (zarówno nowe encje Tag, jak i relacje wiele-do-wielu)
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

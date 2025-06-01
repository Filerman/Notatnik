using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

        // Kolekcja bindowana w XAML (ItemsControl w CheckListPanel)
        public ObservableCollection<ChecklistItem> ChecklistItems { get; }

        // Komendy bindowane w XAML:
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }

        // Flagi widoczności paneli w XAML (DataTrigger w XAML)
        public bool IsChecklist => Note.Type == NoteType.CheckList;
        public bool IsRegular => Note.Type == NoteType.Regular;
        public bool IsLongFormat => Note.Type == NoteType.LongFormat;

        // Zdarzenie zamykające okno (View nasłuchuje, aby zamknąć się z wynikiem ok/false)
        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note)
        {
            // Utwórz nowy kontekst (AppDbContextFactory tak, jak masz w projekcie)
            _db = new AppDbContextFactory().CreateDbContext(null);

            Note = note;
            ChecklistItems = new ObservableCollection<ChecklistItem>();

            // Jeśli to edycja istniejącej notatki typu CheckList, załaduj z bazy zapisane elementy
            if (Note.Id != 0 && Note.Type == NoteType.CheckList)
            {
                _db.Entry(Note)
                   .Collection(n => n.ChecklistItems)
                   .Load();

                foreach (var ci in Note.ChecklistItems)
                {
                    ChecklistItems.Add(new ChecklistItem
                    {
                        Id = ci.Id,
                        Text = ci.Text,
                        IsChecked = ci.IsChecked,
                        NoteId = ci.NoteId
                    });
                }
            }

            // Dla nowych notatek ChecklistItems zostaje pusta — użytkownik może dodawać elementy

            // Inicjalizacja komend:
            SaveCommand = new RelayCommand(_ => OnSave());
            CancelCommand = new RelayCommand(_ => OnCancel());
            AddItemCommand = new RelayCommand(_ => OnAddItem());
            RemoveItemCommand = new RelayCommand(obj => OnRemoveItem(obj as ChecklistItem));
        }

        private void OnAddItem()
        {
            // Dodaj pustą pozycję; UI automatycznie wyświetli linię z CheckBox + TextBox
            ChecklistItems.Add(new ChecklistItem
            {
                Text = string.Empty,
                IsChecked = false,
                NoteId = Note.Id
            });
        }

        private void OnRemoveItem(ChecklistItem item)
        {
            if (item == null) return;
            ChecklistItems.Remove(item);
        }

        private void OnSave()
        {
            Note.ModifiedAt = DateTime.Now;

            if (Note.Type == NoteType.CheckList)
            {
                // 1) Usuń z bazy wszystkie istniejące pozycje powiązane z tą notatką
                if (Note.Id != 0)
                {
                    var existing = _db.ChecklistItems
                                      .Where(ci => ci.NoteId == Note.Id)
                                      .ToList();
                    if (existing.Any())
                    {
                        _db.ChecklistItems.RemoveRange(existing);
                        _db.SaveChanges();
                    }
                }

                // 2) Wyczyść oryginalną listę w Note i przepisz z ObservableCollection
                Note.ChecklistItems.Clear();
                foreach (var ci in ChecklistItems)
                {
                    if (string.IsNullOrWhiteSpace(ci.Text))
                        continue; // pomiń puste
                    var newCi = new ChecklistItem
                    {
                        Text = ci.Text.Trim(),
                        IsChecked = ci.IsChecked,
                        Note = Note
                        // NoteId ustawi EF po zapisie Note
                    };
                    Note.ChecklistItems.Add(newCi);
                }
            }

            // Teraz dodajemy lub aktualizujemy notatkę w kontekście EF
            if (Note.Id == 0)
            {
                Note.CreatedAt = DateTime.Now;
                _db.Notes.Add(Note);
            }
            else
            {
                // Odpinamy nawigację do Folder, żeby EF nie miał konfliktów przy update
                Note.Folder = null;
                _db.Notes.Update(Note);
            }

            _db.SaveChanges();
            RequestClose?.Invoke(this, true);
        }

        private void OnCancel()
        {
            RequestClose?.Invoke(this, false);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}

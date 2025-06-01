using System;
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

        /* -------- Flagi widoku -------- */

        public bool IsChecklist => Note.Type == NoteType.CheckList;
        public bool IsRegular => Note.Type == NoteType.Regular;
        public bool IsLongFormat => Note.Type == NoteType.LongFormat;

        /* -------- Komendy -------- */

        public ICommand RemoveChecklistItemCommand { get; }

        /* -------- Zamknięcie okna -------- */

        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note)
        {
            _db = new AppDbContextFactory().CreateDbContext(null);
            Note = note;

            // Jeśli edytujemy istniejącą checklistę – dociągnij jej elementy
            if (Note.Id != 0 && Note.Type == NoteType.CheckList)
                _db.Entry(Note).Collection(n => n.ChecklistItems).Load();

            RemoveChecklistItemCommand =
                new RelayCommand(o => RemoveChecklistItem(o as ChecklistItem));
        }

        /* ---------- Checklist helpers ---------- */

        public void AddChecklistItem() =>
            Note.ChecklistItems.Add(new ChecklistItem { Text = string.Empty });

        private void RemoveChecklistItem(ChecklistItem item)
        {
            if (item == null) return;
            Note.ChecklistItems.Remove(item);
        }

        /* ---------- Zapis / anulowanie ---------- */

        public void Save()
        {
            Note.ModifiedAt = DateTime.Now;

            // ChecklistItems – dodaj/aktualizuj w kontekście
            if (Note.Type == NoteType.CheckList)
            {
                foreach (var ci in Note.ChecklistItems)
                {
                    if (ci.Id == 0)
                        _db.ChecklistItems.Add(ci);
                    else
                        _db.ChecklistItems.Update(ci);
                }
            }

            if (Note.Id == 0)            // nowa notatka
                _db.Notes.Add(Note);
            else                         // edycja
            {
                Note.Folder = null;      // odpinamy nawigację
                _db.Notes.Update(Note);
            }

            _db.SaveChanges();
            RequestClose?.Invoke(this, true);
        }

        public void Cancel() => RequestClose?.Invoke(this, false);

        /* ---------- INotifyPropertyChanged ---------- */

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}

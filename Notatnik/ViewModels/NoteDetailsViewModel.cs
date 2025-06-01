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

        public ObservableCollection<ChecklistItem> ChecklistItems { get; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }

        public bool IsChecklist => Note.Type == NoteType.CheckList;
        public bool IsRegular => Note.Type == NoteType.Regular;
        public bool IsLongFormat => Note.Type == NoteType.LongFormat;

        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note)
        {
            _db = new AppDbContextFactory().CreateDbContext(null);
            Note = note;
            ChecklistItems = new ObservableCollection<ChecklistItem>();

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

            SaveCommand = new RelayCommand(_ => OnSave());
            CancelCommand = new RelayCommand(_ => OnCancel());
            AddItemCommand = new RelayCommand(_ => OnAddItem());
            RemoveItemCommand = new RelayCommand(obj => OnRemoveItem(obj as ChecklistItem));
        }

        private void OnAddItem()
        {
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

                Note.ChecklistItems.Clear();
                foreach (var ci in ChecklistItems)
                {
                    if (string.IsNullOrWhiteSpace(ci.Text))
                        continue;

                    var newCi = new ChecklistItem
                    {
                        Text = ci.Text.Trim(),
                        IsChecked = ci.IsChecked,
                        Note = Note
                    };
                    Note.ChecklistItems.Add(newCi);
                }
            }

            if (Note.Id == 0)
            {
                Note.CreatedAt = DateTime.Now;
                _db.Notes.Add(Note);
            }
            else
            {
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

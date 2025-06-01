using System;
using System.ComponentModel;
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Zdarzenie do zasygnalizowania View (NoteDetailsWindow), że należy się zamknąć.
        /// Parametr bool: true = zapisz, false = anuluj.
        /// </summary>
        public event EventHandler<bool> RequestClose;

        public NoteDetailsViewModel(Note note)
        {
            // Tworzymy nowy kontekst (oddzielny od tego w MainViewModel)
            _db = new AppDbContextFactory().CreateDbContext(null);

            // Przypisujemy przekazaną notatkę (może być nowa lub już istniejąca)
            Note = note;

            // Inicjalizujemy komendy
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        /// <summary>
        /// Sprawdza, czy można zapisać notatkę (np. tytuł nie jest pusty).
        /// </summary>
        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Note.Title);
        }

        /// <summary>
        /// Logika zapisu notatki do bazy.
        /// Rozpoznajemy, czy jest to nowa notatka (Note.Id == 0), czy edycja.
        /// W przypadku edycji upewniamy się, że nawigacyjna referencja Folder jest odłączona,
        /// aby EF nie próbował wstawić folderu ponownie.
        /// </summary>
        private void Save()
        {
            // Ustawiamy datę modyfikacji
            Note.ModifiedAt = DateTime.Now;

            if (Note.Id == 0)
            {
                // Nowa notatka – dodajemy
                _db.Notes.Add(Note);
            }
            else
            {
                // Istniejąca notatka – edytujemy
                // Odłączamy nawigacyjną referencję do Folder, żeby nie próbować wstawić folderu
                Note.Folder = null;

                // Załączamy notatkę i oznaczamy jako Modified
                _db.Notes.Attach(Note);
                _db.Entry(Note).State = EntityState.Modified;
            }

            // Zapisujemy zmiany w kontekście
            _db.SaveChanges();

            // Sygnalizujemy oknu, że zapis zakończony sukcesem
            RequestClose?.Invoke(this, true);
        }

        /// <summary>
        /// Anulowanie – zwracamy false, co spowoduje zamknięcie okna bez zapisu.
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(this, false);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}

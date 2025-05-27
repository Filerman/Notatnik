using Microsoft.EntityFrameworkCore;
using Notatnik.Models;

namespace Notatnik.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Note> Notes { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            // Tworzymy bazę, jeśli nie istnieje.
            // Ustawiamy EnsureCreated zamiast Migrate, bo w tym momencie
            // nie masz jeszcze wygenerowanych migracji EF Core.
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Note <-> Tag (many-to-many)
            modelBuilder.Entity<Note>()
                        .HasMany(n => n.Tags)
                        .WithMany(t => t.Notes);

            // Folder <-> Note (one-to-many)
            modelBuilder.Entity<Folder>()
                        .HasMany(f => f.Notes)
                        .WithOne(n => n.Folder)
                        .HasForeignKey(n => n.FolderId);

            // Folder hierarchy (self-referencing)
            modelBuilder.Entity<Folder>()
                        .HasMany(f => f.Subfolders)
                        .WithOne(f => f.ParentFolder)
                        .HasForeignKey(f => f.ParentFolderId);

            // ChecklistItem <-> Note (one-to-many)
            modelBuilder.Entity<ChecklistItem>()
                        .HasOne(ci => ci.Note)
                        .WithMany(n => n.ChecklistItems)
                        .HasForeignKey(ci => ci.NoteId);
        }
    }
}

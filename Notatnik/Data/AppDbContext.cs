using Microsoft.EntityFrameworkCore;
using Notatnik.Models;
using System.Linq;

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
            Database.EnsureCreated();
            if (!Folders.Any())
            {
                Folders.Add(new Folder { Name = "Notatki" });
                SaveChanges();
            }
        }

        public AppDbContext()
            : this(new DbContextOptionsBuilder<AppDbContext>()
                     .UseSqlite("Data Source=Notatnik.db")
                     .Options)
        { }

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

            // Folder - hierarchia
            modelBuilder.Entity<Folder>()
                        .HasMany(f => f.Subfolders)
                        .WithOne(f => f.ParentFolder)
                        .HasForeignKey(f => f.ParentFolderId)
                        .OnDelete(DeleteBehavior.Restrict);

            // ChecklistItem <-> Note (one-to-many)
            modelBuilder.Entity<ChecklistItem>()
                        .HasOne(ci => ci.Note)
                        .WithMany(n => n.ChecklistItems)
                        .HasForeignKey(ci => ci.NoteId);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Notatnik.Models;

namespace Notatnik.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Note> Notes { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<Tag> Tags { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Note ←→ Tag relacja wiele-do-wielu
            modelBuilder.Entity<Note>()
                        .HasMany(n => n.Tags)
                        .WithMany(t => t.Notes);

            // Folder ←→ Note relacja jeden-do-wielu
            modelBuilder.Entity<Folder>()
                        .HasMany(f => f.Notes)
                        .WithOne(n => n.Folder)
                        .HasForeignKey(n => n.FolderId);

            // Folder hierarchia parent-child
            modelBuilder.Entity<Folder>()
                        .HasMany(f => f.Subfolders)
                        .WithOne(f => f.ParentFolder)
                        .HasForeignKey(f => f.ParentFolderId);
        }
    }
}

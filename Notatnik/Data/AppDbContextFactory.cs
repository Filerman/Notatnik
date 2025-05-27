using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notatnik.Data
{
    // używane przez EF Core w czasie migracji
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseSqlite("Data Source=notatnik.db");
            return new AppDbContext(builder.Options);
        }
    }
}

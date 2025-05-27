using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notatnik.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            builder.UseSqlite("Data Source=notatnik.db");

            var context = new AppDbContext(builder.Options);
            // też wywołujemy EnsureCreated(), by mieć pewność, że przy
            // uruchomieniu z poziomu EF Tools (dotnet ef…) struktura jest gotowa:
            context.Database.EnsureCreated();
            return context;
        }
    }
}

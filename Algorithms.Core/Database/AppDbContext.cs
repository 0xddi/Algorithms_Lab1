using Microsoft.EntityFrameworkCore;

namespace Algorithms.Core.Database;

public class AppDbContext : DbContext
{
    public DbSet<ExperimentResult> Results { get; set; } = null!;

    public AppDbContext()
    {
        // Автоматически создает файл БД benchmarks.db и нужные таблицы при первом запуске
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Указываем путь к файлу базы данных SQLite
        optionsBuilder.UseSqlite("Data Source=benchmarks.db");
    }
}
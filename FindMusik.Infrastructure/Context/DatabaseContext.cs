using FindMusik.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FindMusik.Infrastructure.Context;

public class DatabaseContext:DbContext
{

    public DbSet<User> Users { get; set; }
    public DatabaseContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=mydatabase.db");
    }
}
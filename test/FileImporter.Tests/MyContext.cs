using Microsoft.EntityFrameworkCore;

namespace FileImporter.Tests;

class MyContext(DbContextOptions<MyContext> options) : DbContext(options)
{
    public DbSet<FileData> Data { get; set; } = null!;
}
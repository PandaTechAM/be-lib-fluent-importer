using Microsoft.EntityFrameworkCore;

namespace FileImporter.Demo.Controllers;

class MyContext(DbContextOptions<MyContext> options) : DbContext(options)
{
    public DbSet<FileData> Data { get; set; } = null!;
}
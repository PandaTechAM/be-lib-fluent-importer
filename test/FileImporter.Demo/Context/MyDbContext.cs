using FileImporter.Demo.Models;
using Microsoft.EntityFrameworkCore;

namespace FileImporter.Demo.Context;

public class MyDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<FileData> FileData { get; set; } = null!;


    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }
}
using Microsoft.EntityFrameworkCore;

namespace Pandatech.TestApi.Controllers;

class MyContext(DbContextOptions<MyContext> options) : DbContext(options)
{
    public DbSet<FileData> Data { get; set; } = null!;
}
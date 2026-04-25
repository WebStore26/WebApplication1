
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
// ===== EF Core =====

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> opts) : base(opts) { }

    public DbSet<Order> Shop_Orders => Set<Order>();
    public DbSet<Item> Shop_Items => Set<Item>();
}

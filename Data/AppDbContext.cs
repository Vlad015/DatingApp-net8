using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Photo> Photos { get; set; } // Add the Photos DbSet

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Photos)
            .WithOne(p => p.AppUser)
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Cascade); // If user is deleted, delete their photos too

        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<Photo>().ToTable("photos"); // Ensure photos table is mapped
    }
}

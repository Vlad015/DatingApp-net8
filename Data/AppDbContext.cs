using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Photo> Photos { get; set; } // Add the Photos DbSet

    public DbSet<UserLike> Likes { get; set; }

    public DbSet<Message> Messages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Photos)
            .WithOne(p => p.AppUser)
            .HasForeignKey(p => p.AppUserId)
            .OnDelete(DeleteBehavior.Cascade); // If user is deleted, delete their photos too

        modelBuilder.Entity<AppUser>().ToTable("users");
        modelBuilder.Entity<Photo>().ToTable("photos"); // Ensure photos table is mapped

        modelBuilder.Entity<UserLike>()
            .HasKey(k => new { k.SourceUserId, k.TargetUserId });
        modelBuilder.Entity<UserLike>()
             .HasOne(s => s.SourceUser)
             .WithMany(l => l.LikedUsers)
             .HasForeignKey(s => s.SourceUserId)
             .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<UserLike>()
            .HasOne(s => s.TargetUser)
            .WithMany(l => l.LikedByUsers)
            .HasForeignKey(s => s.TargetUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Message>()
            .HasOne(x => x.Recipient)
            .WithMany(x => x.MessagesReceived)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(x => x.Sender)
            .WithMany(x => x.MessagesSent)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

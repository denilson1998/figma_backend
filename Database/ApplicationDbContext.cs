using figma_backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace figma_backend.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> dbContext) : base(dbContext)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<CanvasRoom> CanvasRooms { get; set; }

        public DbSet<CanvasComponent> CanvasComponents { get; set; }

        public DbSet<RoomUser> RoomUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.UserId);
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();
            });

            modelBuilder.Entity<CanvasRoom>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(r => r.CreatorUserId).IsRequired();

                // Relación uno-a-muchos con CanvasComponent
                entity.HasMany(r => r.Components)
                     .WithOne()
                     .HasForeignKey(c => c.RoomId)
                     .OnDelete(DeleteBehavior.Cascade);

                // Relación uno-a-muchos con RoomUser
                entity.HasMany(r => r.ConnectedUsers)
                     .WithOne()
                     .HasForeignKey(u => u.RoomId)
                     .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de CanvasComponent
            modelBuilder.Entity<CanvasComponent>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Type).IsRequired().HasMaxLength(50);
                entity.Property(c => c.PositionX).IsRequired();
                entity.Property(c => c.PositionY).IsRequired();
                entity.Property(c => c.RoomId).IsRequired();
            });

            // Configuración de RoomUser
            modelBuilder.Entity<RoomUser>(entity =>
            {
                entity.HasKey(u => u.ConnectionId);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.RoomId).IsRequired();
                entity.Property(u => u.UserId).IsRequired();

                entity.HasOne(u => u.User)
                      .WithMany(u => u.RoomUsers)
                      .HasForeignKey(u => u.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

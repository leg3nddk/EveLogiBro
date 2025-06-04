using EveLogiBro.Models;
using Microsoft.EntityFrameworkCore;

namespace EveLogiBro.Data
{
    public class LogiDbContext : DbContext
    {
        // Constructor that accepts database configuration options
        public LogiDbContext(DbContextOptions<LogiDbContext> options) : base(options)
        {
        }

        // Database tables - Entity Framework will create these automatically
        public DbSet<RepairEvent> RepairEvents { get; set; }
        public DbSet<LogiSession> LogiSessions { get; set; }

        // Configure the database relationships and constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RepairEvent table
            modelBuilder.Entity<RepairEvent>(entity =>
            {
                // Set primary key
                entity.HasKey(e => e.Id);
                
                // Configure decimal precision for ISK values (up to 18 digits, 2 decimal places)
                entity.Property(e => e.IskValue)
                    .HasColumnType("decimal(18,2)");
                
                // Make required fields non-nullable
                entity.Property(e => e.TargetName).IsRequired();
                entity.Property(e => e.LogiPilot).IsRequired();
                entity.Property(e => e.RepairType).IsRequired();
                entity.Property(e => e.Direction).IsRequired();
            });

            // Configure LogiSession table
            modelBuilder.Entity<LogiSession>(entity =>
            {
                // Set primary key
                entity.HasKey(s => s.Id);
                
                // Configure decimal precision for ISK values
                entity.Property(s => s.TotalIskValue)
                    .HasColumnType("decimal(18,2)");
                
                // Set up the relationship between LogiSession and RepairEvents
                entity.HasMany(s => s.RepairEvents)
                    .WithOne()
                    .HasForeignKey(r => r.SessionId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete all repairs when session is deleted
            });
        }
    }
}
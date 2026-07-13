using FoodLens.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodLens.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FoodLog> FoodLogs => Set<FoodLog>();
        public DbSet<DietGoal> DietGoals => Set<DietGoal>();
        public DbSet<FoodCache> FoodCache => Set<FoodCache>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed default diet goal
            modelBuilder.Entity<DietGoal>().HasData(new DietGoal
            {
                Id = 1,
                DeviceId = "default",
                DailyCalorieLimit = 2000,
                ProteinPercentage = 30,
                CarbsPercentage = 40,
                FatPercentage = 30,
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Index FoodCache by FdcId for fast cache lookups
            modelBuilder.Entity<FoodCache>()
                .HasIndex(f => f.FdcId)
                .IsUnique();

            // Index food logs by date for efficient daily queries
            modelBuilder.Entity<FoodLog>()
                .HasIndex(f => new { f.DeviceId, f.LoggedAt });
        }
    }
}

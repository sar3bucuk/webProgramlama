using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proje.Models;

namespace proje.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<GymService> GymServices { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<TrainerSpecialization> TrainerSpecializations { get; set; }
        public DbSet<TrainerService> TrainerServices { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AIRecommendation> AIRecommendations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique Constraints
            builder.Entity<GymService>()
                .HasIndex(gs => new { gs.GymId, gs.ServiceId })
                .IsUnique();

            builder.Entity<TrainerSpecialization>()
                .HasIndex(ts => new { ts.TrainerId, ts.Specialization })
                .IsUnique();

            builder.Entity<TrainerService>()
                .HasIndex(ts => new { ts.TrainerId, ts.ServiceId })
                .IsUnique();

            // Configure decimal precision
            builder.Entity<GymService>()
                .Property(gs => gs.Price)
                .HasPrecision(10, 2);

            builder.Entity<Appointment>()
                .Property(a => a.Price)
                .HasPrecision(10, 2);

            builder.Entity<Member>()
                .Property(m => m.Height)
                .HasPrecision(5, 2);

            builder.Entity<Member>()
                .Property(m => m.Weight)
                .HasPrecision(5, 2);

            // Configure relationships
            builder.Entity<GymService>()
                .HasOne(gs => gs.Gym)
                .WithMany(g => g.GymServices)
                .HasForeignKey(gs => gs.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GymService>()
                .HasOne(gs => gs.Service)
                .WithMany(s => s.GymServices)
                .HasForeignKey(gs => gs.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Trainer>()
                .HasOne(t => t.Gym)
                .WithMany(g => g.Trainers)
                .HasForeignKey(t => t.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasOne(a => a.Member)
                .WithMany(m => m.Appointments)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasOne(a => a.Trainer)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TrainerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasOne(a => a.GymService)
                .WithMany(gs => gs.Appointments)
                .HasForeignKey(a => a.GymServiceId)
                .OnDelete(DeleteBehavior.NoAction); // Cascade path sorunu için
        }
    }
}


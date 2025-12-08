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

        // DbSets - Schema.sql'deki tüm tablolar
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

            // =============================================
            // GYMS TABLE
            // =============================================
            builder.Entity<Gym>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.OpeningTime).IsRequired();
                entity.Property(e => e.ClosingTime).IsRequired();
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedDate);
                
                // Check constraint: OpeningTime < ClosingTime
                entity.ToTable(t => t.HasCheckConstraint("CK_Gyms_Time", "[OpeningTime] < [ClosingTime]"));
            });

            // =============================================
            // SERVICES TABLE
            // =============================================
            builder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
            });

            // =============================================
            // GYMSERVICES TABLE
            // =============================================
            builder.Entity<GymService>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GymId).IsRequired();
                entity.Property(e => e.ServiceId).IsRequired();
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Price).IsRequired().HasPrecision(10, 2);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Unique constraint: GymId + ServiceId
                entity.HasIndex(e => new { e.GymId, e.ServiceId }).IsUnique();
                
                // Check constraints
                entity.ToTable(t => {
                    t.HasCheckConstraint("CK_GymServices_Duration", "[Duration] > 0");
                    t.HasCheckConstraint("CK_GymServices_Price", "[Price] >= 0");
                });
                
                // Foreign keys
                entity.HasOne(gs => gs.Gym)
                    .WithMany(g => g.GymServices)
                    .HasForeignKey(gs => gs.GymId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(gs => gs.Service)
                    .WithMany(s => s.GymServices)
                    .HasForeignKey(gs => gs.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes
                entity.HasIndex(e => e.GymId);
                entity.HasIndex(e => e.ServiceId);
            });

            // =============================================
            // TRAINERS TABLE
            // =============================================
            builder.Entity<Trainer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.GymId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Bio).HasMaxLength(1000);
                entity.Property(e => e.ExperienceYears).HasDefaultValue(0);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Check constraint
                entity.ToTable(t => t.HasCheckConstraint("CK_Trainers_Experience", "[ExperienceYears] >= 0"));
                
                // Foreign key to Gym (nullable)
                entity.HasOne(t => t.Gym)
                    .WithMany(g => g.Trainers)
                    .HasForeignKey(t => t.GymId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Foreign key to IdentityUser (will be added via AddIdentityForeignKeys.sql)
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.GymId);
            });

            // =============================================
            // TRAINERSPECIALIZATIONS TABLE
            // =============================================
            builder.Entity<TrainerSpecialization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TrainerId).IsRequired();
                entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Unique constraint: TrainerId + Specialization
                entity.HasIndex(e => new { e.TrainerId, e.Specialization }).IsUnique();
                
                // Foreign key
                entity.HasOne(ts => ts.Trainer)
                    .WithMany(t => t.TrainerSpecializations)
                    .HasForeignKey(ts => ts.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============================================
            // TRAINERSERVICES TABLE
            // =============================================
            builder.Entity<TrainerService>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TrainerId).IsRequired();
                entity.Property(e => e.ServiceId).IsRequired();
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Unique constraint: TrainerId + ServiceId
                entity.HasIndex(e => new { e.TrainerId, e.ServiceId }).IsUnique();
                
                // Foreign keys
                entity.HasOne(ts => ts.Trainer)
                    .WithMany(t => t.TrainerServices)
                    .HasForeignKey(ts => ts.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(ts => ts.Service)
                    .WithMany(s => s.TrainerServices)
                    .HasForeignKey(ts => ts.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =============================================
            // TRAINERAVAILABILITY TABLE
            // =============================================
            builder.Entity<TrainerAvailability>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TrainerId).IsRequired();
                entity.Property(e => e.DayOfWeek).IsRequired();
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.IsAvailable).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Check constraints
                entity.ToTable(t => {
                    t.HasCheckConstraint("CK_TrainerAvailability_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                    t.HasCheckConstraint("CK_TrainerAvailability_Time", "[StartTime] < [EndTime]");
                });
                
                // Foreign key
                entity.HasOne(ta => ta.Trainer)
                    .WithMany(t => t.TrainerAvailabilities)
                    .HasForeignKey(ta => ta.TrainerId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes
                entity.HasIndex(e => e.TrainerId);
                entity.HasIndex(e => e.DayOfWeek);
            });

            // =============================================
            // MEMBERS TABLE
            // =============================================
            builder.Entity<Member>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.DateOfBirth);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.Height).HasPrecision(5, 2);
                entity.Property(e => e.Weight).HasPrecision(5, 2);
                entity.Property(e => e.BodyType).HasMaxLength(50);
                entity.Property(e => e.HealthConditions).HasMaxLength(500);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedDate);
                
                // Check constraints
                entity.ToTable(t => {
                    t.HasCheckConstraint("CK_Members_Height", "[Height] > 0");
                    t.HasCheckConstraint("CK_Members_Weight", "[Weight] > 0");
                });
                
                // Foreign key to IdentityUser
                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Foreign key to Gym
                entity.HasOne(m => m.Gym)
                    .WithMany()
                    .HasForeignKey(m => m.GymId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // Indexes
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.GymId);
            });

            // =============================================
            // APPOINTMENTS TABLE
            // =============================================
            builder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MemberId).IsRequired();
                entity.Property(e => e.TrainerId).IsRequired();
                entity.Property(e => e.GymServiceId).IsRequired();
                entity.Property(e => e.AppointmentDate).IsRequired();
                entity.Property(e => e.AppointmentTime).IsRequired();
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Price).IsRequired().HasPrecision(10, 2);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedDate);
                
                // Check constraints
                entity.ToTable(t => {
                    t.HasCheckConstraint("CK_Appointments_Status", "[Status] IN ('Pending', 'Approved', 'Rejected', 'Completed', 'Cancelled')");
                    t.HasCheckConstraint("CK_Appointments_Duration", "[Duration] > 0");
                    t.HasCheckConstraint("CK_Appointments_Price", "[Price] >= 0");
                });
                
                // Foreign keys
                entity.HasOne(a => a.Member)
                    .WithMany(m => m.Appointments)
                    .HasForeignKey(a => a.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // NO ACTION for Trainer to avoid cascade path issues (Trainer->Gym, GymService->Gym)
                entity.HasOne(a => a.Trainer)
                    .WithMany(t => t.Appointments)
                    .HasForeignKey(a => a.TrainerId)
                    .OnDelete(DeleteBehavior.NoAction);
                
                // NO ACTION for GymService to avoid cascade path issues
                entity.HasOne(a => a.GymService)
                    .WithMany(gs => gs.Appointments)
                    .HasForeignKey(a => a.GymServiceId)
                    .OnDelete(DeleteBehavior.NoAction);
                
                // Indexes
                entity.HasIndex(e => e.MemberId);
                entity.HasIndex(e => e.TrainerId);
                entity.HasIndex(e => e.AppointmentDate);
                entity.HasIndex(e => e.Status);
            });

            // =============================================
            // AIRECOMMENDATIONS TABLE
            // =============================================
            builder.Entity<AIRecommendation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MemberId).IsRequired();
                entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.InputData);
                entity.Property(e => e.PhotoPath).HasMaxLength(500);
                entity.Property(e => e.AIResponse);
                entity.Property(e => e.GeneratedImagePath).HasMaxLength(500);
                entity.Property(e => e.CreatedDate).IsRequired().HasDefaultValueSql("GETDATE()");
                
                // Check constraint
                entity.ToTable(t => t.HasCheckConstraint("CK_AIRecommendations_RequestType", "[RequestType] IN ('ExercisePlan', 'DietPlan', 'BodyTransformation')"));
                
                // Foreign key
                entity.HasOne(ai => ai.Member)
                    .WithMany(m => m.AIRecommendations)
                    .HasForeignKey(ai => ai.MemberId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

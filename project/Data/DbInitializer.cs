using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Migration'ları uygula (veritabanı yoksa oluşturur)
            await context.Database.MigrateAsync();

            // Rolleri oluştur
            string[] roles = { "Admin", "Member", "Trainer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Admin kullanıcısını oluştur (g231210012@sakarya.edu.tr / sau)
            var adminEmail = "g231210012@sakarya.edu.tr";
            var adminPassword = "sau";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Eğer Services tablosu boşsa, seed data ekle
            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service { Name = "Fitness", Description = "Genel fitness ve kardiyovasküler egzersizler", CreatedDate = DateTime.Now },
                    new Service { Name = "Yoga", Description = "Yoga ve esneklik egzersizleri", CreatedDate = DateTime.Now },
                    new Service { Name = "Pilates", Description = "Pilates ve core güçlendirme", CreatedDate = DateTime.Now },
                    new Service { Name = "CrossFit", Description = "CrossFit antrenmanları", CreatedDate = DateTime.Now },
                    new Service { Name = "Kardiyo", Description = "Kardiyovasküler egzersizler", CreatedDate = DateTime.Now },
                    new Service { Name = "Ağırlık Antrenmanı", Description = "Kas geliştirme ve güç antrenmanları", CreatedDate = DateTime.Now },
                    new Service { Name = "Zumba", Description = "Dans temelli kardiyo egzersizleri", CreatedDate = DateTime.Now },
                    new Service { Name = "Spinning", Description = "Bisiklet antrenmanları", CreatedDate = DateTime.Now }
                };

                await context.Services.AddRangeAsync(services);
                await context.SaveChangesAsync();
            }

            // Örnek bir spor salonu ekle (eğer yoksa)
            if (!await context.Gyms.AnyAsync())
            {
                var gym = new Gym
                {
                    Name = "Sakarya Fitness Center",
                    Address = "Sakarya Üniversitesi Kampüsü",
                    Phone = "0264 123 45 67",
                    Email = "info@sakaryafitness.com",
                    OpeningTime = new TimeSpan(6, 0, 0), // 06:00
                    ClosingTime = new TimeSpan(23, 0, 0), // 23:00
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                await context.Gyms.AddAsync(gym);
                await context.SaveChangesAsync();
            }
        }
    }
}


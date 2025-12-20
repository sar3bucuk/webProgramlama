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
        }
    }
}


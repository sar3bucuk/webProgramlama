using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using proje.Data;

namespace proje.Controllers
{
    public class SeedController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public SeedController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            try
            {
                // Rolleri oluştur
                string[] roles = { "Admin", "Member", "Trainer" };
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Admin kullanıcısını oluştur
                var adminEmail = "g231210012@sakarya.edu.tr";
                var adminPassword = "sau";

                var existingUser = await _userManager.FindByEmailAsync(adminEmail);
                if (existingUser == null)
                {
                    var adminUser = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(adminUser, "Admin");
                        return Json(new { success = true, message = $"Admin kullanıcısı oluşturuldu! Email: {adminEmail}, Şifre: {adminPassword}" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Hata: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
                    }
                }
                else
                {
                    // Kullanıcı var, şifreyi sıfırla
                    var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var resetResult = await _userManager.ResetPasswordAsync(existingUser, token, adminPassword);
                    
                    if (resetResult.Succeeded)
                    {
                        // Admin rolünü kontrol et
                        if (!await _userManager.IsInRoleAsync(existingUser, "Admin"))
                        {
                            await _userManager.AddToRoleAsync(existingUser, "Admin");
                        }
                        return Json(new { success = true, message = $"Admin kullanıcısı güncellendi! Email: {adminEmail}, Şifre: {adminPassword}" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Şifre güncellenemedi: " + string.Join(", ", resetResult.Errors.Select(e => e.Description)) });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata: " + ex.Message });
            }
        }
    }
}


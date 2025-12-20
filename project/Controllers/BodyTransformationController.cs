/*
Vücut transformasyon simülasyonu controller'ı.
Üyelerin fotoğraflarını yükleyerek egzersiz programı sonrası nasıl görüneceklerini AI ile simüle eder.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;
using proje.Services;
using System.IO;

namespace proje.Controllers
{
    [Authorize(Roles = "Member")]
    public class BodyTransformationController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly OpenAIService _openAIService;

        public BodyTransformationController(
            UserManager<IdentityUser> userManager, 
            ApplicationDbContext context,
            OpenAIService openAIService)
        {
            _userManager = userManager;
            _context = context;
            _openAIService = openAIService;
        }

        /// <summary>
        /// Vücut transformasyon simülasyonu sayfasını gösterir
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            // Egzersiz programları listesi
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Services = services;
            ViewBag.Member = member;

            return View();
        }

        /// <summary>
        /// AI ile vücut transformasyon görüntüsü oluşturur
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateTransformation(
            string exerciseProgram, 
            int duration = 3,
            string? additionalNotes = null,
            IFormFile? photo = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            if (string.IsNullOrWhiteSpace(exerciseProgram))
            {
                return Json(new { success = false, message = "Lütfen bir egzersiz programı seçin veya açıklayın." });
            }

            if (duration < 1 || duration > 12)
            {
                return Json(new { success = false, message = "Süre 1-12 ay arasında olmalıdır." });
            }

            try
            {
                // Fotoğraf kontrolü
                string? photoBase64 = null;
                if (photo != null && photo.Length > 0)
                {
                    // Fotoğrafı base64'e çevir
                    using (var memoryStream = new MemoryStream())
                    {
                        await photo.CopyToAsync(memoryStream);
                        var photoBytes = memoryStream.ToArray();
                        photoBase64 = Convert.ToBase64String(photoBytes);
                    }
                }
                else if (!string.IsNullOrEmpty(member.PhotoUrl))
                {
                    // Veritabanındaki fotoğrafı kullan
                    var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", member.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(photoPath))
                    {
                        var photoBytes = await System.IO.File.ReadAllBytesAsync(photoPath);
                        photoBase64 = Convert.ToBase64String(photoBytes);
                    }
                }

                // Mevcut vücut durumu açıklaması oluştur
                var currentBodyDescription = $"Current physique: ";
                if (member.Height.HasValue && member.Weight.HasValue)
                {
                    currentBodyDescription += $"{member.Height}cm tall, {member.Weight}kg weight. ";
                }
                if (!string.IsNullOrEmpty(member.BodyType))
                {
                    currentBodyDescription += $"Body type: {member.BodyType}. ";
                }
                if (!string.IsNullOrEmpty(member.Gender))
                {
                    currentBodyDescription += $"Gender: {member.Gender}. ";
                }
                if (string.IsNullOrEmpty(currentBodyDescription) || currentBodyDescription == "Current physique: ")
                {
                    currentBodyDescription += "Average build, typical fitness level.";
                }

                // Ek notlar varsa ekle
                if (!string.IsNullOrWhiteSpace(additionalNotes))
                {
                    exerciseProgram += $". Additional notes: {additionalNotes}";
                }

                // AI ile görüntü oluştur - Önce Stable Diffusion dene (fotoğraf varsa), yoksa DALL-E kullan
                string imageUrl;
                
                if (!string.IsNullOrEmpty(photoBase64))
                {
                    try
                    {
                        // Stable Diffusion ile image-to-image (daha gerçekçi)
                        imageUrl = await _openAIService.GenerateBodyTransformationWithStableDiffusionAsync(
                            photoBase64,
                            exerciseProgram,
                            duration
                        );
                    }
                    catch (Exception ex)
                    {
                        // Stable Diffusion başarısız olursa DALL-E'ye düş
                        System.Diagnostics.Debug.WriteLine($"Stable Diffusion hatası, DALL-E'ye geçiliyor: {ex.Message}");
                        imageUrl = await _openAIService.GenerateBodyTransformationImageAsync(
                            exerciseProgram, 
                            currentBodyDescription, 
                            duration,
                            photoBase64
                        );
                    }
                }
                else
                {
                    // Fotoğraf yoksa DALL-E kullan
                    imageUrl = await _openAIService.GenerateBodyTransformationImageAsync(
                        exerciseProgram, 
                        currentBodyDescription, 
                        duration,
                        null
                    );
                }

                return Json(new { 
                    success = true, 
                    message = "Transformasyon görüntüsü başarıyla oluşturuldu.",
                    imageUrl = imageUrl,
                    duration = duration,
                    exerciseProgram = exerciseProgram
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Görüntü oluşturulurken hata oluştu: {ex.Message}" 
                });
            }
        }
    }
}


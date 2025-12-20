/*
Üye profil yönetimi controller'ı.
Üyelerin profil bilgilerini görüntüleme ve düzenleme işlemlerini yönetir.
Sadece kendi profil bilgilerini düzenleyebilirler (GymId hariç - bu Admin tarafından yönetilir).
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;
using System.IO;

namespace proje.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public MemberController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Üyenin profil sayfasını gösterir - kullanıcı ve spor salonu bilgileri ile birlikte
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View(member);
        }

        /// <summary>
        /// Üye profil düzenleme form sayfasını gösterir - aktif spor salonları listesi ile birlikte
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
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

            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            return View(member);
        }

        /// <summary>
        /// Üye profil bilgilerini günceller - ad, soyad, telefon, doğum tarihi, cinsiyet, boy, kilo, vücut tipi, sağlık durumu (GymId değiştirilemez, Admin yönetir)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Member member)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (existingMember == null || existingMember.Id != member.Id)
            {
                return NotFound();
            }

            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Gym");
            ModelState.Remove("GymId");
            ModelState.Remove("Appointments");
            ModelState.Remove("AIRecommendations");
            ModelState.Remove("NutritionPlans");
            ModelState.Remove("PhotoUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    existingMember.FirstName = member.FirstName;
                    existingMember.LastName = member.LastName;
                    existingMember.Phone = member.Phone;
                    existingMember.DateOfBirth = member.DateOfBirth;
                    existingMember.Gender = member.Gender;
                    existingMember.Height = member.Height;
                    existingMember.Weight = member.Weight;
                    existingMember.BodyType = member.BodyType;
                    existingMember.HealthConditions = member.HealthConditions;
                    existingMember.UpdatedDate = DateTime.Now;

                    _context.Update(existingMember);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Members.AnyAsync(e => e.Id == member.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Profil güncellenirken bir hata oluştu: {ex.Message}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        ModelState.AddModelError("", error);
                    }
                }
            }

            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            return View(member);
        }

        /// <summary>
        /// Üye profil fotoğrafı yükler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            if (photo == null || photo.Length == 0)
            {
                return Json(new { success = false, message = "Lütfen bir fotoğraf seçin." });
            }

            // Dosya uzantısı kontrolü
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Sadece JPG, PNG veya GIF formatında fotoğraf yükleyebilirsiniz." });
            }

            // Dosya boyutu kontrolü (5MB)
            if (photo.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "Fotoğraf boyutu 5MB'dan küçük olmalıdır." });
            }

            try
            {
                // Uploads klasörünü oluştur
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Benzersiz dosya adı oluştur
                var fileName = $"{member.Id}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Dosyayı kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                // Eski fotoğrafı sil
                if (!string.IsNullOrEmpty(member.PhotoUrl))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", member.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Veritabanına kaydet
                member.PhotoUrl = $"/uploads/photos/{fileName}";
                member.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Fotoğraf başarıyla yüklendi.", photoUrl = member.PhotoUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Fotoğraf yüklenirken hata oluştu: {ex.Message}" });
            }
        }
    }
}

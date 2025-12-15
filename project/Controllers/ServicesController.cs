/*
Hizmet türleri yönetimi controller'ı - Admin rolü gerekir.
Genel hizmet türlerini (fitness, yoga, pilates vb.) yönetir.
Bu hizmetler spor salonlarına atanarak spor salonu hizmetleri (GymService) oluşturulur.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm hizmet türlerini listeler (alfabetik sırayla)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        /// <summary>
        /// Yeni hizmet türü oluşturur ve veritabanına kaydeder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedDate = DateTime.Now;
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hizmet başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        /// <summary>
        /// Hizmet bilgilerini günceller (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id)
            {
                return Json(new { success = false, message = "Hizmet ID'si eşleşmiyor." });
            }

            var existingService = await _context.Services.FindAsync(id);
            if (existingService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            if (ModelState.IsValid)
            {
                existingService.Name = service.Name;
                existingService.Description = service.Description;
                existingService.IsActive = service.IsActive;

                _context.Update(existingService);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Hizmet başarıyla güncellendi." });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(", ", errors) });
        }

        /// <summary>
        /// Hizmet türünü siler - spor salonlarında kullanılıyorsa silme işlemi yapılmaz (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            var hasGymServices = await _context.GymServices.AnyAsync(gs => gs.ServiceId == id);
            if (hasGymServices)
            {
                return Json(new { success = false, message = "Bu hizmet spor salonlarında kullanılıyor. Önce spor salonlarından kaldırmanız gerekiyor." });
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hizmet başarıyla silindi." });
        }
    }
}


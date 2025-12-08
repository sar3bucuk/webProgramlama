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

        // GET: Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        // POST: Services/Create
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

        // POST: Services/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            // Hizmet kullanılıyor mu kontrol et
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


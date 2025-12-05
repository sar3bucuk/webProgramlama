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
            return View(await _context.Services.OrderByDescending(s => s.CreatedDate).ToListAsync());
        }

        // GET: Services/Details/5 - Artık modal kullanıldığı için Index'e yönlendir
        public IActionResult Details(int? id)
        {
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Create - Artık modal kullanıldığı için Index'e yönlendir
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedDate = DateTime.Now;
                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Hizmet eklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Edit/5 - Artık modal kullanıldığı için Index'e yönlendir
        public IActionResult Edit(int? id)
        {
            return RedirectToAction(nameof(Index));
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, string Name, string? Description, bool IsActive, DateTime CreatedDate)
        {
            var service = await _context.Services.FindAsync(Id);
            if (service == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    service.Name = Name;
                    service.Description = Description;
                    service.IsActive = IsActive;
                    service.CreatedDate = CreatedDate;
                    
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Hizmet başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Hizmet güncellenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Delete/5 - Artık modal kullanıldığı için Index'e yönlendir
        public IActionResult Delete(int? id)
        {
            return RedirectToAction(nameof(Index));
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                // Spor salonlarına bağlı hizmetler varsa silinemez kontrolü
                var hasGymServices = await _context.GymServices
                    .AnyAsync(gs => gs.ServiceId == id);

                if (hasGymServices)
                {
                    TempData["Error"] = "Bu hizmet spor salonlarına bağlı olduğu için silinemez.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}


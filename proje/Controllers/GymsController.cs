using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GymsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GymsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Gyms
        public async Task<IActionResult> Index()
        {
            return View(await _context.Gyms.ToListAsync());
        }

        // GET: Gyms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gym = await _context.Gyms
                .Include(g => g.GymServices)
                    .ThenInclude(gs => gs.Service)
                .Include(g => g.Trainers)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (gym == null)
            {
                return NotFound();
            }

            // Eklenebilecek hizmetleri getir (zaten eklenmiş olanlar hariç)
            var existingServiceIds = gym.GymServices.Select(gs => gs.ServiceId).ToList();
            ViewBag.AvailableServices = await _context.Services
                .Where(s => s.IsActive && !existingServiceIds.Contains(s.Id))
                .ToListAsync();

            return View(gym);
        }

        // GET: Gyms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Gyms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,Phone,Email,OpeningTime,ClosingTime,IsActive")] Gym gym)
        {
            if (ModelState.IsValid)
            {
                gym.CreatedDate = DateTime.Now;
                _context.Add(gym);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gym);
        }

        // GET: Gyms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gym = await _context.Gyms.FindAsync(id);
            if (gym == null)
            {
                return NotFound();
            }
            return View(gym);
        }

        // POST: Gyms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Phone,Email,OpeningTime,ClosingTime,IsActive,CreatedDate")] Gym gym)
        {
            if (id != gym.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    gym.UpdatedDate = DateTime.Now;
                    _context.Update(gym);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GymExists(gym.Id))
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
            return View(gym);
        }

        // GET: Gyms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gym = await _context.Gyms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gym == null)
            {
                return NotFound();
            }

            return View(gym);
        }

        // POST: Gyms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gym = await _context.Gyms.FindAsync(id);
            if (gym != null)
            {
                _context.Gyms.Remove(gym);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Gyms/AddService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(int GymId, int ServiceId, int Duration, decimal Price, bool IsActive)
        {
            if (GymId <= 0 || ServiceId <= 0)
            {
                TempData["Error"] = "Spor salonu ve hizmet seçmelisiniz.";
                return RedirectToAction(nameof(Details), new { id = GymId });
            }

            // Aynı kombinasyon kontrolü
            var existing = await _context.GymServices
                .FirstOrDefaultAsync(gs => gs.GymId == GymId && gs.ServiceId == ServiceId);

            if (existing != null)
            {
                TempData["Error"] = "Bu spor salonu için bu hizmet zaten eklenmiş.";
                return RedirectToAction(nameof(Details), new { id = GymId });
            }

            var gymService = new GymService
            {
                GymId = GymId,
                ServiceId = ServiceId,
                Duration = Duration,
                Price = Price,
                IsActive = IsActive,
                CreatedDate = DateTime.Now
            };

            _context.Add(gymService);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Hizmet başarıyla eklendi.";

            return RedirectToAction(nameof(Details), new { id = GymId });
        }

        // POST: Gyms/EditService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, int Duration, decimal Price, bool IsActive)
        {
            var gymService = await _context.GymServices.FindAsync(id);
            if (gymService == null)
            {
                return NotFound();
            }

            gymService.Duration = Duration;
            gymService.Price = Price;
            gymService.IsActive = IsActive;

            try
            {
                _context.Update(gymService);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hizmet başarıyla güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GymServiceExists(gymService.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Details), new { id = gymService.GymId });
        }

        // POST: Gyms/ToggleServiceStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleServiceStatus(int id)
        {
            var gymService = await _context.GymServices.FindAsync(id);
            if (gymService == null)
            {
                return NotFound();
            }

            gymService.IsActive = !gymService.IsActive;
            _context.Update(gymService);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Hizmet {(gymService.IsActive ? "aktif" : "pasif")} yapıldı.";
            return RedirectToAction(nameof(Details), new { id = gymService.GymId });
        }

        // GET: Gyms/DeleteService/5
        public async Task<IActionResult> DeleteService(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gymService = await _context.GymServices
                .Include(gs => gs.Gym)
                .Include(gs => gs.Service)
                .FirstOrDefaultAsync(gs => gs.Id == id);

            if (gymService == null)
            {
                return NotFound();
            }

            // Randevular varsa silinemez kontrolü
            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.GymServiceId == id && (a.Status == "Pending" || a.Status == "Approved"));

            if (hasAppointments)
            {
                TempData["Error"] = "Bu hizmete ait aktif randevular bulunduğu için silinemez.";
                return RedirectToAction(nameof(Details), new { id = gymService.GymId });
            }

            var gymId = gymService.GymId;
            _context.GymServices.Remove(gymService);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Hizmet başarıyla silindi.";

            return RedirectToAction(nameof(Details), new { id = gymId });
        }

        private bool GymExists(int id)
        {
            return _context.Gyms.Any(e => e.Id == id);
        }

        private bool GymServiceExists(int id)
        {
            return _context.GymServices.Any(e => e.Id == id);
        }
    }
}


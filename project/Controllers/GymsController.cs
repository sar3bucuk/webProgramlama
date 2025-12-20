/*
Spor salonu yönetimi controller'ı - Admin rolü gerekir.
Spor salonlarının CRUD işlemleri ve hizmet yönetimi bu controller'da yapılır.
AdminController'dan taşınmıştır.
*/

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

        /// <summary>
        /// Tüm spor salonlarını listeler (alfabetik sırayla)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var gyms = await _context.Gyms.OrderBy(g => g.Name).ToListAsync();
            return View("~/Views/Admin/Gyms.cshtml", gyms);
        }

        /// <summary>
        /// Spor salonu detaylarını gösterir - hizmetler, antrenörler ve bilgilerini listeler
        /// </summary>
        [HttpGet]
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
                    .ThenInclude(t => t.User)
                .Include(g => g.Trainers)
                    .ThenInclude(t => t.TrainerServices)
                        .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gym == null)
            {
                return NotFound();
            }

            if (gym.GymServices != null)
            {
                gym.GymServices = gym.GymServices.OrderBy(gs => gs.Service != null ? gs.Service.Name : "").ToList();
            }

            if (gym.Trainers != null)
            {
                gym.Trainers = gym.Trainers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToList();
            }

            return View("~/Views/Admin/GymDetails.cshtml", gym);
        }

        /// <summary>
        /// Yeni spor salonu oluşturma form sayfasını gösterir
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Admin/CreateGym.cshtml");
        }

        /// <summary>
        /// Yeni spor salonu oluşturur ve veritabanına kaydeder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Gym gym)
        {
            var selectedDays = Request.Form["SelectedDays"]
                .Select(d => int.TryParse(d, out int day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            
            gym.WorkingDaysList = selectedDays;

            if (ModelState.IsValid)
            {
                gym.CreatedDate = DateTime.Now;
                _context.Gyms.Add(gym);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Spor salonu başarıyla eklendi.";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/CreateGym.cshtml", gym);
        }

        /// <summary>
        /// Spor salonu düzenleme form sayfasını gösterir - mevcut hizmetler ile birlikte
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gym = await _context.Gyms
                .Include(g => g.GymServices)
                    .ThenInclude(gs => gs.Service)
                .FirstOrDefaultAsync(g => g.Id == id);
            
            if (gym != null && gym.GymServices != null)
            {
                gym.GymServices = gym.GymServices.OrderBy(gs => gs.Service != null ? gs.Service.Name : "").ToList();
            }
            
            if (gym == null)
            {
                return NotFound();
            }

            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View("~/Views/Admin/EditGym.cshtml", gym);
        }

        /// <summary>
        /// Spor salonu bilgilerini günceller ve veritabanına kaydeder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Gym gym)
        {
            if (id != gym.Id)
            {
                return NotFound();
            }

            var selectedDays = Request.Form["SelectedDays"]
                .Select(d => int.TryParse(d, out int day) ? day : -1)
                .Where(d => d >= 0 && d <= 6)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            
            gym.WorkingDaysList = selectedDays;

            if (ModelState.IsValid)
            {
                try
                {
                    gym.UpdatedDate = DateTime.Now;
                    _context.Update(gym);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Spor salonu başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Gyms.AnyAsync(e => e.Id == id))
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
            
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View("~/Views/Admin/EditGym.cshtml", gym);
        }

        /// <summary>
        /// Spor salonunu veritabanından siler - ilişkili randevular, gym servisleri ve antrenörlerin bağlantısını da temizler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var gym = await _context.Gyms
                .Include(g => g.GymServices)
                    .ThenInclude(gs => gs.Appointments)
                .Include(g => g.Trainers)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gym == null)
            {
                TempData["ErrorMessage"] = "Spor salonu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var gymServiceIds = gym.GymServices.Select(gs => gs.Id).ToList();
                var appointmentsToDelete = await _context.Appointments
                    .Where(a => gymServiceIds.Contains(a.GymServiceId))
                    .ToListAsync();
                
                if (appointmentsToDelete.Any())
                {
                    _context.Appointments.RemoveRange(appointmentsToDelete);
                }

                if (gym.GymServices.Any())
                {
                    _context.GymServices.RemoveRange(gym.GymServices);
                }

                var trainers = await _context.Trainers
                    .Where(t => t.GymId == id)
                    .ToListAsync();
                
                foreach (var trainer in trainers)
                {
                    trainer.GymId = null;
                }

                var members = await _context.Members
                    .Where(m => m.GymId == id)
                    .ToListAsync();
                
                foreach (var member in members)
                {
                    member.GymId = null;
                }

                _context.Gyms.Remove(gym);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Spor salonu ve ilişkili tüm veriler başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Spor salonu silinirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Spor salonuna hizmet ekler - süre, fiyat ve aktiflik durumu ile birlikte (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGymService(int gymId, int serviceId, int duration, string price, bool isActive = true)
        {
            var existingService = await _context.GymServices
                .FirstOrDefaultAsync(gs => gs.GymId == gymId && gs.ServiceId == serviceId);
            
            if (existingService != null)
            {
                return Json(new { success = false, message = "Bu hizmet zaten eklenmiş." });
            }

            if (!decimal.TryParse(price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal priceDecimal))
            {
                return Json(new { success = false, message = "Geçersiz ücret değeri." });
            }

            var gymService = new GymService
            {
                GymId = gymId,
                ServiceId = serviceId,
                Duration = duration,
                Price = priceDecimal,
                IsActive = isActive,
                CreatedDate = DateTime.Now
            };

            _context.GymServices.Add(gymService);
            await _context.SaveChangesAsync();

            var service = await _context.Services.FindAsync(serviceId);
            return Json(new { 
                success = true, 
                message = "Hizmet başarıyla eklendi.",
                gymService = new {
                    id = gymService.Id,
                    serviceName = service?.Name,
                    duration = gymService.Duration,
                    price = gymService.Price,
                    isActive = gymService.IsActive
                }
            });
        }

        /// <summary>
        /// Spor salonu hizmetinin süre, fiyat ve aktiflik durumunu günceller (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGymService(int id, int duration, string price, bool isActive)
        {
            var gymService = await _context.GymServices.FindAsync(id);
            if (gymService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            if (!decimal.TryParse(price, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal priceDecimal))
            {
                return Json(new { success = false, message = "Geçersiz ücret değeri." });
            }

            gymService.Duration = duration;
            gymService.Price = priceDecimal;
            gymService.IsActive = isActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hizmet başarıyla güncellendi." });
        }

        /// <summary>
        /// Spor salonu hizmetini veritabanından siler (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGymService(int id)
        {
            var gymService = await _context.GymServices.FindAsync(id);
            if (gymService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            _context.GymServices.Remove(gymService);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hizmet başarıyla silindi." });
        }
    }
}

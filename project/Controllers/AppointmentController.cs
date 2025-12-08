using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AppointmentController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Create()
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
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ModelState'den bu alanları kaldır (biz manuel kontrol edeceğiz)
            ModelState.Remove("MemberId");
            ModelState.Remove("TrainerId");
            ModelState.Remove("GymServiceId");
            ModelState.Remove("Duration");
            ModelState.Remove("Price");

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                ModelState.AddModelError("", "Üye kaydı bulunamadı.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // MemberId'yi set et
            appointment.MemberId = member.Id;

            // TrainerId ve GymServiceId kontrolü
            if (appointment.TrainerId == 0)
            {
                ModelState.AddModelError("TrainerId", "Lütfen bir antrenör seçin.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }
            if (appointment.GymServiceId == 0)
            {
                ModelState.AddModelError("GymServiceId", "Lütfen bir hizmet seçin.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // GymService kontrolü ve Duration, Price alma
            var gymService = await _context.GymServices
                .Include(gs => gs.Service)
                .FirstOrDefaultAsync(gs => gs.Id == appointment.GymServiceId);

            if (gymService == null || !gymService.IsActive)
            {
                ModelState.AddModelError("", "Seçilen hizmet bulunamadı veya aktif değil.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // Duration ve Price'ı GymService'den al
            appointment.Duration = gymService.Duration;
            appointment.Price = gymService.Price;
            ModelState.Remove("MemberId"); // Bu da controller'da set ediliyor

            // Müsaitlik kontrolü
            var trainer = await _context.Trainers
                .Include(t => t.TrainerAvailabilities)
                .FirstOrDefaultAsync(t => t.Id == appointment.TrainerId);

            if (trainer == null || !trainer.IsActive)
            {
                ModelState.AddModelError("", "Seçilen antrenör bulunamadı veya aktif değil.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // Haftanın gününü hesapla (0=Pazar, 6=Cumartesi)
            // C# DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
            // Bizim sistem: 0=Pazar, 1=Pazartesi, ..., 6=Cumartesi
            int dayOfWeek = (int)appointment.AppointmentDate.DayOfWeek;

            // Müsaitlik kontrolü
            var availability = trainer.TrainerAvailabilities
                .FirstOrDefault(a => a.DayOfWeek == dayOfWeek && a.IsAvailable);

            if (availability == null)
            {
                ModelState.AddModelError("", "Seçilen tarih ve saatte antrenör müsait değil.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // Saat aralığı kontrolü
            var endTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(appointment.Duration));
            if (appointment.AppointmentTime < availability.StartTime || endTime > availability.EndTime)
            {
                ModelState.AddModelError("", $"Randevu saati antrenörün müsaitlik saatleri içinde olmalıdır ({availability.StartTime:hh\\:mm} - {availability.EndTime:hh\\:mm}).");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // Çakışma kontrolü
            // Önce aynı tarih ve antrenör için randevuları çek
            var existingAppointments = await _context.Appointments
                .Where(a => 
                    a.TrainerId == appointment.TrainerId &&
                    a.AppointmentDate == appointment.AppointmentDate &&
                    a.Status != "Cancelled" &&
                    a.Status != "Rejected")
                .ToListAsync();

            // Memory'de çakışma kontrolü yap
            var conflictingAppointment = existingAppointments
                .FirstOrDefault(a =>
                {
                    var aEndTime = a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration));
                    return (appointment.AppointmentTime >= a.AppointmentTime && appointment.AppointmentTime < aEndTime) ||
                           (endTime > a.AppointmentTime && endTime <= aEndTime) ||
                           (appointment.AppointmentTime <= a.AppointmentTime && endTime >= aEndTime);
                });

            if (conflictingAppointment != null)
            {
                ModelState.AddModelError("", "Seçilen saatte başka bir randevu var.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View(appointment);
            }

            // GymService zaten yukarıda kontrol edildi ve Duration, Price set edildi
            appointment.Status = "Pending";
            appointment.CreatedDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu. Onay bekleniyor.";
                return RedirectToAction("MyAppointments");
            }

            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(appointment);
        }

        public async Task<IActionResult> MyAppointments()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> GetAvailableTrainers(int gymId, int gymServiceId, DateTime appointmentDate, string appointmentTime, int duration)
        {
            if (!TimeSpan.TryParse(appointmentTime, out var timeSpan))
            {
                return Json(new { success = false, message = "Geçersiz saat formatı." });
            }

            // GymService'den ServiceId'yi al
            var gymService = await _context.GymServices
                .FirstOrDefaultAsync(gs => gs.Id == gymServiceId);
            
            if (gymService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            // Haftanın gününü hesapla (0=Pazar, 6=Cumartesi)
            // C# DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
            // Bizim sistem: 0=Pazar, 1=Pazartesi, ..., 6=Cumartesi
            int dayOfWeek = (int)appointmentDate.DayOfWeek;

            var endTime = timeSpan.Add(TimeSpan.FromMinutes(duration));

            var availableTrainers = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerAvailabilities)
                .Include(t => t.TrainerServices)
                .Where(t => 
                    t.IsActive &&
                    t.GymId == gymId &&
                    t.TrainerServices.Any(ts => ts.ServiceId == gymService.ServiceId) &&
                    t.TrainerAvailabilities.Any(ta => 
                        ta.DayOfWeek == dayOfWeek &&
                        ta.IsAvailable &&
                        ta.StartTime <= timeSpan &&
                        ta.EndTime >= endTime))
                .Select(t => new
                {
                    id = t.Id,
                    name = t.FirstName + " " + t.LastName,
                    experience = t.ExperienceYears,
                    bio = t.Bio ?? ""
                })
                .ToListAsync();

            // Çakışma kontrolü
            var conflictingAppointments = await _context.Appointments
                .Where(a => 
                    a.AppointmentDate == appointmentDate &&
                    a.Status != "Cancelled" &&
                    a.Status != "Rejected")
                .ToListAsync();

            var finalTrainers = availableTrainers.Where(t =>
            {
                var trainerConflicts = conflictingAppointments
                    .Where(a => a.TrainerId == t.id &&
                        (
                            (timeSpan >= a.AppointmentTime && timeSpan < a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration))) ||
                            (endTime > a.AppointmentTime && endTime <= a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration))) ||
                            (timeSpan <= a.AppointmentTime && endTime >= a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration)))
                        ))
                    .Any();
                return !trainerConflicts;
            }).ToList();

            return Json(new { success = true, trainers = finalTrainers });
        }

        [HttpPost]
        public async Task<IActionResult> GetGymServices(int gymId)
        {
            var services = await _context.GymServices
                .Include(gs => gs.Service)
                .Where(gs => gs.GymId == gymId && gs.IsActive)
                .Select(gs => new
                {
                    id = gs.Id,
                    name = gs.Service.Name,
                    duration = gs.Duration,
                    price = gs.Price
                })
                .ToListAsync();

            return Json(new { success = true, services = services });
        }
    }
}


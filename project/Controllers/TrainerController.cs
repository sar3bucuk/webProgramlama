/*
Antrenör paneli controller'ı - Trainer rolü gerekir.
Antrenörlerin profil yönetimi, randevu görüntüleme ve durum güncelleme, hizmet yönetimi işlemlerini yönetir.
Sadece kendi bilgilerini ve kendi randevularını yönetebilirler.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TrainerController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Kullanıcıya bildirim oluşturur - randevu durum değişikliklerinde kullanılır
        /// </summary>
        private Task CreateNotificationAsync(string userId, string title, string message, int? appointmentId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                AppointmentId = appointmentId,
                IsRead = false,
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(notification);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Antrenör profil sayfasını gösterir - kullanıcı, spor salonu ve hizmet bilgileri ile birlikte
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        /// <summary>
        /// Antrenör profil düzenleme form sayfasını gösterir - aktif hizmetler listesi ile birlikte
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(trainer);
        }

        /// <summary>
        /// Antrenör profil bilgilerini günceller - ad, soyad, telefon, email, bio, deneyim yılı, aktiflik durumu (GymId değiştirilemez, Admin yönetir)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Trainer trainer)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingTrainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (existingTrainer == null || existingTrainer.Id != trainer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingTrainer.FirstName = trainer.FirstName;
                    existingTrainer.LastName = trainer.LastName;
                    existingTrainer.Phone = trainer.Phone;
                    existingTrainer.Email = trainer.Email;
                    existingTrainer.Bio = trainer.Bio;
                    existingTrainer.ExperienceYears = trainer.ExperienceYears;
                    existingTrainer.IsActive = trainer.IsActive;

                    _context.Update(existingTrainer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Bilgileriniz başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Trainers.AnyAsync(e => e.Id == trainer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(trainer);
        }

        /// <summary>
        /// Antrenörün randevularını listeler - güncel, geçmiş ve iptal/reddedilmiş randevuları üç kategoriye ayırır
        /// </summary>
        public async Task<IActionResult> Appointments()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            var today = DateTime.Today;
            
            // Güncel randevular: Bugün ve gelecekteki, tamamlanmamış, iptal edilmemiş, reddedilmemiş randevular
            var currentAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Gym)
                .Where(a => a.TrainerId == trainer.Id && 
                           a.AppointmentDate >= today && 
                           a.Status != "Completed" && 
                           a.Status != "Cancelled" &&
                           a.Status != "Rejected")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // Geçmiş randevular: Tamamlanmış randevular (bugünden önce veya Completed durumunda)
            var pastAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Gym)
                .Where(a => a.TrainerId == trainer.Id && 
                           a.Status == "Completed" &&
                           (a.AppointmentDate < today || a.Status == "Completed"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // İptal ve reddedildi randevular
            var cancelledRejectedAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Gym)
                .Where(a => a.TrainerId == trainer.Id && 
                           (a.Status == "Cancelled" || a.Status == "Rejected"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.CurrentAppointments = currentAppointments;
            ViewBag.PastAppointments = pastAppointments;
            ViewBag.CancelledRejectedAppointments = cancelledRejectedAppointments;

            return View(currentAppointments);
        }

        /// <summary>
        /// Antrenörün geçmiş randevularını listeler - tamamlanmış randevular (artık Appointments metodunda üç kategoriye ayrıldı)
        /// </summary>
        public async Task<IActionResult> PastAppointments()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            var today = DateTime.Today;
            var appointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Gym)
                .Where(a => a.TrainerId == trainer.Id && 
                           a.Status == "Completed" &&
                           (a.AppointmentDate < today || a.Status == "Completed"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        /// <summary>
        /// Randevu durumunu günceller - Pending, Approved, Rejected, Completed, Cancelled (JSON döner, üye'ye bildirim gönderir)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return Json(new { success = false, message = "Antrenör bulunamadı." });
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == trainer.Id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Randevu bulunamadı." });
            }

            var validStatuses = new[] { "Pending", "Approved", "Rejected", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Geçersiz durum." });
            }

            var oldStatus = appointment.Status;
            appointment.Status = status;
            appointment.UpdatedDate = DateTime.Now;

            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                if (oldStatus != status && (status == "Approved" || status == "Rejected"))
                {
                    var appointmentWithDetails = await _context.Appointments
                        .Include(a => a.Member)
                            .ThenInclude(m => m.User)
                        .Include(a => a.Trainer)
                            .ThenInclude(t => t.User)
                        .Include(a => a.GymService)
                            .ThenInclude(gs => gs.Service)
                        .FirstOrDefaultAsync(a => a.Id == id);

                    // Sadece üyeye bildirim gönder (trainer zaten kendi randevularını görüyor)
                    if (appointmentWithDetails?.Member?.User != null && !string.IsNullOrEmpty(appointmentWithDetails.Member.User.Id))
                    {
                        var statusText = status == "Approved" ? "onaylandı" : "reddedildi";
                        await CreateNotificationAsync(
                            appointmentWithDetails.Member.User.Id,
                            $"Randevu {statusText}",
                            $"{appointmentWithDetails.AppointmentDate:dd.MM.yyyy} tarihinde {appointmentWithDetails.AppointmentTime:hh\\:mm} saatindeki randevunuz {statusText}.",
                            appointmentWithDetails.Id
                        );
                        await _context.SaveChangesAsync();
                    }
                }

                var statusMessages = new Dictionary<string, string>
                {
                    { "Pending", "beklemede" },
                    { "Approved", "onaylandı" },
                    { "Rejected", "reddedildi" },
                    { "Completed", "tamamlandı" },
                    { "Cancelled", "iptal edildi" }
                };
                var statusMessage = statusMessages.ContainsKey(status) ? statusMessages[status] : "güncellendi";
                return Json(new { success = true, message = $"Randevu durumu {statusMessage}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Randevu düzenleme sayfasını gösterir - üye, hizmet ve spor salonu bilgileri ile birlikte
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditAppointment(int? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Gym)
                .FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == trainer.Id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        /// <summary>
        /// Randevu durumunu günceller - sayfa üzerinden durum değişikliği yapar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(int id, string status)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return NotFound();
            }

            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.TrainerId == trainer.Id);

            if (existingAppointment == null)
            {
                return NotFound();
            }

            var validStatuses = new[] { "Pending", "Approved", "Rejected", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Geçersiz durum.";
                return RedirectToAction(nameof(Appointments));
            }

            try
            {
                existingAppointment.Status = status;
                existingAppointment.UpdatedDate = DateTime.Now;

                _context.Update(existingAppointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Randevu durumu başarıyla güncellendi.";
                return RedirectToAction(nameof(Appointments));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Randevu güncellenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Appointments));
            }
        }

        /// <summary>
        /// Antrenöre hizmet ekler - antrenörün sunduğu hizmet türlerine ekler (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerService(int serviceId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return Json(new { success = false, message = "Antrenör bulunamadı." });
            }

            var existingService = await _context.TrainerServices
                .FirstOrDefaultAsync(ts => ts.TrainerId == trainer.Id && ts.ServiceId == serviceId);
            
            if (existingService != null)
            {
                return Json(new { success = false, message = "Bu hizmet zaten eklenmiş." });
            }

            var trainerService = new TrainerService
            {
                TrainerId = trainer.Id,
                ServiceId = serviceId,
                CreatedDate = DateTime.Now
            };

            _context.TrainerServices.Add(trainerService);
            await _context.SaveChangesAsync();

            var service = await _context.Services.FindAsync(serviceId);
            return Json(new { 
                success = true, 
                message = "Hizmet başarıyla eklendi.",
                trainerService = new {
                    id = trainerService.Id,
                    serviceName = service?.Name
                }
            });
        }

        /// <summary>
        /// Antrenör hizmetini siler - antrenörün sunduğu hizmet türlerinden kaldırır (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainerService(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);

            if (trainer == null)
            {
                return Json(new { success = false, message = "Antrenör bulunamadı." });
            }

            var trainerService = await _context.TrainerServices
                .FirstOrDefaultAsync(ts => ts.Id == id && ts.TrainerId == trainer.Id);

            if (trainerService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı veya bu hizmet size ait değil." });
            }

            _context.TrainerServices.Remove(trainerService);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hizmet başarıyla silindi." });
        }
    }
}


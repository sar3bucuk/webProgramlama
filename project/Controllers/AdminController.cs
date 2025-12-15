/*
Admin paneli controller'ı; sistem yönetimi admin rolüyle burada yapılır.
Sistem yönetimi, spor salonları, hizmetler, üyeler, antrenörler, randevular, bildirimler vb. işlemleri içerir.
*/
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        /// <summary>
        /// Kullanıcıya bildirim oluşturur - randevu durumu değişiklikleri için kullanılır
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
        /// Admin ana sayfası - spor salonları listesi ve istatistikleri (üye sayısı, antrenör sayısı) gösterir
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var gyms = await _context.Gyms
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    GymId = g.Id,
                    GymName = g.Name,
                    IsActive = g.IsActive,
                    MemberCount = _context.Members.Count(m => m.GymId == g.Id),
                    TrainerCount = _context.Trainers.Count(t => t.GymId == g.Id && t.IsActive)
                })
                .ToListAsync();

            ViewBag.GymStats = gyms;
            return View();
        }

        /// <summary>
        /// Üye listesini gösterir - isim arama ve spor salonu filtresi ile
        /// </summary>
        public async Task<IActionResult> Members(string searchName, int? gymId)
        {
            var query = _context.Members
                .Include(m => m.User)
                .Include(m => m.Gym)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                searchName = searchName.Trim();
                query = query.Where(m => 
                    (m.FirstName + " " + m.LastName).Contains(searchName) ||
                    m.FirstName.Contains(searchName) ||
                    m.LastName.Contains(searchName));
            }

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }
            else if (gymId == 0)
            {
                query = query.Where(m => m.GymId == null);
            }

            var members = await query
                .OrderBy(m => m.FirstName)
                .ThenBy(m => m.LastName)
                .ToListAsync();
            
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.SearchName = searchName;
            ViewBag.SelectedGymId = gymId;
            return View(members);
        }

        /// <summary>
        /// Üyeyi bir spor salonuna bağlar veya bağlantısını kaldırır (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberGym(int memberId, int? gymId)
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            if (gymId == 0)
            {
                gymId = null;
            }
            else if (gymId.HasValue)
            {
                var gymExists = await _context.Gyms.AnyAsync(g => g.Id == gymId.Value);
                if (!gymExists)
                {
                    return Json(new { success = false, message = "Spor salonu bulunamadı." });
                }
            }

            member.GymId = gymId;
            member.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            var gymName = gymId.HasValue 
                ? await _context.Gyms.Where(g => g.Id == gymId.Value).Select(g => g.Name).FirstOrDefaultAsync()
                : null;

            return Json(new { 
                success = true, 
                message = "Üye spor salonuna başarıyla bağlandı.",
                gymName = gymName ?? "Bağlı değil"
            });
        }

        /// <summary>
        /// Tüm antrenörleri listeler - spor salonu, hizmetler ve kullanıcı bilgileri ile birlikte
        /// </summary>
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .ToListAsync();
            
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(trainers);
        }

        /// <summary>
        /// Yeni antrenör oluşturma form sayfasını gösterir - aktif spor salonlarını yükler
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateTrainer()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        /// <summary>
        /// Yeni antrenör oluşturur - IdentityUser oluşturur, Trainer rolü verir ve Trainers tablosuna ekler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("email", "E-posta adresi gereklidir.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                return View(trainer);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("email", "Geçerli bir e-posta adresi giriniz.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                return View(trainer);
            }

            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                if (string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Yeni kullanıcı için şifre gereklidir.");
                    ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                    return View(trainer);
                }

                user = new IdentityUser 
                { 
                    UserName = email, 
                    Email = email,
                    EmailConfirmed = true
                };
                
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                    return View(trainer);
                }

                await _userManager.AddToRoleAsync(user, "Trainer");
            }
            else
            {
                if (await _userManager.IsInRoleAsync(user, "Member"))
                {
                    var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == user.Id);
                    if (existingMember != null)
                    {
                        ModelState.AddModelError("email", "Bu e-posta adresi zaten bir üye olarak kayıtlı. Aynı e-posta hem üye hem antrenör olamaz.");
                        ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                        return View(trainer);
                    }
                }

                var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (existingTrainer != null)
                {
                    ModelState.AddModelError("email", "Bu e-posta adresi ile zaten bir antrenör kayıtlı.");
                    ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                    return View(trainer);
                }

                if (!await _userManager.IsInRoleAsync(user, "Trainer"))
                {
                    await _userManager.AddToRoleAsync(user, "Trainer");
                }
            }

            // GymId null ise 0 olarak gelir, bunu null yap
            if (trainer.GymId == 0)
            {
                trainer.GymId = null;
            }

            // Trainer.Email'i IdentityUser'ın email'inden set et (hem giriş hem iletişim için)
            trainer.Email = email;
            // Email validation hatasını temizle (çünkü email parametresinden alıyoruz)
            ModelState.Remove("Email");
            // UserId validation hatasını temizle (çünkü UserId'yi biz controller'da set ediyoruz)
            ModelState.Remove("UserId");

            // UserId'yi set et (validation'dan önce)
            trainer.UserId = user.Id;
            trainer.CreatedDate = DateTime.Now;

            // ModelState kontrolü (GymId artık nullable olduğu için sorun olmamalı)
            if (ModelState.IsValid)
            {
                
                try
                {
                    _context.Trainers.Add(trainer);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Antrenör başarıyla eklendi.";
                    return RedirectToAction(nameof(Trainers));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Antrenör eklenirken bir hata oluştu: {ex.Message}");
                }
            }
            else
            {
                // ModelState hatalarını göster
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
            return View(trainer);
        }

        /// <summary>
        /// Antrenör düzenleme form sayfasını gösterir - hizmetler, müsaitlikler ve bilgileri ile birlikte
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditTrainer(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .Include(t => t.TrainerAvailabilities)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (trainer == null)
            {
                return NotFound();
            }

            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return View(trainer);
        }

        /// <summary>
        /// Antrenör bilgilerini günceller - ad, soyad, telefon, biyografi, deneyim, aktiflik ve spor salonu
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(int id, Trainer trainer)
        {
            if (id != trainer.Id)
            {
                return NotFound();
            }

            var existingTrainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (existingTrainer == null)
            {
                return NotFound();
            }

            ModelState.Remove("Email");
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    existingTrainer.FirstName = trainer.FirstName;
                    existingTrainer.LastName = trainer.LastName;
                    existingTrainer.Phone = trainer.Phone;
                    existingTrainer.Bio = trainer.Bio;
                    existingTrainer.ExperienceYears = trainer.ExperienceYears;
                    existingTrainer.IsActive = trainer.IsActive;
                    existingTrainer.GymId = trainer.GymId;
                    
                    if (existingTrainer.User != null)
                    {
                        existingTrainer.Email = existingTrainer.User.Email;
                    }

                    _context.Update(existingTrainer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Antrenör başarıyla güncellendi.";
                    return RedirectToAction(nameof(Trainers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Trainers.AnyAsync(e => e.Id == id))
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
                    ModelState.AddModelError("", $"Antrenör güncellenirken bir hata oluştu: {ex.Message}");
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
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            
            existingTrainer.FirstName = trainer.FirstName;
            existingTrainer.LastName = trainer.LastName;
            existingTrainer.Phone = trainer.Phone;
            existingTrainer.Bio = trainer.Bio;
            existingTrainer.ExperienceYears = trainer.ExperienceYears;
            existingTrainer.IsActive = trainer.IsActive;
            existingTrainer.GymId = trainer.GymId;
            
            return View(existingTrainer);
        }

        /// <summary>
        /// Antrenöre hizmet ekler - antrenörün verebileceği hizmet türlerini belirler (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerService(int trainerId, int serviceId)
        {
            var existingService = await _context.TrainerServices
                .FirstOrDefaultAsync(ts => ts.TrainerId == trainerId && ts.ServiceId == serviceId);
            
            if (existingService != null)
            {
                return Json(new { success = false, message = "Bu hizmet zaten eklenmiş." });
            }

            var trainerService = new TrainerService
            {
                TrainerId = trainerId,
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
        /// Antrenörden hizmeti kaldırır (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainerService(int id)
        {
            var trainerService = await _context.TrainerServices.FindAsync(id);
            if (trainerService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            _context.TrainerServices.Remove(trainerService);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hizmet başarıyla silindi." });
        }

        /// <summary>
        /// Antrenöre müsaitlik saati ekler - haftanın günü, başlangıç-bitiş saatleri ile (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerAvailability(int trainerId, int dayOfWeek, string startTime, string endTime, bool isAvailable = true)
        {
            var existingAvailability = await _context.TrainerAvailabilities
                .FirstOrDefaultAsync(ta => ta.TrainerId == trainerId && ta.DayOfWeek == dayOfWeek);
            
            if (existingAvailability != null)
            {
                return Json(new { success = false, message = "Bu gün için zaten bir müsaitlik saati tanımlı." });
            }

            if (!TimeSpan.TryParse(startTime, out var startTimeSpan) || !TimeSpan.TryParse(endTime, out var endTimeSpan))
            {
                return Json(new { success = false, message = "Geçersiz saat formatı." });
            }

            if (startTimeSpan >= endTimeSpan)
            {
                return Json(new { success = false, message = "Başlangıç saati bitiş saatinden önce olmalıdır." });
            }

            var availability = new TrainerAvailability
            {
                TrainerId = trainerId,
                DayOfWeek = dayOfWeek,
                StartTime = startTimeSpan,
                EndTime = endTimeSpan,
                IsAvailable = isAvailable,
                CreatedDate = DateTime.Now
            };

            _context.TrainerAvailabilities.Add(availability);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Müsaitlik saati başarıyla eklendi.",
                availability = new {
                    id = availability.Id,
                    dayName = availability.DayName,
                    startTime = availability.StartTime.ToString(@"hh\:mm"),
                    endTime = availability.EndTime.ToString(@"hh\:mm")
                }
            });
        }

        /// <summary>
        /// Tüm randevuları listeler - güncel, geçmiş ve iptal/reddedilmiş randevuları üç kategoriye ayırır
        /// </summary>
        public async Task<IActionResult> Appointments()
        {
            var today = DateTime.Today;
            
            // Güncel randevular: Bugün ve gelecekteki, tamamlanmamış, iptal edilmemiş, reddedilmemiş randevular
            var currentAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.AppointmentDate >= today && 
                           a.Status != "Completed" && 
                           a.Status != "Cancelled" &&
                           a.Status != "Rejected")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // Geçmiş randevular: Tamamlanmış randevular
            var pastAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.Status == "Completed" &&
                           (a.AppointmentDate < today || a.Status == "Completed"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            // İptal ve reddedildi randevular
            var cancelledRejectedAppointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.Status == "Cancelled" || a.Status == "Rejected")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.CurrentAppointments = currentAppointments;
            ViewBag.PastAppointments = pastAppointments;
            ViewBag.CancelledRejectedAppointments = cancelledRejectedAppointments;

            return View(currentAppointments);
        }

        /// <summary>
        /// Geçmiş randevuları listeler - bugünden önceki veya tamamlanmış/iptal edilmiş randevular
        /// </summary>
        public async Task<IActionResult> PastAppointments()
        {
            var today = DateTime.Today;
            var appointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.AppointmentDate < today || a.Status == "Completed" || a.Status == "Cancelled")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        /// <summary>
        /// Randevu durumunu günceller - Pending/Approved/Rejected/Completed/Cancelled. Durum değişikliğinde üyeye bildirim gönderir (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
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
            await _context.SaveChangesAsync();

            if (oldStatus != status && (status == "Approved" || status == "Rejected"))
            {
                var appointmentWithDetails = await _context.Appointments
                    .Include(a => a.Member)
                        .ThenInclude(m => m.User)
                    .Include(a => a.Trainer)
                    .Include(a => a.GymService)
                        .ThenInclude(gs => gs.Service)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointmentWithDetails?.Member?.User != null)
                {
                    var statusText = status == "Approved" ? "onaylandı" : "reddedildi";
                    await CreateNotificationAsync(
                        appointmentWithDetails.Member.User.Id,
                        $"Randevu {statusText}",
                        $"{appointmentWithDetails.AppointmentDate:dd.MM.yyyy} tarihinde {appointmentWithDetails.AppointmentTime:hh\\:mm} saatindeki randevunuz admin tarafından {statusText}.",
                        appointmentWithDetails.Id
                    );
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "Randevu durumu başarıyla güncellendi." });
        }

        /// <summary>
        /// Antrenör müsaitlik saatini siler (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainerAvailability(int id)
        {
            var availability = await _context.TrainerAvailabilities.FindAsync(id);
            if (availability == null)
            {
                return Json(new { success = false, message = "Müsaitlik saati bulunamadı." });
            }

            _context.TrainerAvailabilities.Remove(availability);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Müsaitlik saati başarıyla silindi." });
        }

        /// <summary>
        /// Antrenörü siler - bağlı randevuları, IdentityUser'ı ve tüm ilişkili verileri temizler (JSON döner)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Appointments)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainer == null)
            {
                return Json(new { success = false, message = "Antrenör bulunamadı." });
            }

            try
            {
                if (trainer.Appointments != null && trainer.Appointments.Any())
                {
                    _context.Appointments.RemoveRange(trainer.Appointments);
                }

                if (trainer.User != null)
                {
                    await _userManager.DeleteAsync(trainer.User);
                }
                
                _context.Trainers.Remove(trainer);
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Antrenör silinirken bir hata oluştu: {ex.Message}" });
            }

            return Json(new { success = true, message = "Antrenör başarıyla silindi." });
        }
    }
}


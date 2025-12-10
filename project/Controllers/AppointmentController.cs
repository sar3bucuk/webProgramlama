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

        private async Task PopulateViewBagForError(Appointment appointment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                var member = await _context.Members
                    .Include(m => m.Gym)
                    .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                
                if (member?.Gym != null)
                {
                    ViewBag.MemberGym = member.Gym;
                    ViewBag.MemberGymId = member.GymId;
                }
            }
            
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            
            // Seçili değerleri ViewBag'e ekle (dropdown'ları yeniden doldurmak için)
            if (appointment.GymServiceId > 0)
            {
                var selectedGymService = await _context.GymServices
                    .Include(gs => gs.Gym)
                    .FirstOrDefaultAsync(gs => gs.Id == appointment.GymServiceId);
                if (selectedGymService != null)
                {
                    ViewBag.SelectedGymId = selectedGymService.GymId;
                    ViewBag.SelectedGymServiceId = appointment.GymServiceId;
                }
            }
            ViewBag.SelectedTrainerId = appointment.TrainerId > 0 ? appointment.TrainerId : 0;
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

            // Üyenin kayıtlı olduğu spor salonunu kontrol et
            if (member.GymId == null || member.Gym == null)
            {
                // Spor salonu yoksa sayfayı aç ama mesaj göster
                ViewBag.MemberGym = null;
                ViewBag.MemberGymId = null;
                ViewBag.HasGym = false;
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View();
            }

            // Sadece üyenin kayıtlı olduğu spor salonunu göster
            ViewBag.MemberGym = member.Gym;
            ViewBag.MemberGymId = member.GymId;
            ViewBag.HasGym = true;
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

            // ModelState'i önce temizle (Required attribute hatalarını kaldırmak için)
            ModelState.Clear();

            // Form'dan tüm değerleri manuel olarak al (model binding güvenilir değil)
            var trainerIdValue = Request.Form["TrainerId"].FirstOrDefault() ?? "";
            var gymServiceIdValue = Request.Form["GymServiceId"].FirstOrDefault() ?? "";
            var appointmentDateValue = Request.Form["AppointmentDate"].FirstOrDefault() ?? "";
            var appointmentTimeValue = Request.Form["AppointmentTime"].FirstOrDefault() ?? "";
            var notesValue = Request.Form["Notes"].FirstOrDefault() ?? "";

            // Debug: Gelen form değerlerini logla
            System.Diagnostics.Debug.WriteLine($"Form değerleri - TrainerId: '{trainerIdValue}', GymServiceId: '{gymServiceIdValue}', Date: '{appointmentDateValue}', Time: '{appointmentTimeValue}'");
            
            // Appointment nesnesini form değerlerinden oluştur
            if (appointment == null)
            {
                appointment = new Appointment();
            }

            // TrainerId
            if (!string.IsNullOrEmpty(trainerIdValue) && int.TryParse(trainerIdValue, out int trainerId))
            {
                appointment.TrainerId = trainerId;
            }
            else
            {
                appointment.TrainerId = 0;
            }

            // GymServiceId
            if (!string.IsNullOrEmpty(gymServiceIdValue) && int.TryParse(gymServiceIdValue, out int gymServiceId))
            {
                appointment.GymServiceId = gymServiceId;
            }
            else
            {
                appointment.GymServiceId = 0;
            }

            // AppointmentDate
            if (!string.IsNullOrEmpty(appointmentDateValue) && DateTime.TryParse(appointmentDateValue, out DateTime appointmentDate))
            {
                appointment.AppointmentDate = appointmentDate;
            }

            // AppointmentTime - TimeSpan'a çevir (HH:mm formatından)
            if (!string.IsNullOrEmpty(appointmentTimeValue) && TimeSpan.TryParse(appointmentTimeValue, out TimeSpan appointmentTime))
            {
                appointment.AppointmentTime = appointmentTime;
            }

            // Notes
            appointment.Notes = notesValue;

            var member = await _context.Members
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                ModelState.AddModelError("", "Üye kaydı bulunamadı.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // Üyenin kayıtlı olduğu spor salonunu kontrol et
            if (member.GymId == null || member.Gym == null)
            {
                ModelState.AddModelError("", "Randevu almak için önce bir spor salonuna kayıt olmanız gerekmektedir. Lütfen admin ile iletişime geçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // MemberId'yi set et
            appointment.MemberId = member.Id;

            // TrainerId ve GymServiceId kontrolü
            if (appointment.TrainerId == 0)
            {
                ModelState.AddModelError("TrainerId", "Lütfen bir antrenör seçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }
            if (appointment.GymServiceId == 0)
            {
                ModelState.AddModelError("GymServiceId", "Lütfen bir hizmet seçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }
            
            // AppointmentDate ve AppointmentTime kontrolü
            if (appointment.AppointmentDate == default(DateTime))
            {
                System.Diagnostics.Debug.WriteLine($"HATA: AppointmentDate default değerde - '{appointmentDateValue}' parse edilemedi");
                ModelState.AddModelError("AppointmentDate", "Lütfen bir randevu tarihi seçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }
            
            if (appointment.AppointmentTime == default(TimeSpan))
            {
                System.Diagnostics.Debug.WriteLine($"HATA: AppointmentTime default değerde - '{appointmentTimeValue}' parse edilemedi");
                ModelState.AddModelError("AppointmentTime", "Lütfen bir randevu saati seçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }
            
            System.Diagnostics.Debug.WriteLine($"Değerler parse edildi - TrainerId: {appointment.TrainerId}, GymServiceId: {appointment.GymServiceId}, Date: {appointment.AppointmentDate}, Time: {appointment.AppointmentTime}");

            // GymService kontrolü ve Duration, Price alma
            var gymService = await _context.GymServices
                .Include(gs => gs.Service)
                .Include(gs => gs.Gym)
                .FirstOrDefaultAsync(gs => gs.Id == appointment.GymServiceId);

            if (gymService == null || !gymService.IsActive)
            {
                ModelState.AddModelError("", "Seçilen hizmet bulunamadı veya aktif değil.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // Seçilen hizmetin üyenin kayıtlı olduğu spor salonuna ait olduğunu kontrol et
            if (gymService.GymId != member.GymId)
            {
                ModelState.AddModelError("", "Seçilen hizmet kayıtlı olduğunuz spor salonuna ait değil.");
                await PopulateViewBagForError(appointment);
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
                await PopulateViewBagForError(appointment);
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
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // Saat aralığı kontrolü
            var endTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(appointment.Duration));
            if (appointment.AppointmentTime < availability.StartTime || endTime > availability.EndTime)
            {
                ModelState.AddModelError("", $"Randevu saati antrenörün müsaitlik saatleri içinde olmalıdır ({availability.StartTime:hh\\:mm} - {availability.EndTime:hh\\:mm}).");
                await PopulateViewBagForError(appointment);
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
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // GymService zaten yukarıda kontrol edildi ve Duration, Price set edildi
            appointment.Status = "Pending";
            appointment.CreatedDate = DateTime.Now;

            // Debug: Tüm değerleri logla
            System.Diagnostics.Debug.WriteLine($"Randevu kaydedilmeye hazır - MemberId: {appointment.MemberId}, TrainerId: {appointment.TrainerId}, GymServiceId: {appointment.GymServiceId}");
            System.Diagnostics.Debug.WriteLine($"Date: {appointment.AppointmentDate}, Time: {appointment.AppointmentTime}, Duration: {appointment.Duration}, Price: {appointment.Price}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                System.Diagnostics.Debug.WriteLine($"ModelState hataları: {string.Join(", ", errors)}");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            // Tüm kontroller başarılı, randevuyu kaydet
            try
            {
                System.Diagnostics.Debug.WriteLine("Randevu veritabanına ekleniyor...");
                _context.Appointments.Add(appointment);
                System.Diagnostics.Debug.WriteLine("SaveChangesAsync çağrılıyor...");
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Randevu başarıyla kaydedildi!");

                // Randevu bilgilerini al
                var savedAppointment = await _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.GymService)
                        .ThenInclude(gs => gs.Service)
                    .FirstOrDefaultAsync(a => a.Id == appointment.Id);

                if (savedAppointment != null)
                {
                    // Trainer'a bildirim gönder
                    var trainerForNotification = await _context.Trainers
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == savedAppointment.TrainerId);
                    
                    if (trainerForNotification?.User != null)
                    {
                        await CreateNotificationAsync(
                            trainerForNotification.User.Id,
                            "Yeni Randevu Talebi",
                            $"{savedAppointment.Member?.FullName} adlı üye {savedAppointment.AppointmentDate:dd.MM.yyyy} tarihinde {savedAppointment.AppointmentTime:hh\\:mm} saatinde randevu talebinde bulundu.",
                            savedAppointment.Id
                        );
                    }

                    // Tüm Admin'lere bildirim gönder
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    foreach (var admin in adminUsers)
                    {
                        await CreateNotificationAsync(
                            admin.Id,
                            "Yeni Randevu Talebi",
                            $"{savedAppointment.Member?.FullName} adlı üye {savedAppointment.AppointmentDate:dd.MM.yyyy} tarihinde {savedAppointment.AppointmentTime:hh\\:mm} saatinde randevu talebinde bulundu.",
                            savedAppointment.Id
                        );
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Randevunuz başarıyla oluşturuldu. Onay bekleniyor.";
                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Randevu kaydedilirken hata: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", $"Randevu kaydedilirken bir hata oluştu: {ex.Message}");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }
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

            var today = DateTime.Today;
            var activeAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
                           a.AppointmentDate >= today && 
                           a.Status != "Completed" && 
                           a.Status != "Cancelled")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            var pastAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
                           (a.AppointmentDate < today || a.Status == "Completed" || a.Status == "Cancelled"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.ActiveAppointments = activeAppointments;
            ViewBag.PastAppointments = pastAppointments;

            return View(activeAppointments);
        }

        public async Task<IActionResult> PastAppointments()
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

            var today = DateTime.Today;
            var appointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
                           (a.AppointmentDate < today || a.Status == "Completed" || a.Status == "Cancelled"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        [HttpGet]
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

        [HttpGet]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int id)
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

            var appointment = await _context.Appointments
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.MemberId == member.Id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Randevu bulunamadı." });
            }

            if (appointment.Status == "Cancelled")
            {
                return Json(new { success = false, message = "Randevu zaten iptal edilmiş." });
            }

            var oldStatus = appointment.Status;
            appointment.Status = "Cancelled";
            appointment.UpdatedDate = DateTime.Now;

            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                // Trainer'a bildirim gönder
                if (appointment.Trainer?.User != null)
                {
                    await CreateNotificationAsync(
                        appointment.Trainer.User.Id,
                        "Randevu İptal Edildi",
                        $"{appointment.Member?.FullName} adlı üye {appointment.AppointmentDate:dd.MM.yyyy} tarihinde {appointment.AppointmentTime:hh\\:mm} saatindeki randevuyu iptal etti.",
                        appointment.Id
                    );
                }

                // Tüm Admin'lere bildirim gönder
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await CreateNotificationAsync(
                        admin.Id,
                        "Randevu İptal Edildi",
                        $"{appointment.Member?.FullName} adlı üye {appointment.AppointmentDate:dd.MM.yyyy} tarihinde {appointment.AppointmentTime:hh\\:mm} saatindeki randevuyu iptal etti.",
                        appointment.Id
                    );
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Randevu başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }
}


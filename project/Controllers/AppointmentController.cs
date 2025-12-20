/*
Üye randevu işlemleri controller'ı. 
Üyelerin randevu oluşturma, randevularını görüntüleme ve iptal etme işlemlerini yönetir.
AppointmentsApiController ile birlikte çalışır - API controller dinamik veri sağlarken bu controller sayfa gösterimi ve iş mantığını yönetir.
*/

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

        /// <summary>
        /// Kullanıcıya bildirim oluşturur - randevu işlemleri sonrası kullanılır
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
        /// Hata durumunda view için gerekli verileri ViewBag'e yükler - form hatalarında sayfayı yeniden doldurmak için
        /// </summary>
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

        /// <summary>
        /// Randevu oluşturma form sayfasını gösterir - üyenin kayıtlı olduğu spor salonu bilgilerini yükler
        /// </summary>
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

            if (member.GymId == null || member.Gym == null)
            {
                ViewBag.MemberGym = null;
                ViewBag.MemberGymId = null;
                ViewBag.HasGym = false;
                ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
                return View();
            }

            ViewBag.MemberGym = member.Gym;
            ViewBag.MemberGymId = member.GymId;
            ViewBag.HasGym = true;
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            
            return View();
        }

        /// <summary>
        /// Randevu oluşturur - validasyon, müsaitlik kontrolü, çakışma kontrolü yapar, kaydeder ve bildirim gönderir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ModelState.Clear();

            var trainerIdValue = Request.Form["TrainerId"].FirstOrDefault() ?? "";
            var gymServiceIdValue = Request.Form["GymServiceId"].FirstOrDefault() ?? "";
            var appointmentDateValue = Request.Form["AppointmentDate"].FirstOrDefault() ?? "";
            var appointmentTimeValue = Request.Form["AppointmentTime"].FirstOrDefault() ?? "";
            var notesValue = Request.Form["Notes"].FirstOrDefault() ?? "";

            System.Diagnostics.Debug.WriteLine($"Form değerleri - TrainerId: '{trainerIdValue}', GymServiceId: '{gymServiceIdValue}', Date: '{appointmentDateValue}', Time: '{appointmentTimeValue}'");
            
            if (appointment == null)
            {
                appointment = new Appointment();
            }

            if (!string.IsNullOrEmpty(trainerIdValue) && int.TryParse(trainerIdValue, out int trainerId))
            {
                appointment.TrainerId = trainerId;
            }
            else
            {
                appointment.TrainerId = 0;
            }

            if (!string.IsNullOrEmpty(gymServiceIdValue) && int.TryParse(gymServiceIdValue, out int gymServiceId))
            {
                appointment.GymServiceId = gymServiceId;
            }
            else
            {
                appointment.GymServiceId = 0;
            }

            if (!string.IsNullOrEmpty(appointmentDateValue) && DateTime.TryParse(appointmentDateValue, out DateTime appointmentDate))
            {
                appointment.AppointmentDate = appointmentDate;
            }

            if (!string.IsNullOrEmpty(appointmentTimeValue) && TimeSpan.TryParse(appointmentTimeValue, out TimeSpan appointmentTime))
            {
                appointment.AppointmentTime = appointmentTime;
            }

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

            if (member.GymId == null || member.Gym == null)
            {
                ModelState.AddModelError("", "Randevu almak için önce bir spor salonuna kayıt olmanız gerekmektedir. Lütfen admin ile iletişime geçin.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            appointment.MemberId = member.Id;

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

            if (gymService.GymId != member.GymId)
            {
                ModelState.AddModelError("", "Seçilen hizmet kayıtlı olduğunuz spor salonuna ait değil.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            appointment.Duration = gymService.Duration;
            appointment.Price = gymService.Price;
            ModelState.Remove("MemberId"); 

            var endTime = appointment.AppointmentTime.Add(TimeSpan.FromMinutes(appointment.Duration));

            var gym = await _context.Gyms
                .Include(g => g.GymServices)
                .FirstOrDefaultAsync(g => g.Id == member.GymId);

            if (gym != null && !string.IsNullOrWhiteSpace(gym.WorkingDays))
            {
                int dayOfWeekForGym = (int)appointment.AppointmentDate.DayOfWeek;
                var workingDays = gym.WorkingDaysList;
                
                if (!workingDays.Contains(dayOfWeekForGym))
                {
                    ModelState.AddModelError("AppointmentDate", $"Seçilen tarih spor salonunun çalışma günleri içinde değil. Spor salonu sadece {gym.WorkingDaysText} günleri çalışmaktadır.");
                    await PopulateViewBagForError(appointment);
                    return View(appointment);
                }
            }

            if (gym != null)
            {
                if (appointment.AppointmentTime < gym.OpeningTime || endTime > gym.ClosingTime)
                {
                    ModelState.AddModelError("AppointmentTime", $"Randevu saati spor salonunun çalışma saatleri içinde olmalıdır. Spor salonu {gym.OpeningTime:hh\\:mm} - {gym.ClosingTime:hh\\:mm} saatleri arasında açıktır.");
                    await PopulateViewBagForError(appointment);
                    return View(appointment);
                }
            }

            var trainer = await _context.Trainers
                .Include(t => t.TrainerAvailabilities)
                .FirstOrDefaultAsync(t => t.Id == appointment.TrainerId);

            if (trainer == null || !trainer.IsActive)
            {
                ModelState.AddModelError("", "Seçilen antrenör bulunamadı veya aktif değil.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            int dayOfWeek = (int)appointment.AppointmentDate.DayOfWeek;

            var availability = trainer.TrainerAvailabilities
                .FirstOrDefault(a => a.DayOfWeek == dayOfWeek && a.IsAvailable);

            if (availability == null)
            {
                ModelState.AddModelError("", "Seçilen tarih ve saatte antrenör müsait değil.");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            if (appointment.AppointmentTime < availability.StartTime || endTime > availability.EndTime)
            {
                ModelState.AddModelError("", $"Randevu saati antrenörün müsaitlik saatleri içinde olmalıdır ({availability.StartTime:hh\\:mm} - {availability.EndTime:hh\\:mm}).");
                await PopulateViewBagForError(appointment);
                return View(appointment);
            }

            var existingAppointments = await _context.Appointments
                .Where(a => 
                    a.TrainerId == appointment.TrainerId &&
                    a.AppointmentDate == appointment.AppointmentDate &&
                    a.Status != "Cancelled" &&
                    a.Status != "Rejected")
                .ToListAsync();

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

            appointment.Status = "Pending";
            appointment.CreatedDate = DateTime.Now;

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

            try
            {
                System.Diagnostics.Debug.WriteLine("Randevu veritabanına ekleniyor...");
                _context.Appointments.Add(appointment);
                System.Diagnostics.Debug.WriteLine("SaveChangesAsync çağrılıyor...");
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Randevu başarıyla kaydedildi!");

                var savedAppointment = await _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.GymService)
                        .ThenInclude(gs => gs.Service)
                    .FirstOrDefaultAsync(a => a.Id == appointment.Id);

                if (savedAppointment != null)
                {
                    var trainerForNotification = await _context.Trainers
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == savedAppointment.TrainerId);
                    
                    var notifiedUserIds = new HashSet<string>();

                    if (trainerForNotification?.User != null && !string.IsNullOrEmpty(trainerForNotification.User.Id))
                    {
                        await CreateNotificationAsync(
                            trainerForNotification.User.Id,
                            "Yeni Randevu Talebi",
                            $"{savedAppointment.Member?.FullName} adlı üye {savedAppointment.AppointmentDate:dd.MM.yyyy} tarihinde {savedAppointment.AppointmentTime:hh\\:mm} saatinde randevu talebinde bulundu.",
                            savedAppointment.Id
                        );
                        notifiedUserIds.Add(trainerForNotification.User.Id);
                    }

                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    foreach (var admin in adminUsers)
                    {
                        if (!string.IsNullOrEmpty(admin.Id) && !notifiedUserIds.Contains(admin.Id))
                        {
                            await CreateNotificationAsync(
                                admin.Id,
                                "Yeni Randevu Talebi",
                                $"{savedAppointment.Member?.FullName} adlı üye {savedAppointment.AppointmentDate:dd.MM.yyyy} tarihinde {savedAppointment.AppointmentTime:hh\\:mm} saatinde randevu talebinde bulundu.",
                                savedAppointment.Id
                            );
                            notifiedUserIds.Add(admin.Id);
                        }
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

        /// <summary>
        /// Üyenin randevularını listeler - güncel, geçmiş ve iptal/reddedilmiş randevuları üç kategoriye ayırır
        /// </summary>
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
            
            var currentAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
                           a.AppointmentDate >= today && 
                           a.Status != "Completed" && 
                           a.Status != "Cancelled" &&
                           a.Status != "Rejected")
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            var pastAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
                           a.Status == "Completed" &&
                           (a.AppointmentDate < today || a.Status == "Completed"))
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            var cancelledRejectedAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .Where(a => a.MemberId == member.Id && 
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
        /// Üyenin geçmiş randevularını listeler - tamamlanmış veya iptal edilmiş randevular
        /// </summary>
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

        /// <summary>
        /// Üyenin randevusunu iptal eder - durumu "Cancelled" yapar, antrenör ve admin'lere bildirim gönderir (JSON döner)
        /// </summary>
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

                var notifiedUserIds = new HashSet<string>();

                if (appointment.Trainer?.User != null && !string.IsNullOrEmpty(appointment.Trainer.User.Id))
                {
                    await CreateNotificationAsync(
                        appointment.Trainer.User.Id,
                        "Randevu İptal Edildi",
                        $"{appointment.Member?.FullName} adlı üye {appointment.AppointmentDate:dd.MM.yyyy} tarihinde {appointment.AppointmentTime:hh\\:mm} saatindeki randevuyu iptal etti.",
                        appointment.Id
                    );
                    notifiedUserIds.Add(appointment.Trainer.User.Id);
                }

                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrEmpty(admin.Id) && !notifiedUserIds.Contains(admin.Id))
                    {
                        await CreateNotificationAsync(
                            admin.Id,
                            "Randevu İptal Edildi",
                            $"{appointment.Member?.FullName} adlı üye {appointment.AppointmentDate:dd.MM.yyyy} tarihinde {appointment.AppointmentTime:hh\\:mm} saatindeki randevuyu iptal etti.",
                            appointment.Id
                        );
                        notifiedUserIds.Add(admin.Id);
                    }
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


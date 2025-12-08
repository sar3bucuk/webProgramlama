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

        public async Task<IActionResult> Index()
        {
            // Spor salonları ve istatistikleri
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

        // Spor Salonu Yönetimi
        public async Task<IActionResult> Gyms()
        {
            var gyms = await _context.Gyms.OrderBy(g => g.Name).ToListAsync();
            return View(gyms);
        }

        [HttpGet]
        public async Task<IActionResult> GymDetails(int? id)
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

            // GymServices'i sırala
            if (gym.GymServices != null)
            {
                gym.GymServices = gym.GymServices.OrderBy(gs => gs.Service != null ? gs.Service.Name : "").ToList();
            }

            // Trainers'ı sırala
            if (gym.Trainers != null)
            {
                gym.Trainers = gym.Trainers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName).ToList();
            }

            return View(gym);
        }

        [HttpGet]
        public IActionResult CreateGym()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGym(Gym gym)
        {
            if (ModelState.IsValid)
            {
                gym.CreatedDate = DateTime.Now;
                _context.Gyms.Add(gym);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Spor salonu başarıyla eklendi.";
                return RedirectToAction(nameof(Gyms));
            }
            return View(gym);
        }

        [HttpGet]
        public async Task<IActionResult> EditGym(int? id)
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
            return View(gym);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGym(int id, Gym gym)
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
                return RedirectToAction(nameof(Gyms));
            }
            return View(gym);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGymConfirmed(int id)
        {
            var gym = await _context.Gyms.FindAsync(id);
            if (gym != null)
            {
                _context.Gyms.Remove(gym);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Spor salonu başarıyla silindi.";
            }

            return RedirectToAction(nameof(Gyms));
        }

        // Hizmet Yönetimi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGymService(int gymId, int serviceId, int duration, string price, bool isActive = true)
        {
            // Aynı hizmet zaten ekli mi kontrol et
            var existingService = await _context.GymServices
                .FirstOrDefaultAsync(gs => gs.GymId == gymId && gs.ServiceId == serviceId);
            
            if (existingService != null)
            {
                return Json(new { success = false, message = "Bu hizmet zaten eklenmiş." });
            }

            // String olarak gelen price değerini InvariantCulture ile parse et
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGymService(int id, int duration, string price, bool isActive)
        {
            var gymService = await _context.GymServices.FindAsync(id);
            if (gymService == null)
            {
                return Json(new { success = false, message = "Hizmet bulunamadı." });
            }

            // String olarak gelen price değerini InvariantCulture ile parse et
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

        // Üye Yönetimi
        public async Task<IActionResult> Members(string searchName, int? gymId)
        {
            var query = _context.Members
                .Include(m => m.User)
                .Include(m => m.Gym)
                .AsQueryable();

            // İsme göre arama
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                searchName = searchName.Trim();
                query = query.Where(m => 
                    (m.FirstName + " " + m.LastName).Contains(searchName) ||
                    m.FirstName.Contains(searchName) ||
                    m.LastName.Contains(searchName));
            }

            // Spor salonuna göre filtreleme
            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }
            else if (gymId == 0)
            {
                // 0 değeri "Bağlı değil" anlamına gelir
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMemberGym(int memberId, int? gymId)
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            // gymId 0 ise null yap (bağlantıyı kaldır)
            if (gymId == 0)
            {
                gymId = null;
            }
            else if (gymId.HasValue)
            {
                // Gym var mı kontrol et
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

        // Antrenör Yönetimi
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

        [HttpGet]
        public async Task<IActionResult> CreateTrainer()
        {
            ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(Trainer trainer, string email, string password)
        {
            // E-posta kontrolü
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("email", "E-posta adresi gereklidir.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                return View(trainer);
            }

            // E-posta format kontrolü
            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("email", "Geçerli bir e-posta adresi giriniz.");
                ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                return View(trainer);
            }

            // E-posta ile kullanıcı bul veya oluştur
            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                // Yeni kullanıcı oluştur
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

                // Trainer rolü ver
                await _userManager.AddToRoleAsync(user, "Trainer");
            }
            else
            {
                // Kullanıcı zaten var, Trainer rolü var mı kontrol et
                if (!await _userManager.IsInRoleAsync(user, "Trainer"))
                {
                    await _userManager.AddToRoleAsync(user, "Trainer");
                }

                // Bu kullanıcı zaten antrenör olarak kayıtlı mı kontrol et
                var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (existingTrainer != null)
                {
                    ModelState.AddModelError("", "Bu e-posta adresi ile zaten bir antrenör kayıtlı.");
                    ViewBag.Gyms = await _context.Gyms.Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
                    return View(trainer);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(int id, Trainer trainer)
        {
            if (id != trainer.Id)
            {
                return NotFound();
            }

            // Mevcut trainer'ı bul
            var existingTrainer = await _context.Trainers
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (existingTrainer == null)
            {
                return NotFound();
            }

            // Email validation hatasını temizle (IdentityUser'dan alıyoruz)
            ModelState.Remove("Email");
            // UserId validation hatasını temizle (zaten mevcut)
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Sadece değişen alanları güncelle
                    existingTrainer.FirstName = trainer.FirstName;
                    existingTrainer.LastName = trainer.LastName;
                    existingTrainer.Phone = trainer.Phone;
                    existingTrainer.Bio = trainer.Bio;
                    existingTrainer.ExperienceYears = trainer.ExperienceYears;
                    existingTrainer.IsActive = trainer.IsActive;
                    existingTrainer.GymId = trainer.GymId;
                    
                    // Email'i IdentityUser'dan al (senkronize tut)
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
            ViewBag.Services = await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            
            // View'a göndermek için existingTrainer'ı kullan (navigation property'ler dahil)
            existingTrainer.FirstName = trainer.FirstName;
            existingTrainer.LastName = trainer.LastName;
            existingTrainer.Phone = trainer.Phone;
            existingTrainer.Bio = trainer.Bio;
            existingTrainer.ExperienceYears = trainer.ExperienceYears;
            existingTrainer.IsActive = trainer.IsActive;
            existingTrainer.GymId = trainer.GymId;
            
            return View(existingTrainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerService(int trainerId, int serviceId)
        {
            // Aynı hizmet zaten ekli mi kontrol et
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTrainerAvailability(int trainerId, int dayOfWeek, string startTime, string endTime, bool isAvailable = true)
        {
            // Aynı gün ve saat aralığı zaten var mı kontrol et
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

        // Randevu Yönetimi
        public async Task<IActionResult> Appointments()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Member)
                    .ThenInclude(m => m.User)
                .Include(a => a.Trainer)
                    .ThenInclude(t => t.Gym)
                .Include(a => a.GymService)
                    .ThenInclude(gs => gs.Service)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

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

            appointment.Status = status;
            appointment.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Randevu durumu başarıyla güncellendi." });
        }

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
                // Randevuları kontrol et ve sil (NoAction olduğu için manuel silmemiz gerekiyor)
                if (trainer.Appointments != null && trainer.Appointments.Any())
                {
                    _context.Appointments.RemoveRange(trainer.Appointments);
                }

                // IdentityUser'ı önce sil (Identity sistemini temizlemek için)
                if (trainer.User != null)
                {
                    await _userManager.DeleteAsync(trainer.User);
                }

                // Trainer'ı sil (TrainerServices, TrainerSpecializations, TrainerAvailabilities cascade delete ile otomatik silinecek)
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


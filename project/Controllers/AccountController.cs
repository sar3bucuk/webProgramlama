using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    isRead = n.IsRead,
                    createdDate = n.CreatedDate,
                    appointmentId = n.AppointmentId
                })
                .ToListAsync();

            return Json(new { success = true, notifications = notifications });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, count = 0 });
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.Id && !n.IsRead);

            return Json(new { success = true, count = count });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);

            if (notification == null)
            {
                return Json(new { success = false, message = "Bildirim bulunamadı." });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Email ile kullanıcıyı bul
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !string.IsNullOrEmpty(user.UserName))
                {
                    // UserName ile giriş yap (Identity'de UserName kullanılır)
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
                        // Rol kontrolü
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Trainer"))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        
                        return RedirectToAction("Index", "Home");
                    }
                    else if (result.IsLockedOut)
                    {
                        ModelState.AddModelError(string.Empty, "Hesabınız kilitlenmiştir.");
                    }
                    else if (result.IsNotAllowed)
                    {
                        ModelState.AddModelError(string.Empty, "Giriş yapmanıza izin verilmiyor.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // E-posta ile kullanıcı var mı kontrol et
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    // Kullanıcı zaten var - Trainer rolünde mi kontrol et
                    if (await _userManager.IsInRoleAsync(existingUser, "Trainer"))
                    {
                        // Trainer olarak kayıtlı mı kontrol et
                        var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == existingUser.Id);
                        if (existingTrainer != null)
                        {
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten bir antrenör olarak kayıtlı. Aynı e-posta hem üye hem antrenör olamaz.");
                            return View(model);
                        }
                    }

                    // Member olarak kayıtlı mı kontrol et
                    var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.UserId == existingUser.Id);
                    if (existingMember != null)
                    {
                        ModelState.AddModelError("Email", "Bu e-posta adresi zaten bir üye olarak kayıtlı.");
                        return View(model);
                    }
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Yeni kullanıcıya Member rolü ver
                    await _userManager.AddToRoleAsync(user, "Member");
                    
                    // Members tablosuna kayıt ekle
                    var member = new Member
                    {
                        UserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        CreatedDate = DateTime.Now
                    };
                    
                    _context.Members.Add(member);
                    await _context.SaveChangesAsync();
                    
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}


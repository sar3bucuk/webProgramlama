/*
Giriş, kayıt ve çıkış işlemleri bu controller'da yönetilir. 
Üyeler kayıt olabilir, tüm kullanıcılar giriş yapabilir.
*/

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

        /// <summary>
        /// Giriş sayfasını gösterir
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Kullanıcı giriş işlemini gerçekleştirir - Email ve şifre ile doğrulama yapar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !string.IsNullOrEmpty(user.UserName))
                {
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
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

        /// <summary>
        /// Üye kayıt sayfasını gösterir
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Yeni üye kaydı oluşturur - IdentityUser ve Members tablosuna kayıt ekler, Member rolü atar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    if (await _userManager.IsInRoleAsync(existingUser, "Trainer"))
                    {
                        var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == existingUser.Id);
                        if (existingTrainer != null)
                        {
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten bir antrenör olarak kayıtlı. Aynı e-posta hem üye hem antrenör olamaz.");
                            return View(model);
                        }
                    }

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
                    await _userManager.AddToRoleAsync(user, "Member");
                    
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

        /// <summary>
        /// Kullanıcı oturumunu sonlandırır ve ana sayfaya yönlendirir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}


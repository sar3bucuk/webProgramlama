using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public MemberController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.Gym)
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
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
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Member member)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingMember = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (existingMember == null || existingMember.Id != member.Id)
            {
                return NotFound();
            }

            // UserId validation hatasını temizle (zaten mevcut)
            ModelState.Remove("UserId");
            ModelState.Remove("User");
            ModelState.Remove("Gym");
            ModelState.Remove("GymId"); // Üyeler spor salonunu değiştiremez
            ModelState.Remove("Appointments");
            ModelState.Remove("AIRecommendations");

            if (ModelState.IsValid)
            {
                try
                {
                    // Sadece üyenin düzenleyebileceği alanları güncelle
                    // GymId değiştirilemez - sadece admin değiştirebilir
                    existingMember.FirstName = member.FirstName;
                    existingMember.LastName = member.LastName;
                    existingMember.Phone = member.Phone;
                    existingMember.DateOfBirth = member.DateOfBirth;
                    existingMember.Gender = member.Gender;
                    existingMember.Height = member.Height;
                    existingMember.Weight = member.Weight;
                    existingMember.BodyType = member.BodyType;
                    existingMember.HealthConditions = member.HealthConditions;
                    // existingMember.GymId = member.GymId; // Üyeler spor salonunu değiştiremez
                    existingMember.UpdatedDate = DateTime.Now;

                    _context.Update(existingMember);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Members.AnyAsync(e => e.Id == member.Id))
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
                    ModelState.AddModelError("", $"Profil güncellenirken bir hata oluştu: {ex.Message}");
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
            return View(member);
        }
    }
}

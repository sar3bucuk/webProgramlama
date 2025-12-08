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
                    // Sadece antrenörün kendi bilgilerini güncelle
                    existingTrainer.FirstName = trainer.FirstName;
                    existingTrainer.LastName = trainer.LastName;
                    existingTrainer.Phone = trainer.Phone;
                    // Email IdentityUser'dan alınır, değiştirilemez
                    existingTrainer.Email = currentUser.Email;
                    existingTrainer.Bio = trainer.Bio;
                    existingTrainer.ExperienceYears = trainer.ExperienceYears;
                    // GymId ve IsActive admin tarafından yönetilir, antrenör değiştiremez

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
    }
}


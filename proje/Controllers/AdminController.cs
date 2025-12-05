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
            // Toplam üye sayısı: Member rolüne sahip tüm kullanıcılar
            var totalMembers = await (from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      where r.Name == "Member"
                                      select ur.UserId)
                                      .Distinct()
                                      .CountAsync();

            var viewModel = new Models.AdminDashboardViewModel
            {
                TotalGyms = await _context.Gyms.CountAsync(),
                ActiveGyms = await _context.Gyms.CountAsync(g => g.IsActive),
                TotalServices = await _context.Services.CountAsync(),
                TotalTrainers = await _context.Trainers.CountAsync(),
                ActiveTrainers = await _context.Trainers.CountAsync(t => t.IsActive),
                TotalMembers = totalMembers,
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == "Pending"),
                ApprovedAppointments = await _context.Appointments.CountAsync(a => a.Status == "Approved"),
                TotalUsers = _userManager.Users.Count(),
                TotalRevenue = await _context.Appointments
                    .Where(a => a.Status == "Approved" || a.Status == "Completed")
                    .SumAsync(a => (decimal?)a.Price) ?? 0,
                RecentAppointments = await _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .Include(a => a.GymService)
                        .ThenInclude(gs => gs.Service)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentTime)
                    .Take(10)
                    .Select(a => new Models.RecentAppointment
                    {
                        Id = a.Id,
                        MemberName = a.Member.FirstName + " " + a.Member.LastName,
                        TrainerName = a.Trainer.FirstName + " " + a.Trainer.LastName,
                        ServiceName = a.GymService.Service.Name,
                        AppointmentDate = a.AppointmentDate.Date.Add(a.AppointmentTime),
                        Status = a.Status
                    })
                    .ToListAsync(),
                RecentMembers = await _context.Members
                    .Include(m => m.User)
                    .OrderByDescending(m => m.CreatedDate)
                    .Take(10)
                    .Select(m => new Models.RecentMember
                    {
                        Id = m.Id,
                        FullName = m.FirstName + " " + m.LastName,
                        Email = m.User != null ? m.User.Email ?? "" : "",
                        CreatedDate = m.CreatedDate
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }
    }
}


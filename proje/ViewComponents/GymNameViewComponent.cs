using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;

namespace proje.ViewComponents
{
    public class GymNameViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public GymNameViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // İlk aktif spor salonunun adını al
            var gym = await _context.Gyms
                .Where(g => g.IsActive)
                .OrderBy(g => g.CreatedDate)
                .FirstOrDefaultAsync();

            var gymName = gym?.Name ?? "Fitness Center"; // Varsayılan isim

            return View("Default", gymName);
        }
    }
}


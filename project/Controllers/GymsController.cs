using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proje.Data;

namespace proje.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GymsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GymsController(ApplicationDbContext context)
        {
            _context = context;
        }
    }
}

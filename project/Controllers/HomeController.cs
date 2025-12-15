/*
Ana sayfa ve genel sayfalar controller'ı.
Ana sayfa, gizlilik politikası, hata sayfası ve seed işlemleri burada yönetilir.
*/

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using proje.Models;

namespace proje.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ana sayfayı gösterir - tüm kullanıcılar erişebilir
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Gizlilik politikası sayfasını gösterir
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Seed (veri yükleme) sayfasını gösterir - test verileri için kullanılır
        /// </summary>
        public IActionResult Seed()
        {
            return View();
        }

        /// <summary>
        /// Hata sayfasını gösterir - uygulama hatalarını görüntüler
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

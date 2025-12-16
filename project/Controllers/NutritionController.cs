/*
Beslenme programı oluşturma controller'ı.
Üyelerden bilgileri alarak yapay zeka ile beslenme programı oluşturur.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;
using proje.Services;

namespace proje.Controllers
{
    [Authorize]
    public class NutritionController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly OpenAIService _openAIService;

        public NutritionController(UserManager<IdentityUser> userManager, ApplicationDbContext context, OpenAIService openAIService)
        {
            _userManager = userManager;
            _context = context;
            _openAIService = openAIService;
        }

        /// <summary>
        /// Beslenme programı oluşturma form sayfasını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            // Üyenin mevcut bilgilerini ViewBag'e ekle
            ViewBag.Member = member;
            return View();
        }

        /// <summary>
        /// Beslenme programı oluşturur - kullanıcı bilgilerini toplar, AI'ya gönderir ve sonucu kaydeder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NutritionPlan nutritionPlan)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            ModelState.Remove("Member");
            ModelState.Remove("MemberId");

            if (!ModelState.IsValid)
            {
                ViewBag.Member = member;
                return View(nutritionPlan);
            }

            try
            {
                // AI için prompt oluştur
                var prompt = BuildNutritionPrompt(member, nutritionPlan);

                // AI'dan beslenme programı al
                var aiResponse = await _openAIService.GenerateNutritionPlanAsync(prompt);

                // Beslenme programını kaydet
                nutritionPlan.MemberId = member.Id;
                nutritionPlan.PlanDetails = aiResponse;
                nutritionPlan.CreatedDate = DateTime.Now;

                _context.NutritionPlans.Add(nutritionPlan);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Beslenme programınız başarıyla oluşturuldu!";
                return RedirectToAction(nameof(Details), new { id = nutritionPlan.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Beslenme programı oluşturulurken bir hata oluştu: {ex.Message}");
                ViewBag.Member = member;
                return View(nutritionPlan);
            }
        }

        /// <summary>
        /// Beslenme programı detaylarını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            var nutritionPlan = await _context.NutritionPlans
                .Include(np => np.Member)
                .FirstOrDefaultAsync(np => np.Id == id && np.MemberId == member.Id);

            if (nutritionPlan == null)
            {
                return NotFound();
            }

            return View(nutritionPlan);
        }

        /// <summary>
        /// Üyenin tüm beslenme programlarını listeler
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyPlans()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

            if (member == null)
            {
                return RedirectToAction("Register", "Account");
            }

            var nutritionPlans = await _context.NutritionPlans
                .Where(np => np.MemberId == member.Id)
                .OrderByDescending(np => np.CreatedDate)
                .ToListAsync();

            return View(nutritionPlans);
        }

        /// <summary>
        /// API key test sayfası - API'nin çalışıp çalışmadığını test eder
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestApi()
        {
            try
            {
                var testPrompt = "Merhaba, bu bir test mesajıdır. Lütfen 'API çalışıyor!' yaz.";
                var response = await _openAIService.GenerateNutritionPlanAsync(testPrompt);
                
                ViewBag.Success = true;
                ViewBag.Response = response;
                ViewBag.Message = "✅ API başarıyla çalışıyor!";
            }
            catch (Exception ex)
            {
                ViewBag.Success = false;
                ViewBag.Error = ex.Message;
                ViewBag.Message = "❌ API hatası oluştu!";
            }
            
            return View();
        }

        /// <summary>
        /// AI için detaylı prompt oluşturur
        /// </summary>
        private string BuildNutritionPrompt(Member member, NutritionPlan nutritionPlan)
        {
            var prompt = new System.Text.StringBuilder();
            prompt.AppendLine("Aşağıdaki bilgilere göre kişiselleştirilmiş bir beslenme programı oluştur:\n");

            // Kişisel bilgiler
            prompt.AppendLine("=== KİŞİSEL BİLGİLER ===");
            prompt.AppendLine($"Ad Soyad: {member.FirstName} {member.LastName}");
            if (member.DateOfBirth.HasValue)
            {
                var age = DateTime.Now.Year - member.DateOfBirth.Value.Year;
                if (member.DateOfBirth.Value.Date > DateTime.Now.AddYears(-age)) age--;
                prompt.AppendLine($"Yaş: {age}");
            }
            if (!string.IsNullOrEmpty(member.Gender))
                prompt.AppendLine($"Cinsiyet: {member.Gender}");
            if (member.Height.HasValue)
                prompt.AppendLine($"Boy: {member.Height} cm");
            if (member.Weight.HasValue)
                prompt.AppendLine($"Kilo: {member.Weight} kg");
            if (!string.IsNullOrEmpty(member.BodyType))
                prompt.AppendLine($"Vücut Tipi: {member.BodyType}");
            if (!string.IsNullOrEmpty(member.HealthConditions))
                prompt.AppendLine($"Sağlık Durumu: {member.HealthConditions}");

            prompt.AppendLine("\n=== HEDEF VE TERCİHLER ===");
            if (!string.IsNullOrEmpty(nutritionPlan.Goal))
                prompt.AppendLine($"Hedef: {nutritionPlan.Goal}");
            if (!string.IsNullOrEmpty(nutritionPlan.ActivityLevel))
                prompt.AppendLine($"Aktivite Seviyesi: {nutritionPlan.ActivityLevel}");
            if (!string.IsNullOrEmpty(nutritionPlan.Allergies))
                prompt.AppendLine($"Alerjiler: {nutritionPlan.Allergies}");
            if (!string.IsNullOrEmpty(nutritionPlan.DislikedFoods))
                prompt.AppendLine($"Sevilmeyen Gıdalar: {nutritionPlan.DislikedFoods}");
            if (!string.IsNullOrEmpty(nutritionPlan.SpecialNotes))
                prompt.AppendLine($"Özel Notlar: {nutritionPlan.SpecialNotes}");

            prompt.AppendLine("\n=== MAKRO BESİN HEDEFLERİ ===");
            if (nutritionPlan.DailyCalorieTarget.HasValue)
                prompt.AppendLine($"Günlük Kalori Hedefi: {nutritionPlan.DailyCalorieTarget} kcal");
            if (nutritionPlan.DailyProtein.HasValue)
                prompt.AppendLine($"Günlük Protein: {nutritionPlan.DailyProtein} g");
            if (nutritionPlan.DailyCarbohydrate.HasValue)
                prompt.AppendLine($"Günlük Karbonhidrat: {nutritionPlan.DailyCarbohydrate} g");
            if (nutritionPlan.DailyFat.HasValue)
                prompt.AppendLine($"Günlük Yağ: {nutritionPlan.DailyFat} g");

            prompt.AppendLine("\n=== TALİMATLAR ===");
            prompt.AppendLine("Lütfen aşağıdaki formatta detaylı bir beslenme programı oluştur:");
            prompt.AppendLine("1. Günlük öğün planı (kahvaltı, öğle yemeği, akşam yemeği, ara öğünler)");
            prompt.AppendLine("2. Her öğün için önerilen yemekler ve porsiyon miktarları");
            prompt.AppendLine("3. Türk mutfağına uygun, pratik ve sağlıklı tarifler");
            prompt.AppendLine("4. Makro besin dağılımı (protein, karbonhidrat, yağ)");
            prompt.AppendLine("5. Önemli notlar ve öneriler");
            prompt.AppendLine("6. Programı takip ederken dikkat edilmesi gerekenler");
            prompt.AppendLine("\nYanıtını Türkçe olarak ver ve profesyonel, anlaşılır bir dil kullan.");

            return prompt.ToString();
        }
    }
}


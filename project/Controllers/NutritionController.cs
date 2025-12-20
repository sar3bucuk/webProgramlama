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
        /// Beslenme programını siler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
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
                .FirstOrDefaultAsync(np => np.Id == id && np.MemberId == member.Id);

            if (nutritionPlan == null)
            {
                TempData["ErrorMessage"] = "Beslenme programı bulunamadı veya silme yetkiniz yok.";
                return RedirectToAction(nameof(MyPlans));
            }

            try
            {
                _context.NutritionPlans.Remove(nutritionPlan);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Beslenme programı başarıyla silindi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Beslenme programı silinirken bir hata oluştu: {ex.Message}";
            }

            return RedirectToAction(nameof(MyPlans));
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
            prompt.AppendLine("\nÖNEMLİ: Öğün formatı şu şekilde olmalıdır:");
            prompt.AppendLine("1. Kahvaltı (saat bilgisi ile, örn: Kahvaltı (08:00))");
            prompt.AppendLine("2. İlk Ara Öğün (saat bilgisi ile, örn: Ara Öğün (10:30))");
            prompt.AppendLine("3. Öğle Yemeği (saat bilgisi ile, örn: Öğle Yemeği (12:30))");
            prompt.AppendLine("4. İkinci Ara Öğün (saat bilgisi ile, örn: Ara Öğün (15:30))");
            prompt.AppendLine("5. Akşam Yemeği (saat bilgisi ile, örn: Akşam Yemeği (18:30))");
            prompt.AppendLine("\nÖNEMLİ: Gece yemeği veya gece atıştırmalığı EKLEMEYİN. Sadece yukarıdaki 5 öğünü kullanın.");
            prompt.AppendLine("\nProgram yapısı şu şekilde olmalıdır:");
            prompt.AppendLine("### 1. Günlük Öğün Planı");
            prompt.AppendLine("Her öğün için markdown formatında başlık kullan (örn: #### Kahvaltı (08:00))");
            prompt.AppendLine("Her öğünün altında SADECE o öğüne ait yemekleri listele (liste formatında: - veya *)");
            prompt.AppendLine("\nÖNEMLİ KURALLAR:");
            prompt.AppendLine("- Öğün içinde SADECE yemek listesi olmalı.");
            prompt.AppendLine("- Her öğün için: yemek adı, miktar (gram, porsiyon, su bardağı, adet vb.) ve kısa hazırlama notları (varsa) yazılabilir.");
            prompt.AppendLine("- Su tüketimi ile ilgili kısa bir not eklenebilir (örn: 'Günlük su tüketimine dikkat edin' gibi), ama sadece bir öğünde ve çok kısa.");
            prompt.AppendLine("- Öğün içinde EGZERSİZ, GENEL ÖNERİLER, UYARILAR, DİKKAT EDİLMESİ GEREKENLER, DOKTOR TAVSİYESİ gibi bilgileri ASLA EKLEMEYİN.");
            prompt.AppendLine("- 'Bu program hedeflerinize ulaşmanızda yardımcı olacak', 'Başarılar dilerim', 'Doktora danışın' gibi genel ifadeleri ÖĞÜN İÇİNE EKLEMEYİN.");
            prompt.AppendLine("- Öğün içinde sadece yemek listesi ve miktarları olmalı, başka hiçbir şey olmamalı.");
            prompt.AppendLine("\n### 2. Porsiyon Miktarları");
            prompt.AppendLine("Tüm öğünlerdeki yemeklerin porsiyon miktarlarını özetle");
            prompt.AppendLine("\n### 3. Türk Mutfağına Uygun, Pratik ve Sağlıklı Tarifler");
            prompt.AppendLine("Önerilen yemekler için pratik tarifler ver");
            prompt.AppendLine("\n### 4. Makro Besin Dağılımı");
            prompt.AppendLine("Toplam günlük kalori, protein, karbonhidrat ve yağ dağılımını belirt");
            prompt.AppendLine("\n### 5. Önemli Notlar ve Öneriler");
            prompt.AppendLine("Genel öneriler ve ipuçları");
            prompt.AppendLine("\n### 6. Programı Takip Ederken Dikkat Edilmesi Gerekenler");
            prompt.AppendLine("Uyarılar ve dikkat edilmesi gerekenler (EN SONA KOY)");
            prompt.AppendLine("\nÖNEMLİ: Uyarılar, dikkat edilmesi gerekenler ve genel bilgileri ÖĞÜN İÇİNE EKLEMEYİN. Bunları sadece 5. ve 6. bölümlerde belirtin.");
            prompt.AppendLine("Her öğün sadece o öğüne ait yemekleri içermelidir.");
            prompt.AppendLine("\nYanıtını Türkçe olarak ver ve profesyonel, anlaşılır bir dil kullan.");

            return prompt.ToString();
        }
    }
}


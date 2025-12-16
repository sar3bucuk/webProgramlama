using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class NutritionPlan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Üye gereklidir.")]
        [Display(Name = "Üye")]
        public int MemberId { get; set; }

        [StringLength(100)]
        [Display(Name = "Hedef")]
        public string? Goal { get; set; } // Kilo verme, kilo alma, kas kütlesi artırma, genel sağlık

        [Range(0.01, double.MaxValue, ErrorMessage = "Günlük kalori 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(8,2)")]
        [Display(Name = "Günlük Kalori Hedefi")]
        public decimal? DailyCalorieTarget { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Protein 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Günlük Protein (g)")]
        public decimal? DailyProtein { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Karbonhidrat 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Günlük Karbonhidrat (g)")]
        public decimal? DailyCarbohydrate { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Yağ 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Günlük Yağ (g)")]
        public decimal? DailyFat { get; set; }

        [Display(Name = "Beslenme Programı Detayları")]
        public string? PlanDetails { get; set; } // AI tarafından oluşturulan detaylı program (JSON veya text)

        [StringLength(1000)]
        [Display(Name = "Özel Notlar")]
        public string? SpecialNotes { get; set; } // Alerjiler, tercihler, kısıtlamalar

        [StringLength(500)]
        [Display(Name = "Alerjiler")]
        public string? Allergies { get; set; }

        [StringLength(500)]
        [Display(Name = "Sevilmeyen Gıdalar")]
        public string? DislikedFoods { get; set; }

        [StringLength(50)]
        [Display(Name = "Aktivite Seviyesi")]
        public string? ActivityLevel { get; set; } // Sedanter, hafif aktif, orta aktif, çok aktif

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("MemberId")]
        public virtual Member Member { get; set; } = null!;
    }
}


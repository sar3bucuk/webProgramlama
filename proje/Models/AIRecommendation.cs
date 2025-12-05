using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class AIRecommendation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Üye gereklidir.")]
        [Display(Name = "Üye")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "İstek tipi gereklidir.")]
        [StringLength(50)]
        [Display(Name = "İstek Tipi")]
        public string RequestType { get; set; } = string.Empty; // ExercisePlan, DietPlan, BodyTransformation

        [Display(Name = "Giriş Verileri")]
        public string? InputData { get; set; } // JSON formatında

        [StringLength(500, ErrorMessage = "Fotoğraf yolu en fazla 500 karakter olabilir.")]
        [Display(Name = "Fotoğraf Yolu")]
        public string? PhotoPath { get; set; }

        [Display(Name = "AI Yanıtı")]
        public string? AIResponse { get; set; }

        [StringLength(500, ErrorMessage = "Oluşturulan görsel yolu en fazla 500 karakter olabilir.")]
        [Display(Name = "Oluşturulan Görsel Yolu")]
        public string? GeneratedImagePath { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("MemberId")]
        public virtual Member Member { get; set; } = null!;
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class TrainerAvailability
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Haftanın günü gereklidir.")]
        [Range(0, 6, ErrorMessage = "Haftanın günü 0-6 arasında olmalıdır (0=Pazar, 6=Cumartesi).")]
        [Display(Name = "Haftanın Günü")]
        public int DayOfWeek { get; set; } 

        [Required(ErrorMessage = "Başlangıç saati gereklidir.")]
        [Display(Name = "Başlangıç Saati")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Bitiş saati gereklidir.")]
        [Display(Name = "Bitiş Saati")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Müsait")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; } = null!;

        [NotMapped]
        [Display(Name = "Gün")]
        public string DayName => DayOfWeek switch
        {
            0 => "Pazar",
            1 => "Pazartesi",
            2 => "Salı",
            3 => "Çarşamba",
            4 => "Perşembe",
            5 => "Cuma",
            6 => "Cumartesi",
            _ => "Bilinmiyor"
        };
    }
}


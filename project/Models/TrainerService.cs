using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class TrainerService
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Hizmet gereklidir.")]
        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; } = null!;

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class TrainerSpecialization
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı gereklidir.")]
        [StringLength(100, ErrorMessage = "Uzmanlık alanı en fazla 100 karakter olabilir.")]
        [Display(Name = "Uzmanlık Alanı")]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; } = null!;
    }
}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class GymService
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Spor salonu gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Spor salonu seçmelisiniz.")]
        [Display(Name = "Spor Salonu")]
        public int GymId { get; set; }

        [Required(ErrorMessage = "Hizmet gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Hizmet seçmelisiniz.")]
        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Süre gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Süre 1 dakikadan fazla olmalıdır.")]
        [Display(Name = "Süre (Dakika)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Ücret gereklidir.")]
        [Range(0, double.MaxValue, ErrorMessage = "Ücret 0 veya daha büyük olmalıdır.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Ücret")]
        public decimal Price { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}


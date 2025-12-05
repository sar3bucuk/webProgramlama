using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class Trainer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı gereklidir.")]
        [StringLength(450)]
        [Display(Name = "Kullanıcı")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Spor salonu gereklidir.")]
        [Display(Name = "Spor Salonu")]
        public int GymId { get; set; }

        [Required(ErrorMessage = "Ad gereklidir.")]
        [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [StringLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Biyografi")]
        public string? Bio { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Deneyim yılı 0 veya daha büyük olmalıdır.")]
        [Display(Name = "Deneyim Yılı")]
        public int ExperienceYears { get; set; } = 0;

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        [ForeignKey("GymId")]
        public virtual Gym Gym { get; set; } = null!;

        public virtual ICollection<TrainerSpecialization> TrainerSpecializations { get; set; } = new List<TrainerSpecialization>();
        public virtual ICollection<TrainerService> TrainerServices { get; set; } = new List<TrainerService>();
        public virtual ICollection<TrainerAvailability> TrainerAvailabilities { get; set; } = new List<TrainerAvailability>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        [NotMapped]
        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
    }
}


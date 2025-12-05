using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı gereklidir.")]
        [StringLength(450)]
        [Display(Name = "Kullanıcı")]
        public string UserId { get; set; } = string.Empty;

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

        [Display(Name = "Doğum Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10, ErrorMessage = "Cinsiyet en fazla 10 karakter olabilir.")]
        [Display(Name = "Cinsiyet")]
        public string? Gender { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Boy 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Boy (cm)")]
        public decimal? Height { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Kilo 0'dan büyük olmalıdır.")]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Kilo (kg)")]
        public decimal? Weight { get; set; }

        [StringLength(50, ErrorMessage = "Vücut tipi en fazla 50 karakter olabilir.")]
        [Display(Name = "Vücut Tipi")]
        public string? BodyType { get; set; }

        [StringLength(500, ErrorMessage = "Sağlık durumu en fazla 500 karakter olabilir.")]
        [Display(Name = "Sağlık Durumu")]
        public string? HealthConditions { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<AIRecommendation> AIRecommendations { get; set; } = new List<AIRecommendation>();

        [NotMapped]
        [Display(Name = "Ad Soyad")]
        public string FullName => $"{FirstName} {LastName}";
    }
}


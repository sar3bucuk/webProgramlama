using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace proje.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Kullanıcı ID")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "Mesaj")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Randevu ID")]
        public int? AppointmentId { get; set; }

        [Display(Name = "Okundu mu")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}


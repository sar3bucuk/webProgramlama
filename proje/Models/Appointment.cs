using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Üye gereklidir.")]
        [Display(Name = "Üye")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Antrenör gereklidir.")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Hizmet gereklidir.")]
        [Display(Name = "Hizmet")]
        public int GymServiceId { get; set; }

        [Required(ErrorMessage = "Randevu tarihi gereklidir.")]
        [Display(Name = "Randevu Tarihi")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required(ErrorMessage = "Randevu saati gereklidir.")]
        [Display(Name = "Randevu Saati")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        [Required(ErrorMessage = "Süre gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Süre 1 dakikadan fazla olmalıdır.")]
        [Display(Name = "Süre (Dakika)")]
        public int Duration { get; set; }

        [Required(ErrorMessage = "Ücret gereklidir.")]
        [Range(0, double.MaxValue, ErrorMessage = "Ücret 0 veya daha büyük olmalıdır.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Ücret")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Durum gereklidir.")]
        [StringLength(20)]
        [Display(Name = "Durum")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed, Cancelled

        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
        [Display(Name = "Notlar")]
        public string? Notes { get; set; }

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("MemberId")]
        public virtual Member Member { get; set; } = null!;

        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; } = null!;

        [ForeignKey("GymServiceId")]
        public virtual GymService GymService { get; set; } = null!;

        [NotMapped]
        [Display(Name = "Randevu Tarih ve Saati")]
        public DateTime AppointmentDateTime => AppointmentDate.Date.Add(AppointmentTime);
    }
}


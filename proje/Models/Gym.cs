using System.ComponentModel.DataAnnotations;

namespace proje.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Spor salonu adı gereklidir.")]
        [StringLength(200, ErrorMessage = "Spor salonu adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Spor Salonu Adı")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "E-posta en fazla 100 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Açılış saati gereklidir.")]
        [Display(Name = "Açılış Saati")]
        public TimeSpan OpeningTime { get; set; }

        [Required(ErrorMessage = "Kapanış saati gereklidir.")]
        [Display(Name = "Kapanış Saati")]
        public TimeSpan ClosingTime { get; set; }

        [Display(Name = "Aktif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        public virtual ICollection<GymService> GymServices { get; set; } = new List<GymService>();
        public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}


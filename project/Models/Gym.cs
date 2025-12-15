using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [StringLength(50, ErrorMessage = "Çalışma günleri en fazla 50 karakter olabilir.")]
        [Display(Name = "Çalışma Günleri")]
        public string? WorkingDays { get; set; } // Virgülle ayrılmış gün numaraları (0=Pazar, 1=Pazartesi, ..., 6=Cumartesi) Örn: "1,2,3,4,5" = Pazartesi-Cuma

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [NotMapped]
        [Display(Name = "Çalışma Günleri Listesi")]
        public List<int> WorkingDaysList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(WorkingDays))
                    return new List<int>();
                return WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => int.TryParse(d.Trim(), out int day) ? day : -1)
                    .Where(d => d >= 0 && d <= 6)
                    .ToList();
            }
            set
            {
                WorkingDays = value != null && value.Any() 
                    ? string.Join(",", value.Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d))
                    : null;
            }
        }

        [NotMapped]
        [Display(Name = "Çalışma Günleri Metni")]
        public string WorkingDaysText
        {
            get
            {
                var days = WorkingDaysList;
                if (!days.Any())
                    return "Çalışma günü belirtilmemiş";

                var dayNames = new[] { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };
                return string.Join(", ", days.Select(d => dayNames[d]));
            }
        }

        // Navigation Properties
        public virtual ICollection<GymService> GymServices { get; set; } = new List<GymService>();
        public virtual ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
    }
}


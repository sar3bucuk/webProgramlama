using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proje.Models
{
    public class Message
    {
        public int Id { get; set; }

        // Sender can be either Member or Trainer (one must be set, other null)
        [Display(Name = "Gönderen Üye")]
        public int? SenderMemberId { get; set; }

        [Display(Name = "Gönderen Antrenör")]
        public int? SenderTrainerId { get; set; }

        // Receiver can be either Member or Trainer (one must be set, other null)
        [Display(Name = "Alıcı Üye")]
        public int? ReceiverMemberId { get; set; }

        [Display(Name = "Alıcı Antrenör")]
        public int? ReceiverTrainerId { get; set; }

        // Admin messaging support (IdentityUser ID as string)
        [Display(Name = "Gönderen Admin")]
        public string? SenderAdminUserId { get; set; }

        [Display(Name = "Alıcı Admin")]
        public string? ReceiverAdminUserId { get; set; }

        [Required(ErrorMessage = "Mesaj içeriği gereklidir.")]
        [StringLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
        [Display(Name = "Mesaj")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Okundu")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "Okunma Tarihi")]
        public DateTime? ReadDate { get; set; }

        [Display(Name = "Gönderilme Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Optional: Reply to a specific message to maintain conversation thread
        [Display(Name = "Yanıtlanan Mesaj")]
        public int? ReplyToMessageId { get; set; }

        // Navigation Properties
        [ForeignKey("SenderMemberId")]
        public virtual Member? SenderMember { get; set; }

        [ForeignKey("SenderTrainerId")]
        public virtual Trainer? SenderTrainer { get; set; }

        [ForeignKey("ReceiverMemberId")]
        public virtual Member? ReceiverMember { get; set; }

        [ForeignKey("ReceiverTrainerId")]
        public virtual Trainer? ReceiverTrainer { get; set; }

        [ForeignKey("ReplyToMessageId")]
        public virtual Message? ReplyToMessage { get; set; }
    }
}

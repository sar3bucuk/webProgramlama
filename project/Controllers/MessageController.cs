/*
Mesajlaşma controller'ı - Trainer ve Member rolleri için.
Trainer ve Member arasında mesajlaşma işlemlerini yönetir.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [Authorize(Roles = "Trainer,Member")]
    public class MessageController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public MessageController(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Mesajlaşma ana sayfası - kullanıcının tüm konuşmalarını listeler
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isTrainer = User.IsInRole("Trainer");
            var isMember = User.IsInRole("Member");

            var conversations = new List<ConversationViewModel>();

            if (isTrainer)
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                if (trainer == null)
                {
                    return NotFound();
                }

                var memberIds = await _context.Messages
                    .Where(m => (m.ReceiverTrainerId == trainer.Id && m.SenderMemberId != null) ||
                                (m.SenderTrainerId == trainer.Id && m.ReceiverMemberId != null))
                    .Select(m => m.SenderMemberId ?? m.ReceiverMemberId ?? 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToListAsync();

                foreach (var memberId in memberIds)
                {
                    var member = await _context.Members
                        .Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.Id == memberId);

                    if (member != null)
                    {
                        var lastMessage = await _context.Messages
                            .Where(m => (m.ReceiverTrainerId == trainer.Id && m.SenderMemberId == memberId) ||
                                        (m.SenderTrainerId == trainer.Id && m.ReceiverMemberId == memberId))
                            .Include(m => m.SenderMember)
                            .Include(m => m.SenderTrainer)
                            .OrderByDescending(m => m.CreatedDate)
                            .FirstOrDefaultAsync();

                        var unreadCount = await _context.Messages
                            .CountAsync(m => m.ReceiverTrainerId == trainer.Id && 
                                           m.SenderMemberId == memberId && 
                                           !m.IsRead);

                        var lastMessageSenderName = "";
                        var isLastMessageFromMe = false;
                        if (lastMessage != null)
                        {
                            if (lastMessage.SenderTrainerId == trainer.Id)
                            {
                                lastMessageSenderName = "Siz";
                                isLastMessageFromMe = true;
                            }
                            else if (lastMessage.SenderMemberId != null)
                            {
                                lastMessageSenderName = member.FullName;
                            }
                        }

                        conversations.Add(new ConversationViewModel
                        {
                            OtherUserId = member.Id,
                            OtherUserName = member.FullName,
                            OtherUserType = "Member",
                            LastMessage = lastMessage?.Content ?? "",
                            LastMessageDate = lastMessage?.CreatedDate ?? DateTime.MinValue,
                            UnreadCount = unreadCount,
                            LastMessageSenderName = lastMessageSenderName,
                            IsLastMessageFromMe = isLastMessageFromMe
                        });
                    }
                }
            }
            else if (isMember)
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                if (member == null)
                {
                    return RedirectToAction("Register", "Account");
                }

                var trainerIds = await _context.Messages
                    .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId != null) ||
                                (m.ReceiverMemberId == member.Id && m.SenderTrainerId != null))
                    .Select(m => m.ReceiverTrainerId ?? m.SenderTrainerId ?? 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToListAsync();

                foreach (var trainerId in trainerIds)
                {
                    var trainer = await _context.Trainers
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == trainerId);

                    if (trainer != null)
                    {
                        var lastMessage = await _context.Messages
                            .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId == trainerId) ||
                                        (m.ReceiverMemberId == member.Id && m.SenderTrainerId == trainerId))
                            .Include(m => m.SenderMember)
                            .Include(m => m.SenderTrainer)
                            .OrderByDescending(m => m.CreatedDate)
                            .FirstOrDefaultAsync();

                        var unreadCount = await _context.Messages
                            .CountAsync(m => m.ReceiverMemberId == member.Id && 
                                           m.SenderTrainerId == trainerId && 
                                           !m.IsRead);

                        var lastMessageSenderName = "";
                        var isLastMessageFromMe = false;
                        if (lastMessage != null)
                        {
                            if (lastMessage.SenderMemberId == member.Id)
                            {
                                lastMessageSenderName = "Siz";
                                isLastMessageFromMe = true;
                            }
                            else if (lastMessage.SenderTrainerId != null)
                            {
                                lastMessageSenderName = trainer.FullName;
                            }
                        }

                        conversations.Add(new ConversationViewModel
                        {
                            OtherUserId = trainer.Id,
                            OtherUserName = trainer.FullName,
                            OtherUserType = "Trainer",
                            LastMessage = lastMessage?.Content ?? "",
                            LastMessageDate = lastMessage?.CreatedDate ?? DateTime.MinValue,
                            UnreadCount = unreadCount,
                            LastMessageSenderName = lastMessageSenderName,
                            IsLastMessageFromMe = isLastMessageFromMe
                        });
                    }
                }
            }

            if (isMember)
            {
                ViewBag.Trainers = await _context.Trainers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.FirstName)
                    .ThenBy(t => t.LastName)
                    .ToListAsync();
            }

            if (isTrainer)
            {
                ViewBag.Members = await _context.Members
                    .OrderBy(m => m.FirstName)
                    .ThenBy(m => m.LastName)
                    .ToListAsync();
            }

            if (isTrainer)
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                ViewBag.CurrentUserName = trainer?.FullName ?? currentUser.UserName ?? "";
            }
            else if (isMember)
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                ViewBag.CurrentUserName = member?.FullName ?? currentUser.UserName ?? "";
            }

            return View(conversations.OrderByDescending(c => c.LastMessageDate).ToList());
        }

        /// <summary>
        /// Belirli bir kullanıcı ile konuşmayı gösterir
        /// </summary>
        public async Task<IActionResult> Conversation(int otherUserId, string otherUserType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var isTrainer = User.IsInRole("Trainer");
            var isMember = User.IsInRole("Member");

            if ((isTrainer && otherUserType != "Member") || (isMember && otherUserType != "Trainer"))
            {
                return BadRequest();
            }

            List<Message> messages;

            if (isTrainer)
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                if (trainer == null)
                {
                    return NotFound();
                }

                messages = await _context.Messages
                    .Where(m => (m.ReceiverTrainerId == trainer.Id && m.SenderMemberId == otherUserId) ||
                                (m.SenderTrainerId == trainer.Id && m.ReceiverMemberId == otherUserId))
                    .Include(m => m.SenderMember)
                    .Include(m => m.SenderTrainer)
                    .Include(m => m.ReceiverMember)
                    .Include(m => m.ReceiverTrainer)
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                var unreadMessages = messages.Where(m => m.ReceiverTrainerId == trainer.Id && !m.IsRead).ToList();
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadDate = DateTime.Now;
                }
                await _context.SaveChangesAsync();

                var member = await _context.Members
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == otherUserId);

                ViewBag.OtherUser = member;
                ViewBag.OtherUserType = "Member";
            }
            else 
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                if (member == null)
                {
                    return RedirectToAction("Register", "Account");
                }

                messages = await _context.Messages
                    .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId == otherUserId) ||
                                (m.ReceiverMemberId == member.Id && m.SenderTrainerId == otherUserId))
                    .Include(m => m.SenderMember)
                    .Include(m => m.SenderTrainer)
                    .Include(m => m.ReceiverMember)
                    .Include(m => m.ReceiverTrainer)
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                var unreadMessages = messages.Where(m => m.ReceiverMemberId == member.Id && !m.IsRead).ToList();
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.ReadDate = DateTime.Now;
                }
                await _context.SaveChangesAsync();

                var trainer = await _context.Trainers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == otherUserId);

                ViewBag.OtherUser = trainer;
                ViewBag.OtherUserType = "Trainer";
            }

            return View(messages);
        }

        /// <summary>
        /// Yeni mesaj gönderir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int otherUserId, string otherUserType, string content, int? replyToMessageId = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Mesaj içeriği boş olamaz." });
            }

            var isTrainer = User.IsInRole("Trainer");
            var isMember = User.IsInRole("Member");

            Message message = new Message
            {
                Content = content.Trim(),
                CreatedDate = DateTime.Now,
                IsRead = false,
                ReplyToMessageId = replyToMessageId
            };

            if (isTrainer && otherUserType == "Member")
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                if (trainer == null)
                {
                    return Json(new { success = false, message = "Antrenör bulunamadı." });
                }

                message.SenderTrainerId = trainer.Id;
                message.ReceiverMemberId = otherUserId;
            }
            else if (isMember && otherUserType == "Trainer")
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                if (member == null)
                {
                    return Json(new { success = false, message = "Üye bulunamadı." });
                }

                message.SenderMemberId = member.Id;
                message.ReceiverTrainerId = otherUserId;
            }
            else
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı türü." });
            }

            try
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var receiverUserId = isTrainer 
                    ? (await _context.Members.FirstOrDefaultAsync(m => m.Id == otherUserId))?.UserId
                    : (await _context.Trainers.FirstOrDefaultAsync(t => t.Id == otherUserId))?.UserId;

                if (!string.IsNullOrEmpty(receiverUserId))
                {
                    int? senderId = null;
                    string? senderType = null;
                    
                    if (isTrainer)
                    {
                        var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                        if (trainer != null)
                        {
                            senderId = trainer.Id;
                            senderType = "Trainer";
                        }
                    }
                    else if (isMember)
                    {
                        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                        if (member != null)
                        {
                            senderId = member.Id;
                            senderType = "Member";
                        }
                    }
                    
                    var notification = new Notification
                    {
                        UserId = receiverUserId,
                        Title = "Yeni Mesaj",
                        Message = $"Size yeni bir mesaj gönderildi.",
                        IsRead = false,
                        CreatedDate = DateTime.Now,
                        MessageId = message.Id,
                        SenderId = senderId,
                        SenderType = senderType
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Mesaj başarıyla gönderildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Mesaj gönderilirken hata oluştu: {ex.Message}" });
            }
        }

        /// <summary>
        /// Member'in tüm konuşmalarını JSON olarak döner
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return Json(new { success = false, message = "Üye bulunamadı." });
            }

            var conversations = new List<object>();

            var trainerIds = await _context.Messages
                .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId != null) ||
                            (m.ReceiverMemberId == member.Id && m.SenderTrainerId != null))
                .Select(m => m.ReceiverTrainerId ?? m.SenderTrainerId ?? 0)
                .Where(id => id > 0)
                .Distinct()
                .ToListAsync();

            foreach (var trainerId in trainerIds)
            {
                var trainer = await _context.Trainers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == trainerId);

                if (trainer != null)
                {
                    var lastMessage = await _context.Messages
                        .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId == trainerId) ||
                                    (m.ReceiverMemberId == member.Id && m.SenderTrainerId == trainerId))
                        .Include(m => m.SenderMember)
                        .Include(m => m.SenderTrainer)
                        .OrderByDescending(m => m.CreatedDate)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _context.Messages
                        .CountAsync(m => m.ReceiverMemberId == member.Id && 
                                       m.SenderTrainerId == trainerId && 
                                       !m.IsRead);

                    var lastMessageSenderName = "";
                    var isLastMessageFromMe = false;
                    if (lastMessage != null)
                    {
                        if (lastMessage.SenderMemberId == member.Id)
                        {
                            lastMessageSenderName = "Siz";
                            isLastMessageFromMe = true;
                        }
                        else if (lastMessage.SenderTrainerId != null)
                        {
                            lastMessageSenderName = trainer.FullName;
                        }
                    }

                    conversations.Add(new
                    {
                        otherUserId = trainer.Id,
                        otherUserName = trainer.FullName,
                        otherUserType = "Trainer",
                        lastMessage = lastMessage?.Content ?? "",
                        lastMessageDate = lastMessage?.CreatedDate ?? DateTime.MinValue,
                        unreadCount = unreadCount,
                        lastMessageSenderName = lastMessageSenderName,
                        isLastMessageFromMe = isLastMessageFromMe
                    });
                }
            }

            return Json(new { success = true, conversations = conversations.OrderByDescending(c => ((dynamic)c).lastMessageDate) });
        }

        /// <summary>
        /// Belirli bir trainer veya member ile konuşmayı JSON olarak döner
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConversationMessages(int? trainerId, int? memberId, string? userType)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var isTrainer = User.IsInRole("Trainer");
            var isMember = User.IsInRole("Member");

            List<object> messages;
            object? otherUser = null;

            if (isMember)
            {
                var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
                if (member == null)
                {
                    return Json(new { success = false, message = "Üye bulunamadı." });
                }

                if (trainerId.HasValue)
                {
                    var trainer = await _context.Trainers
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.Id == trainerId.Value);

                    if (trainer == null)
                    {
                        return Json(new { success = false, message = "Antrenör bulunamadı." });
                    }

                    var messagesList = await _context.Messages
                        .Where(m => (m.SenderMemberId == member.Id && m.ReceiverTrainerId == trainerId.Value) ||
                                    (m.ReceiverMemberId == member.Id && m.SenderTrainerId == trainerId.Value))
                        .OrderBy(m => m.CreatedDate)
                        .Select(m => new
                        {
                            id = m.Id,
                            content = m.Content,
                            createdDate = m.CreatedDate,
                            isRead = m.IsRead,
                            isSent = m.SenderMemberId == member.Id
                        })
                        .ToListAsync();
                    
                    messages = messagesList.Cast<object>().ToList();

                    var unreadMessages = await _context.Messages
                        .Where(m => m.ReceiverMemberId == member.Id && 
                                   m.SenderTrainerId == trainerId.Value && 
                                   !m.IsRead)
                        .ToListAsync();

                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.ReadDate = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();

                    otherUser = new { id = trainer.Id, name = trainer.FullName, type = "Trainer" };
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz parametreler." });
                }
            }
            else if (isTrainer)
            {
                var trainer = await _context.Trainers.FirstOrDefaultAsync(t => t.UserId == currentUser.Id);
                if (trainer == null)
                {
                    return Json(new { success = false, message = "Antrenör bulunamadı." });
                }

                if (memberId.HasValue)
                {
                    var member = await _context.Members
                        .Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.Id == memberId.Value);

                    if (member == null)
                    {
                        return Json(new { success = false, message = "Üye bulunamadı." });
                    }

                    var messagesList = await _context.Messages
                        .Where(m => (m.ReceiverTrainerId == trainer.Id && m.SenderMemberId == memberId.Value) ||
                                    (m.SenderTrainerId == trainer.Id && m.ReceiverMemberId == memberId.Value))
                        .OrderBy(m => m.CreatedDate)
                        .Select(m => new
                        {
                            id = m.Id,
                            content = m.Content,
                            createdDate = m.CreatedDate,
                            isRead = m.IsRead,
                            isSent = m.SenderTrainerId == trainer.Id
                        })
                        .ToListAsync();
                    
                    messages = messagesList.Cast<object>().ToList();

                    var unreadMessages = await _context.Messages
                        .Where(m => m.ReceiverTrainerId == trainer.Id && 
                                   m.SenderMemberId == memberId.Value && 
                                   !m.IsRead)
                        .ToListAsync();

                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        msg.ReadDate = DateTime.Now;
                    }
                    await _context.SaveChangesAsync();

                    otherUser = new { id = member.Id, name = member.FullName, type = "Member" };
                }
                else
                {
                    return Json(new { success = false, message = "Geçersiz parametreler." });
                }
            }
            else
            {
                return Json(new { success = false, message = "Yetkisiz erişim." });
            }

            return Json(new { 
                success = true, 
                messages = messages,
                otherUser = otherUser
            });
        }

        /// <summary>
        /// Okunmamış mesaj sayısını döner (Member için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadMessageCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, count = 0 });
            }

            var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);
            if (member == null)
            {
                return Json(new { success = false, count = 0 });
            }

            var count = await _context.Messages
                .CountAsync(m => m.ReceiverMemberId == member.Id && !m.IsRead);

            return Json(new { success = true, count = count });
        }

        /// <summary>
        /// Yeni konuşma başlatmak için aktif antrenörleri JSON olarak döner (Member için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAvailableTrainers()
        {
            var trainers = await _context.Trainers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FirstName)
                .ThenBy(t => t.LastName)
                .Select(t => new { id = t.Id, name = t.FullName })
                .ToListAsync();

            return Json(new { success = true, trainers = trainers });
        }
    }

    /// <summary>
    /// Konuşma listesi için view model
    /// </summary>
    public class ConversationViewModel
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserType { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
        public string LastMessageSenderName { get; set; } = string.Empty;
        public bool IsLastMessageFromMe { get; set; }
    }
}

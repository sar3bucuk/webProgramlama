/*
Bildirim işlemleri için REST API controller'ı.
Kullanıcı bildirimlerini getirme, okunmamış sayısını alma, bildirimi okundu işaretleme işlemlerini yönetir.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using System.Security.Claims;

namespace proje.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationsApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Kullanıcının bildirimlerini LINQ sorguları ile getirir
        /// </summary>
        /// <returns>Bildirim listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id)           
                .OrderByDescending(n => n.CreatedDate)            
                .Take(10)                                        
                .Select(n => new                               
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    isRead = n.IsRead,
                    createdDate = n.CreatedDate,
                    appointmentId = n.AppointmentId,
                    messageId = n.MessageId,
                    senderId = n.SenderId,
                    senderType = n.SenderType,
                    receiverId = n.ReceiverId,
                    receiverType = n.ReceiverType
                })
                .ToListAsync();

            return Ok(new { success = true, notifications = notifications });
        }

        /// <summary>
        /// Okunmamış bildirim sayısını LINQ Count ile getirir
        /// </summary>
        /// <returns>Okunmamış bildirim sayısı</returns>
        [HttpGet]
        [Route("unread-count")]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, count = 0 });
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.Id && !n.IsRead); 

            return Ok(new { success = true, count = count });
        }

        /// <summary>
        /// Bildirimi okundu olarak işaretler - LINQ FirstOrDefault kullanımı
        /// </summary>
        /// <param name="id">Bildirim ID</param>
        /// <returns>İşlem sonucu</returns>
        [HttpPost]
        [Route("mark-read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);  

            if (notification == null)
            {
                return NotFound(new { success = false, message = "Bildirim bulunamadı." });
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        /// <summary>
        /// Tüm bildirimleri okundu olarak işaretler - LINQ Where kullanımı
        /// </summary>
        /// <returns>İşlem sonucu</returns>
        [HttpPost]
        [Route("mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.Id && !n.IsRead)  // LINQ Where
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Randevuları LINQ sorguları ile filtreler
        /// </summary>
        /// <param name="status">Randevu durumu (Pending, Approved, Rejected, Completed, Cancelled)</param>
        /// <param name="trainerId">Antrenör ID'si</param>
        /// <param name="memberId">Üye ID'si</param>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <param name="minPrice">Minimum ücret</param>
        /// <param name="maxPrice">Maksimum ücret</param>
        /// <param name="gymId">Spor salonu ID'si</param>
        /// <returns>Filtrelenmiş randevu listesi</returns>
        [HttpGet]
        [Route("filter")]
        [AllowAnonymous]
        public async Task<IActionResult> FilterAppointments(
            [FromQuery] string? status = null,
            [FromQuery] int? trainerId = null,
            [FromQuery] int? memberId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int? gymId = null)
        {
            try
            {
                // LINQ sorgusu ile filtreleme başlatılıyor
                var query = _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                        .ThenInclude(t => t.Gym)
                    .Include(a => a.GymService)
                        .ThenInclude(gs => gs.Service)
                    .AsQueryable();

                // Status filtresi - LINQ Where kullanımı
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status == status);
                }

                // Trainer ID filtresi - LINQ Where kullanımı
                if (trainerId.HasValue)
                {
                    query = query.Where(a => a.TrainerId == trainerId.Value);
                }

                // Member ID filtresi - LINQ Where kullanımı
                if (memberId.HasValue)
                {
                    query = query.Where(a => a.MemberId == memberId.Value);
                }

                // Tarih aralığı filtresi - LINQ Where kullanımı
                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate <= endDate.Value);
                }

                // Fiyat aralığı filtresi - LINQ Where kullanımı
                if (minPrice.HasValue)
                {
                    query = query.Where(a => a.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(a => a.Price <= maxPrice.Value);
                }

                // Spor salonu filtresi - LINQ Where ile ilişkili tablo filtreleme
                if (gymId.HasValue)
                {
                    query = query.Where(a => a.Trainer != null && a.Trainer.GymId == gymId.Value);
                }

                // LINQ OrderBy ve Select ile sonuçlar hazırlanıyor
                var appointments = await query
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenByDescending(a => a.AppointmentTime)
                    .Select(a => new
                    {
                        id = a.Id,
                        memberId = a.MemberId,
                        memberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : "Bilinmiyor",
                        trainerId = a.TrainerId,
                        trainerName = a.Trainer != null ? $"{a.Trainer.FirstName} {a.Trainer.LastName}" : "Bilinmiyor",
                        gymName = a.Trainer != null && a.Trainer.Gym != null ? a.Trainer.Gym.Name : "Bilinmiyor",
                        serviceName = a.GymService != null && a.GymService.Service != null ? a.GymService.Service.Name : "Bilinmiyor",
                        appointmentDate = a.AppointmentDate,
                        appointmentTime = a.AppointmentTime,
                        duration = a.Duration,
                        price = a.Price,
                        status = a.Status,
                        notes = a.Notes,
                        createdDate = a.CreatedDate
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = appointments.Count,
                    appointments = appointments
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Randevular filtrelenirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Tarihe göre randevuları gruplar (LINQ GroupBy kullanımı)
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Tarihe göre gruplandırılmış randevular</returns>
        [HttpGet]
        [Route("group-by-date")]
        [AllowAnonymous]
        public async Task<IActionResult> GroupAppointmentsByDate(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var query = _context.Appointments
                    .Include(a => a.Member)
                    .Include(a => a.Trainer)
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate <= endDate.Value);
                }

                // LINQ GroupBy kullanımı
                var groupedAppointments = await query
                    .GroupBy(a => a.AppointmentDate)
                    .Select(g => new
                    {
                        date = g.Key,
                        count = g.Count(),
                        appointments = g.Select(a => new
                        {
                            id = a.Id,
                            memberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : "Bilinmiyor",
                            trainerName = a.Trainer != null ? $"{a.Trainer.FirstName} {a.Trainer.LastName}" : "Bilinmiyor",
                            appointmentTime = a.AppointmentTime,
                            status = a.Status,
                            price = a.Price
                        }).ToList()
                    })
                    .OrderBy(g => g.date)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    groupedAppointments = groupedAppointments
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Randevular gruplandırılırken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Duruma göre randevu istatistikleri (LINQ GroupBy ve Count kullanımı)
        /// </summary>
        /// <returns>Durum bazlı istatistikler</returns>
        [HttpGet]
        [Route("statistics-by-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatisticsByStatus()
        {
            try
            {
                // LINQ GroupBy ve Count kullanımı
                var statistics = await _context.Appointments
                    .GroupBy(a => a.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count(),
                        totalRevenue = g.Sum(a => a.Price),
                        averagePrice = g.Average(a => a.Price)
                    })
                    .ToListAsync();

                var totalAppointments = await _context.Appointments.CountAsync();
                var totalRevenue = await _context.Appointments.SumAsync(a => a.Price);

                return Ok(new
                {
                    success = true,
                    totalAppointments = totalAppointments,
                    totalRevenue = totalRevenue,
                    statisticsByStatus = statistics
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "İstatistikler hesaplanırken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Antrenöre göre randevu sayıları (LINQ ile ilişkili tablo filtreleme)
        /// </summary>
        /// <param name="minAppointmentCount">Minimum randevu sayısı</param>
        /// <returns>Antrenör bazlı randevu sayıları</returns>
        [HttpGet]
        [Route("trainer-statistics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrainerStatistics([FromQuery] int minAppointmentCount = 0)
        {
            try
            {
                // LINQ ile ilişkili tablolar üzerinde filtreleme ve gruplama
                var trainerStats = await _context.Trainers
                    .Where(t => t.Appointments.Count >= minAppointmentCount)
                    .Select(t => new
                    {
                        trainerId = t.Id,
                        trainerName = $"{t.FirstName} {t.LastName}",
                        gymName = t.Gym != null ? t.Gym.Name : "Bağlı değil",
                        totalAppointments = t.Appointments.Count,
                        pendingAppointments = t.Appointments.Count(a => a.Status == "Pending"),
                        approvedAppointments = t.Appointments.Count(a => a.Status == "Approved"),
                        completedAppointments = t.Appointments.Count(a => a.Status == "Completed"),
                        totalRevenue = t.Appointments.Sum(a => a.Price)
                    })
                    .OrderByDescending(t => t.totalAppointments)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    trainerStatistics = trainerStats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Antrenör istatistikleri hesaplanırken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }
    }
}

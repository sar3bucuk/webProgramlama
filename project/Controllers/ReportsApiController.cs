/*
Raporlama için REST API controller'ı.
Antrenör, üye ve randevu bilgilerini LINQ sorguları ile filtreleyerek raporlama verileri sağlar.
Tüm antrenörleri listeleme, belirli tarihte uygun antrenörleri getirme, üye randevularını getirme gibi işlemleri yapar.
*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proje.Data;
using proje.Models;

namespace proje.Controllers
{
    /// <summary>
    /// Raporlama API Controller - REST API kullanarak veritabanı ile iletişim ve LINQ sorguları ile filtreleme
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm antrenörleri LINQ sorguları ile listeler
        /// </summary>
        /// <param name="gymId">Spor salonu ID'sine göre filtreleme (opsiyonel)</param>
        /// <param name="isActive">Aktif antrenörlere göre filtreleme (opsiyonel)</param>
        /// <param name="experienceYears">Minimum deneyim yılına göre filtreleme (opsiyonel)</param>
        /// <returns>Antrenör listesi</returns>
        [HttpGet]
        [Route("trainers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTrainers(
            [FromQuery] int? gymId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int? experienceYears = null)
        {
            try
            {
                var query = _context.Trainers
                    .Include(t => t.Gym)                              
                    .Include(t => t.TrainerServices)                  
                        .ThenInclude(ts => ts.Service)                 
                    .Include(t => t.TrainerAvailabilities)             
                    .AsQueryable();                                    

                if (gymId.HasValue)
                {
                    query = query.Where(t => t.GymId == gymId.Value);  
                }

                if (isActive.HasValue)
                {
                    query = query.Where(t => t.IsActive == isActive.Value);  
                }

                if (experienceYears.HasValue)
                {
                    query = query.Where(t => t.ExperienceYears >= experienceYears.Value);  
                }

                var trainers = await query
                    .OrderBy(t => t.FirstName)                         
                    .ThenBy(t => t.LastName)                           
                    .Select(t => new                                   
                    {
                        id = t.Id,
                        firstName = t.FirstName,
                        lastName = t.LastName,
                        fullName = t.FirstName + " " + t.LastName,
                        email = t.Email,
                        phone = t.Phone,
                        bio = t.Bio,
                        experienceYears = t.ExperienceYears,
                        isActive = t.IsActive,
                        gymId = t.GymId,
                        gymName = t.Gym != null ? t.Gym.Name : "Bağlı değil",
                        services = t.TrainerServices.Select(ts => new  
                        {
                            id = ts.Service.Id,
                            name = ts.Service.Name
                        }).ToList(),
                        availabilityCount = t.TrainerAvailabilities.Count(ta => ta.IsAvailable),  
                        totalAppointments = t.Appointments.Count      
                    })
                    .ToListAsync();                                    

                return Ok(new
                {
                    success = true,
                    count = trainers.Count,
                    trainers = trainers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Antrenörler listelenirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Üye randevularını LINQ sorguları ile getirir
        /// </summary>
        /// <param name="memberId">Üye ID'si</param>
        /// <param name="status">Randevu durumu (Pending, Approved, Rejected, Completed, Cancelled) - opsiyonel</param>
        /// <param name="startDate">Başlangıç tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitiş tarihi (opsiyonel)</param>
        /// <param name="trainerId">Antrenör ID'si (opsiyonel)</param>
        /// <returns>Üye randevuları listesi</returns>
        [HttpGet]
        [Route("member-appointments/{memberId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMemberAppointments(
            int memberId,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? trainerId = null)
        {
            try
            {
                var memberExists = await _context.Members
                    .AnyAsync(m => m.Id == memberId);                  

                if (!memberExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Üye bulunamadı."
                    });
                }

                var query = _context.Appointments
                    .Include(a => a.Member)                            
                        .ThenInclude(m => m.User)                      
                    .Include(a => a.Trainer)                           
                        .ThenInclude(t => t.Gym)                       
                    .Include(a => a.GymService)                        
                        .ThenInclude(gs => gs.Service)                 
                    .Where(a => a.MemberId == memberId)                
                    .AsQueryable();                                    

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(a => a.Status == status);      
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate >= startDate.Value);  
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDate <= endDate.Value);    
                }

                if (trainerId.HasValue)
                {
                    query = query.Where(a => a.TrainerId == trainerId.Value);        
                }

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
                        appointmentTime = a.AppointmentTime.ToString(@"hh\:mm"),
                        duration = a.Duration,
                        price = a.Price,
                        status = a.Status,
                        notes = a.Notes,
                        createdDate = a.CreatedDate,
                        updatedDate = a.UpdatedDate
                    })
                    .ToListAsync();                                    

                var statistics = new
                {
                    totalCount = appointments.Count,
                    pendingCount = appointments.Count(a => a.status == "Pending"),         
                    approvedCount = appointments.Count(a => a.status == "Approved"),       
                    completedCount = appointments.Count(a => a.status == "Completed"),     
                    cancelledCount = appointments.Count(a => a.status == "Cancelled"),     
                    totalPrice = appointments.Sum(a => a.price),                          
                    averagePrice = appointments.Any() ? appointments.Average(a => a.price) : 0  
                };

                return Ok(new
                {
                    success = true,
                    memberId = memberId,
                    statistics = statistics,
                    appointments = appointments
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Üye randevuları getirilirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Tüm üyeleri LINQ sorguları ile listeler (randevu istatistikleri ile birlikte)
        /// </summary>
        /// <param name="gymId">Spor salonu ID'sine göre filtreleme (opsiyonel)</param>
        /// <returns>Üye listesi</returns>
        [HttpGet]
        [Route("members")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMembers([FromQuery] int? gymId = null)
        {
            try
            {
                var query = _context.Members
                    .Include(m => m.Gym)                               
                    .Include(m => m.User)                              
                    .Include(m => m.Appointments)                      
                    .AsQueryable();                                    

                if (gymId.HasValue)
                {
                    query = query.Where(m => m.GymId == gymId.Value);  
                }

                var members = await query
                    .OrderBy(m => m.FirstName)                         
                    .ThenBy(m => m.LastName)                           
                    .Select(m => new                                   
                    {
                        id = m.Id,
                        firstName = m.FirstName,
                        lastName = m.LastName,
                        fullName = m.FirstName + " " + m.LastName,
                        phone = m.Phone,
                        email = m.User != null ? m.User.Email : null,
                        gymId = m.GymId,
                        gymName = m.Gym != null ? m.Gym.Name : "Bağlı değil",
                        totalAppointments = m.Appointments.Count,      
                        pendingAppointments = m.Appointments.Count(a => a.Status == "Pending"),      
                        approvedAppointments = m.Appointments.Count(a => a.Status == "Approved"),    
                        completedAppointments = m.Appointments.Count(a => a.Status == "Completed"),  
                        totalSpent = m.Appointments.Where(a => a.Status == "Completed").Sum(a => a.Price)  
                    })
                    .ToListAsync();                                    

                return Ok(new
                {
                    success = true,
                    count = members.Count,
                    members = members
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Üyeler listelenirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }
    }
}


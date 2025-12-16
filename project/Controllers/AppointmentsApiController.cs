/*
Randevu işlemleri için REST API controller'ı.
AppointmentController ile birlikte çalışır - bu controller frontend'e JSON veri sağlar.
Randevu oluşturma formunda dinamik dropdown'ları doldurmak için kullanılır 
(spor salonu seçildiğinde hizmetler, tarih/saat seçildiğinde müsait antrenörler).
LINQ sorguları ile filtreleme ve veri getirme işlemleri yapar.
*/
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
        /// Belirli tarih, saat ve süre için müsait antrenörleri LINQ sorguları ile getirir
        /// </summary>
        /// <param name="gymId">Spor salonu ID</param>
        /// <param name="gymServiceId">Spor salonu hizmet ID</param>
        /// <param name="appointmentDate">Randevu tarihi</param>
        /// <param name="appointmentTime">Randevu saati (HH:mm formatında)</param>
        /// <param name="duration">Süre (dakika)</param>
        /// <returns>Müsait antrenörler listesi</returns>
        [HttpGet]
        [Route("available-trainers")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableTrainers(
            [FromQuery] int gymId,
            [FromQuery] int gymServiceId,
            [FromQuery] DateTime appointmentDate,
            [FromQuery] string appointmentTime,
            [FromQuery] int duration)
        {
            try
            {
                if (!TimeSpan.TryParse(appointmentTime, out var timeSpan))
                {
                    return BadRequest(new { success = false, message = "Geçersiz saat formatı." });
                }

                var gymService = await _context.GymServices
                    .Include(gs => gs.Gym)
                    .FirstOrDefaultAsync(gs => gs.Id == gymServiceId);

                if (gymService == null)
                {
                    return NotFound(new { success = false, message = "Hizmet bulunamadı." });
                }

                // Spor salonunun çalışma günlerini kontrol et
                var gym = gymService.Gym;
                if (gym != null && !string.IsNullOrWhiteSpace(gym.WorkingDays))
                {
                    int dayOfWeekForGym = (int)appointmentDate.DayOfWeek;
                    var workingDays = gym.WorkingDaysList;
                    
                    if (!workingDays.Contains(dayOfWeekForGym))
                    {
                        return Ok(new { success = true, trainers = new List<object>() });
                    }
                }

                // Spor salonunun açılış/kapanış saatlerini kontrol et
                int dayOfWeek = (int)appointmentDate.DayOfWeek;
                var endTime = timeSpan.Add(TimeSpan.FromMinutes(duration));
                
                if (gym != null && (timeSpan < gym.OpeningTime || endTime > gym.ClosingTime))
                {
                    return Ok(new { success = true, trainers = new List<object>() });
                }

                var availableTrainers = await _context.Trainers
                    .Include(t => t.Gym)                             
                    .Include(t => t.TrainerAvailabilities)           
                    .Include(t => t.TrainerServices)                 
                    .Where(t =>                                     
                        t.IsActive &&
                        t.GymId == gymId &&
                        t.TrainerServices.Any(ts => ts.ServiceId == gymService.ServiceId) &&  // LINQ Any
                        t.TrainerAvailabilities.Any(ta =>            
                            ta.DayOfWeek == dayOfWeek &&
                            ta.IsAvailable &&
                            ta.StartTime <= timeSpan &&
                            ta.EndTime >= endTime))
                    .Select(t => new                                 
                    {
                        id = t.Id,
                        name = t.FirstName + " " + t.LastName,
                        experience = t.ExperienceYears,
                        bio = t.Bio ?? ""
                    })
                    .ToListAsync();

                var conflictingAppointments = await _context.Appointments
                    .Where(a =>                                     
                        a.AppointmentDate == appointmentDate &&
                        a.Status != "Cancelled" &&
                        a.Status != "Rejected")
                    .ToListAsync();

                var finalTrainers = availableTrainers.Where(t =>    
                {
                    var trainerConflicts = conflictingAppointments
                        .Where(a => a.TrainerId == t.id &&         
                            (
                                (timeSpan >= a.AppointmentTime && timeSpan < a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration))) ||
                                (endTime > a.AppointmentTime && endTime <= a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration))) ||
                                (timeSpan <= a.AppointmentTime && endTime >= a.AppointmentTime.Add(TimeSpan.FromMinutes(a.Duration)))
                            ))
                        .Any();                                     
                    return !trainerConflicts;
                }).ToList();

                return Ok(new { success = true, trainers = finalTrainers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Müsait antrenörler getirilirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Spor salonuna ait aktif hizmetleri LINQ sorguları ile getirir
        /// </summary>
        /// <param name="gymId">Spor salonu ID</param>
        /// <returns>Hizmet listesi</returns>
        [HttpGet]
        [Route("gym-services/{gymId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetGymServices(int gymId)
        {
            try
            {
                var services = await _context.GymServices
                    .Include(gs => gs.Service)                       
                    .Where(gs => gs.GymId == gymId && gs.IsActive)   
                    .Select(gs => new                                
                    {
                        id = gs.Id,
                        name = gs.Service.Name,
                        duration = gs.Duration,
                        price = gs.Price
                    })
                    .ToListAsync();

                return Ok(new { success = true, services = services });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Hizmetler getirilirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }
    }
}

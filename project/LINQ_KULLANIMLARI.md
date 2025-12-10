# Projedeki LINQ Kullanımları - Detaylı Liste

## 📁 1. AccountController.cs

### GetNotifications() - Satır 33-46
```csharp
var notifications = await _context.Notifications
    .Where(n => n.UserId == currentUser.Id)           // LINQ Where
    .OrderByDescending(n => n.CreatedDate)            // LINQ OrderByDescending
    .Take(10)                                         // LINQ Take
    .Select(n => new { ... })                         // LINQ Select
    .ToListAsync();
```

### GetUnreadNotificationCount() - Satır 61-62
```csharp
var count = await _context.Notifications
    .CountAsync(n => n.UserId == currentUser.Id && !n.IsRead);  // LINQ Count
```

### MarkNotificationAsRead() - Satır 78-79
```csharp
var notification = await _context.Notifications
    .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.Id);  // LINQ FirstOrDefault
```

### MarkAllNotificationsAsRead() - Satır 103-104
```csharp
var notifications = await _context.Notifications
    .Where(n => n.UserId == currentUser.Id && !n.IsRead)  // LINQ Where
    .ToListAsync();
```

### Register() - Satır 199, 208
```csharp
var existingTrainer = await _context.Trainers.FirstOrDefaultAsync(...);  // LINQ FirstOrDefault
var existingMember = await _context.Members.FirstOrDefaultAsync(...);    // LINQ FirstOrDefault
```

---

## 📁 2. AppointmentController.cs

### Create GET - Satır 43-44, 53
```csharp
var member = await _context.Members
    .Include(m => m.Gym)                             // LINQ Include
    .FirstOrDefaultAsync(m => m.UserId == currentUser.Id);  // LINQ FirstOrDefault

ViewBag.Services = await _context.Services
    .Where(s => s.IsActive)                          // LINQ Where
    .OrderBy(s => s.Name)                            // LINQ OrderBy
    .ToListAsync();
```

### Create POST - Satır 172-173, 227-229, 253-254, 270, 291, 300, 326
```csharp
// Çok sayıda LINQ kullanımı:
.Include(m => m.Gym)                                 // LINQ Include
.FirstOrDefaultAsync(m => m.UserId == currentUser.Id);

.Include(gs => gs.Service)                           // LINQ Include
.Include(gs => gs.Gym)                               // LINQ Include
.FirstOrDefaultAsync(gs => gs.Id == appointment.GymServiceId);

.FirstOrDefault(a => a.DayOfWeek == dayOfWeek && a.IsAvailable);  // LINQ FirstOrDefault

.Where(a => ...)                                     // LINQ Where (çoklu)
.FirstOrDefault(a => ...);

var errors = ModelState.Values
    .SelectMany(v => v.Errors)                       // LINQ SelectMany
    .Select(e => e.ErrorMessage);                    // LINQ Select
```

### MyAppointments() - Satır 413-424, 426-435
```csharp
var activeAppointments = await _context.Appointments
    .Include(a => a.Trainer)                         // LINQ Include
        .ThenInclude(t => t.Gym)                     // LINQ ThenInclude
    .Include(a => a.GymService)                      // LINQ Include
        .ThenInclude(gs => gs.Service)               // LINQ ThenInclude
    .Where(a => a.MemberId == member.Id &&           // LINQ Where
               a.AppointmentDate >= today && 
               a.Status != "Completed" && 
               a.Status != "Cancelled")
    .OrderByDescending(a => a.AppointmentDate)       // LINQ OrderByDescending
    .ThenByDescending(a => a.AppointmentTime)        // LINQ ThenByDescending
    .ToListAsync();
```

### GetAvailableTrainers() - Satır 500-516, 519-524, 526-537
```csharp
var availableTrainers = await _context.Trainers
    .Include(t => t.Gym)                             // LINQ Include
    .Include(t => t.TrainerAvailabilities)           // LINQ Include
    .Include(t => t.TrainerServices)                 // LINQ Include
    .Where(t => 
        t.IsActive &&
        t.GymId == gymId &&
        t.TrainerServices.Any(ts => ts.ServiceId == gymService.ServiceId) &&  // LINQ Any
        t.TrainerAvailabilities.Any(ta => ...))      // LINQ Any
    .Select(t => new { ... })                        // LINQ Select
    .ToListAsync();

var conflictingAppointments = await _context.Appointments
    .Where(a => 
        a.AppointmentDate == appointmentDate &&
        a.Status != "Cancelled" &&
        a.Status != "Rejected")                      // LINQ Where
    .ToListAsync();

var finalTrainers = availableTrainers
    .Where(t => { ... })                             // LINQ Where (memory'de)
    .ToList();
```

### GetGymServices() - Satır 545-548
```csharp
var services = await _context.GymServices
    .Include(gs => gs.Service)                       // LINQ Include
    .Where(gs => gs.GymId == gymId && gs.IsActive)   // LINQ Where
    .Select(gs => new { ... })                       // LINQ Select
    .ToListAsync();
```

---

## 📁 3. AdminController.cs

### Gyms() - Satır 43-50
```csharp
var gyms = await _context.Gyms
    .OrderBy(g => g.Name)                            // LINQ OrderBy
    .Select(g => new {
        ...
        MemberCount = _context.Members.Count(m => m.GymId == g.Id),      // LINQ Count
        TrainerCount = _context.Trainers.Count(t => t.GymId == g.Id && t.IsActive)  // LINQ Count
    })
    .ToListAsync();
```

### Members() - Satır 300-322
```csharp
var query = _context.Members
    .Include(m => m.User)                            // LINQ Include
    .Include(m => m.Gym)                             // LINQ Include
    .AsQueryable();

if (!string.IsNullOrWhiteSpace(searchName))
{
    query = query.Where(m =>                         // LINQ Where
        (m.FirstName + " " + m.LastName).Contains(searchName) ||
        m.FirstName.Contains(searchName) ||
        m.LastName.Contains(searchName));
}

if (gymId.HasValue && gymId.Value > 0)
{
    query = query.Where(m => m.GymId == gymId.Value);  // LINQ Where
}

var members = await query
    .OrderBy(m => m.FirstName)                       // LINQ OrderBy
    .ThenBy(m => m.LastName)                         // LINQ ThenBy
    .ToListAsync();
```

### Appointments() - Satır 748-772
```csharp
var activeAppointments = await _context.Appointments
    .Include(a => a.Member)                          // LINQ Include
        .ThenInclude(m => m.User)                    // LINQ ThenInclude
    .Include(a => a.Trainer)                         // LINQ Include
        .ThenInclude(t => t.Gym)                     // LINQ ThenInclude
    .Include(a => a.GymService)                      // LINQ Include
        .ThenInclude(gs => gs.Service)               // LINQ ThenInclude
    .Where(a => a.AppointmentDate >= today &&        // LINQ Where
               a.Status != "Completed" && 
               a.Status != "Cancelled")
    .OrderByDescending(a => a.AppointmentDate)       // LINQ OrderByDescending
    .ThenByDescending(a => a.AppointmentTime)        // LINQ ThenByDescending
    .ToListAsync();
```

### Çok sayıda ViewBag kullanımı:
```csharp
ViewBag.Services = await _context.Services
    .Where(s => s.IsActive)                          // LINQ Where
    .OrderBy(s => s.Name)                            // LINQ OrderBy
    .ToListAsync();

ViewBag.Gyms = await _context.Gyms
    .Where(g => g.IsActive)                          // LINQ Where
    .OrderBy(g => g.Name)                            // LINQ OrderBy
    .ToListAsync();
```

---

## 📁 4. TrainerController.cs

### Index() - Satır 46-50
```csharp
var trainer = await _context.Trainers
    .Include(t => t.User)                            // LINQ Include
    .Include(t => t.Gym)                             // LINQ Include
    .Include(t => t.TrainerServices)                 // LINQ Include
    .FirstOrDefaultAsync(t => t.UserId == currentUser.Id);  // LINQ FirstOrDefault
```

### Appointments() - Satır 155-181
```csharp
var activeAppointments = await _context.Appointments
    .Include(a => a.Member)                          // LINQ Include
        .ThenInclude(m => m.User)                    // LINQ ThenInclude
    .Include(a => a.GymService)                      // LINQ Include
        .ThenInclude(gs => gs.Service)               // LINQ ThenInclude
    .Include(a => a.GymService)                      // LINQ Include
        .ThenInclude(gs => gs.Gym)                   // LINQ ThenInclude
    .Where(a => a.TrainerId == trainer.Id &&         // LINQ Where
               a.AppointmentDate >= today && 
               a.Status != "Completed" && 
               a.Status != "Cancelled")
    .OrderByDescending(a => a.AppointmentDate)       // LINQ OrderByDescending
    .ThenByDescending(a => a.AppointmentTime)        // LINQ ThenByDescending
    .ToListAsync();
```

---

## 📊 Kullanılan LINQ Metodları Özeti:

### Filtreleme:
- ✅ `Where()` - Çok sayıda yerde kullanılıyor
- ✅ `FirstOrDefault()` / `FirstOrDefaultAsync()` - Çok sayıda yerde

### Sıralama:
- ✅ `OrderBy()` - Çok sayıda yerde
- ✅ `OrderByDescending()` - Çok sayıda yerde
- ✅ `ThenBy()` - Birkaç yerde
- ✅ `ThenByDescending()` - Çok sayıda yerde

### Projeksiyon:
- ✅ `Select()` - Birkaç yerde
- ✅ `SelectMany()` - Birkaç yerde

### Kısıtlama:
- ✅ `Take()` - Birkaç yerde

### Toplama:
- ✅ `Count()` / `CountAsync()` - Birkaç yerde

### İlişkili Veriler:
- ✅ `Include()` - Çok sayıda yerde
- ✅ `ThenInclude()` - Çok sayıda yerde

### Kontrol:
- ✅ `Any()` - Birkaç yerde

---

## ⚠️ ÖNEMLİ NOT:
Tüm bu kullanımlar **MVC Controller action'ları** içinde. Yeni oluşturulan `AppointmentsApiController.cs` ise **API Controller** ve şu anda aktif olarak kullanılmıyor.


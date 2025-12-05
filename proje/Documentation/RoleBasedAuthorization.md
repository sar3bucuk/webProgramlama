# Rol Bazlı Yetkilendirme Nasıl Çalışır?

## 1. Veritabanı Yapısı

### AspNetRoles Tablosu
Roller burada saklanır:
- Admin
- Member  
- Trainer

### AspNetUserRoles Tablosu
Kullanıcı-rol ilişkisi burada tutulur:
- UserId: Kullanıcı ID'si
- RoleId: Rol ID'si

### Örnek Sorgu:
```sql
-- Bir kullanıcının rollerini görmek için:
SELECT u.Email, r.Name AS RoleName
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'g231210012@sakarya.edu.tr';
```

## 2. Controller Seviyesinde Yetkilendirme

### [Authorize(Roles = "Admin")] Attribute'u
Controller veya Action seviyesinde kullanılır:

```csharp
[Authorize(Roles = "Admin")]
public class GymsController : Controller
{
    // Sadece Admin rolündeki kullanıcılar bu controller'a erişebilir
}
```

### Örnekler:
- `GymsController`: Sadece Admin erişebilir
- `AdminController`: Sadece Admin erişebilir
- `AccountController`: Herkes erişebilir (attribute yok)

## 3. View Seviyesinde Kontrol

### User.IsInRole("Admin") Metodu
View'larda menü ve butonları göstermek için kullanılır:

```razor
@if (User.IsInRole("Admin"))
{
    <a href="/Admin">Admin Paneli</a>
}
```

### Örnekler:
- `_Layout.cshtml`: Admin menüsü sadece Admin'e gösterilir
- `Home/Index.cshtml`: Farklı butonlar farklı rollere gösterilir

## 4. Kullanıcı Kayıt ve Rol Atama

### Yeni Kullanıcı Kaydı:
```csharp
// AccountController.cs - Register action'ında
var user = new IdentityUser { UserName = model.Email, Email = model.Email };
await _userManager.CreateAsync(user, model.Password);
await _userManager.AddToRoleAsync(user, "Member"); // Otomatik Member rolü
```

### Admin Kullanıcısı Oluşturma:
```csharp
// DbInitializer.cs veya SeedController.cs'de
var adminUser = new IdentityUser { ... };
await _userManager.CreateAsync(adminUser, "sau");
await _userManager.AddToRoleAsync(adminUser, "Admin"); // Admin rolü
```

## 5. Sistem Akışı

1. **Kullanıcı Giriş Yapar:**
   - `AccountController.Login()` çalışır
   - Identity sistemi kullanıcıyı doğrular
   - Cookie'ye kullanıcı bilgileri ve rolleri yazılır

2. **Sayfa İstekleri:**
   - `[Authorize(Roles = "Admin")]` kontrolü yapılır
   - Kullanıcının Admin rolü varsa erişim izni verilir
   - Yoksa 403 Forbidden hatası döner

3. **View Render:**
   - `User.IsInRole("Admin")` kontrolü yapılır
   - Admin menüsü sadece Admin'e gösterilir

## 6. Rolleri Kontrol Etme

### Veritabanında:
```sql
-- Tüm kullanıcılar ve rolleri
SELECT 
    u.Email,
    r.Name AS RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.Email, r.Name;
```

### Kodda:
```csharp
// Bir kullanıcının rolünü kontrol etme
var user = await _userManager.FindByEmailAsync("email@example.com");
var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
```

## 7. Önemli Notlar

- **Authentication (Kimlik Doğrulama):** Kullanıcı giriş yapmış mı?
- **Authorization (Yetkilendirme):** Kullanıcının yetkisi var mı?

- `UseAuthentication()`: Kimlik doğrulama middleware'i
- `UseAuthorization()`: Yetkilendirme middleware'i

- Roller veritabanında saklanır
- Kullanıcı giriş yaptığında rolleri cookie'ye yazılır
- Her istekte cookie'den rolleri okunur


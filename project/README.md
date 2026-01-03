# 🏋️ Spor Salonu Yönetim ve Randevu Sistemi

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019-CC2927?logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-Educational-blue)](LICENSE)

Modern web teknolojileri kullanılarak geliştirilmiş, spor salonu yönetimi için kapsamlı bir web uygulaması. Üyeler, antrenörler, randevular, beslenme planları ve AI destekli öneriler gibi tüm iş süreçlerini tek bir platformda toplar.

## ✨ Özellikler

- 👥 **Rol Bazlı Yetkilendirme**: Admin, Member (Üye), Trainer (Antrenör) rolleri
- 🏢 **Spor Salonu Yönetimi**: Çoklu spor salonu desteği, hizmet ve fiyatlandırma yönetimi
- 👨‍🏫 **Antrenör Yönetimi**: Profil, müsaitlik takibi, uzmanlık alanları
- 📅 **Randevu Sistemi**: Online randevu oluşturma, onaylama ve takip
- 🍎 **Beslenme Planları**: Kişiselleştirilmiş beslenme programları ve makro besin takibi
- 🤖 **AI Destekli Öneriler**: OpenAI GPT-4o-mini ile egzersiz ve diyet planları
- 🔔 **Bildirim Sistemi**: Gerçek zamanlı bildirimler ve okunma takibi
- 📊 **REST API**: LINQ sorguları ile veri filtreleme ve raporlama
- 🎨 **Modern UI/UX**: Responsive tasarım, Bootstrap 5, dinamik arayüzler

## 🛠️ Teknolojiler

### Backend
- .NET 8.0
- ASP.NET Core MVC
- Entity Framework Core 8.0
- SQL Server
- ASP.NET Core Identity

### Frontend
- Bootstrap 5
- jQuery & jQuery Validation
- Razor Pages

### API & Services
- REST API
- OpenAI API (GPT-4o-mini)
- HttpClient

## 🚀 Hızlı Başlangıç

### Gereksinimler

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express veya üzeri)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) veya [Visual Studio Code](https://code.visualstudio.com/)

### Kurulum

1. **Projeyi klonlayın**
   ```bash
   git clone https://github.com/kullaniciadi/proje.git
   cd proje
   ```

2. **Veritabanı bağlantı string'ini yapılandırın**
   
   `appsettings.json` dosyasını düzenleyin:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=sporSalonu;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
   }
   ```

3. **Veritabanını oluşturun**
   ```bash
   dotnet ef database update
   ```

4. **Projeyi çalıştırın**
   ```bash
   dotnet run
   ```

5. **Tarayıcıda açın**
   ```
   https://localhost:5001
   ```

### İlk Giriş

**Admin Hesabı:**
- Email: `g231210012@sakarya.edu.tr`
- Şifre: `sau`

> Not: İlk çalıştırmada otomatik olarak admin kullanıcısı ve roller oluşturulur.

## 🔐 Roller ve Yetkiler

| Rol | Açıklama | Yetkiler |
|-----|----------|----------|
| **Admin** | Sistem yöneticisi | Tüm işlemler, spor salonu/üye/antrenör yönetimi |
| **Member** | Üye | Randevu oluşturma, profil yönetimi, beslenme planları |
| **Trainer** | Antrenör | Randevu onaylama, müsaitlik yönetimi, profil yönetimi |

## 📊 Veritabanı Şeması

Ana tablolar:
- `Gyms` - Spor salonları
- `Services` - Hizmet türleri
- `Members` - Üyeler
- `Trainers` - Antrenörler
- `Appointments` - Randevular
- `NutritionPlans` - Beslenme planları
- `AIRecommendations` - AI önerileri
- `Notifications` - Bildirimler

## 🤖 AI Entegrasyonu (Opsiyonel)

### OpenAI (Beslenme Planları ve DALL-E)

AI özelliklerini kullanmak için:

1. [OpenAI](https://platform.openai.com/) hesabı oluşturun
2. API key alın
3. `appsettings.json` dosyasına ekleyin:
   ```json
   {
     "OpenAI": {
       "ApiKey": "YOUR_OPENAI_API_KEY"
     }
   }
   ```

### Replicate / Stable Diffusion (Vücut Transformasyon Simülatörü)

Fotoğraf referanslı transformasyon için:

1. [Replicate](https://replicate.com/) hesabı oluşturun
2. API token alın
3. `appsettings.json` dosyasına ekleyin:
   ```json
   {
     "Replicate": {
       "ApiKey": "YOUR_REPLICATE_API_TOKEN"
     }
   }
   ```

**Not:** Replicate API key yoksa sistem otomatik olarak DALL-E'ye geçer, ancak fotoğraf referansı kullanılamaz.

## 📝 Migration Yönetimi

```bash
# Yeni migration oluştur
dotnet ef migrations add MigrationAdi

# Migration'ları uygula
dotnet ef database update

# Migration'ı geri al
dotnet ef database update PreviousMigrationName
```

## 🎓 Proje Bilgileri

- **Öğrenci Numarası:** g231210012
- **Üniversite:** Sakarya Üniversitesi
- **Ders:** Web Programlama
- **Proje Tipi:** Spor Salonu Yönetim ve Randevu Sistemi


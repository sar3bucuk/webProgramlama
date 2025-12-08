-- =============================================
-- Admin Kullanıcısı Oluşturma Scripti
-- Bu scripti SQL Server Management Studio'da çalıştırın
-- =============================================

USE FitnessCenter;
GO

-- Admin rolünü oluştur (eğer yoksa)
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    PRINT 'Admin rolü oluşturuldu.';
END
ELSE
BEGIN
    PRINT 'Admin rolü zaten mevcut.';
END
GO

-- Admin kullanıcısını oluştur (eğer yoksa)
DECLARE @adminEmail NVARCHAR(256) = 'g231210012@sakarya.edu.tr';
DECLARE @adminPassword NVARCHAR(MAX) = 'sau';
DECLARE @userId NVARCHAR(450) = NEWID();

IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = @adminEmail)
BEGIN
    -- Password hash oluştur (Identity'nin kullandığı format)
    -- Not: Gerçek uygulamada bu hash'i Identity oluşturur
    -- Burada basit bir hash kullanıyoruz, ama en iyisi uygulamadan oluşturmak
    
    DECLARE @passwordHash NVARCHAR(MAX);
    -- Bu hash'i uygulamadan almak daha güvenli olur
    -- Şimdilik boş bırakıyoruz, uygulama üzerinden oluşturulacak
    
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                             EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                             AccessFailedCount, LockoutEnabled, TwoFactorEnabled, PhoneNumberConfirmed)
    VALUES (@userId, @adminEmail, UPPER(@adminEmail), @adminEmail, UPPER(@adminEmail),
            1, NULL, NEWID(), NEWID(), 0, 1, 0, 0);
    
    PRINT 'Admin kullanıcısı oluşturuldu (ID: ' + @userId + ').';
    PRINT 'NOT: Şifre hash''i uygulama üzerinden oluşturulmalıdır.';
    PRINT 'Lütfen uygulamada şifre sıfırlama özelliğini kullanın veya aşağıdaki endpoint''i çağırın.';
END
ELSE
BEGIN
    PRINT 'Admin kullanıcısı zaten mevcut.';
    SELECT @userId = Id FROM AspNetUsers WHERE Email = @adminEmail;
END
GO

-- Kullanıcıyı Admin rolüne ekle
DECLARE @adminEmail2 NVARCHAR(256) = 'g231210012@sakarya.edu.tr';
DECLARE @userId2 NVARCHAR(450);
DECLARE @roleId NVARCHAR(450);

SELECT @userId2 = Id FROM AspNetUsers WHERE Email = @adminEmail2;
SELECT @roleId = Id FROM AspNetRoles WHERE Name = 'Admin';

IF @userId2 IS NOT NULL AND @roleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM AspNetUserRoles WHERE UserId = @userId2 AND RoleId = @roleId)
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@userId2, @roleId);
        PRINT 'Kullanıcı Admin rolüne eklendi.';
    END
    ELSE
    BEGIN
        PRINT 'Kullanıcı zaten Admin rolüne sahip.';
    END
END
GO

PRINT 'İşlem tamamlandı!';
PRINT 'NOT: Şifre hash''i için lütfen uygulamadaki /Admin sayfasını kullanın veya kayıt ol sayfasından yeni bir kullanıcı oluşturup sonra Admin rolü verin.';
GO


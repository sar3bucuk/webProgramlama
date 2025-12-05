-- =============================================
-- Çift Admin Kullanıcısını Temizleme Scripti
-- Bu scripti SQL Server Management Studio'da çalıştırın
-- =============================================

USE FitnessCenter;
GO

-- Önce mevcut admin kullanıcılarını göster
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    u.EmailConfirmed,
    r.Name AS RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin' OR u.Email LIKE '%@sakarya.edu.tr'
ORDER BY u.Email;
GO

-- Hangi admin kullanıcısını tutmak istediğinizi belirleyin
-- Genellikle g231210012@sakarya.edu.tr olanı tutmak isteyeceksiniz

-- Eğer eski admin kullanıcısını (ogrencinumarasi@sakarya.edu.tr) silmek istiyorsanız:
DECLARE @EmailToDelete NVARCHAR(256) = 'ogrencinumarasi@sakarya.edu.tr';
DECLARE @UserIdToDelete NVARCHAR(450);

-- Kullanıcı ID'sini bul
SELECT @UserIdToDelete = Id FROM AspNetUsers WHERE Email = @EmailToDelete;

IF @UserIdToDelete IS NOT NULL
BEGIN
    PRINT 'Silinecek kullanıcı bulundu: ' + @EmailToDelete + ' (ID: ' + @UserIdToDelete + ')';
    
    -- Önce UserRoles'dan kaldır
    DELETE FROM AspNetUserRoles WHERE UserId = @UserIdToDelete;
    PRINT 'Kullanıcı rolleri silindi.';
    
    -- Sonra UserClaims'den kaldır
    DELETE FROM AspNetUserClaims WHERE UserId = @UserIdToDelete;
    PRINT 'Kullanıcı claim''leri silindi.';
    
    -- UserLogins'den kaldır
    DELETE FROM AspNetUserLogins WHERE UserId = @UserIdToDelete;
    PRINT 'Kullanıcı login''leri silindi.';
    
    -- UserTokens'dan kaldır
    DELETE FROM AspNetUserTokens WHERE UserId = @UserIdToDelete;
    PRINT 'Kullanıcı token''ları silindi.';
    
    -- Son olarak kullanıcıyı sil
    DELETE FROM AspNetUsers WHERE Id = @UserIdToDelete;
    PRINT 'Kullanıcı başarıyla silindi: ' + @EmailToDelete;
END
ELSE
BEGIN
    PRINT 'Silinecek kullanıcı bulunamadı: ' + @EmailToDelete;
END
GO

-- Alternatif: Manuel olarak ID ile silmek isterseniz:
-- Yukarıdaki SELECT sorgusundan ID'yi alıp aşağıdaki komutu kullanın:
/*
DECLARE @UserIdToDelete NVARCHAR(450) = 'BURAYA_ID_YAZIN';

DELETE FROM AspNetUserRoles WHERE UserId = @UserIdToDelete;
DELETE FROM AspNetUserClaims WHERE UserId = @UserIdToDelete;
DELETE FROM AspNetUserLogins WHERE UserId = @UserIdToDelete;
DELETE FROM AspNetUserTokens WHERE UserId = @UserIdToDelete;
DELETE FROM AspNetUsers WHERE Id = @UserIdToDelete;
*/

-- Silme işleminden sonra kontrol
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    r.Name AS RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin'
ORDER BY u.Email;
GO

PRINT 'İşlem tamamlandı!';
GO


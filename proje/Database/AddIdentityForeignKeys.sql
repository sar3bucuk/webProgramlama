-- =============================================
-- Identity Foreign Key Constraint'lerini Ekleme Scripti
-- Bu scripti, ASP.NET Identity migration'ları çalıştırıldıktan SONRA çalıştırın
-- =============================================

USE FitnessCenter;
GO

-- AspNetUsers tablosunun var olduğunu kontrol et
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    PRINT 'AspNetUsers tablosu bulundu. Foreign key constraint''leri ekleniyor...';
    
    -- Trainers tablosu için foreign key ekle
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Trainers_User')
    BEGIN
        ALTER TABLE Trainers
        ADD CONSTRAINT FK_Trainers_User 
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE;
        PRINT 'FK_Trainers_User constraint''i eklendi.';
    END
    ELSE
    BEGIN
        PRINT 'FK_Trainers_User constraint''i zaten mevcut.';
    END
    
    -- Members tablosu için foreign key ekle
    -- NO ACTION kullanıyoruz çünkü cascade path sorunu var (Appointments->Members->AspNetUsers ve Appointments->Trainers->AspNetUsers)
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Members_User')
    BEGIN
        ALTER TABLE Members
        ADD CONSTRAINT FK_Members_User 
        FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION;
        PRINT 'FK_Members_User constraint''i eklendi (NO ACTION).';
    END
    ELSE
    BEGIN
        PRINT 'FK_Members_User constraint''i zaten mevcut.';
    END
    
    PRINT 'Tüm foreign key constraint''leri başarıyla eklendi!';
END
ELSE
BEGIN
    PRINT 'HATA: AspNetUsers tablosu bulunamadı!';
    PRINT 'Lütfen önce ASP.NET Identity migration''larını çalıştırın.';
END
GO


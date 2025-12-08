-- =============================================
-- Spor Salonu Yönetim ve Randevu Sistemi
-- Veritabanı Şema Scripti
-- =============================================

USE FitnessCenter;
GO

-- =============================================
-- 1. SPOR SALONLARI (Gyms)
-- =============================================
CREATE TABLE Gyms (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(500),
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    OpeningTime TIME NOT NULL, -- Açılış saati
    ClosingTime TIME NOT NULL, -- Kapanış saati
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2,
    CONSTRAINT CK_Gyms_Time CHECK (OpeningTime < ClosingTime)
);
GO

-- =============================================
-- 2. HİZMET TÜRLERİ (Services)
-- =============================================
CREATE TABLE Services (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL UNIQUE, -- fitness, yoga, pilates vb.
    Description NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- =============================================
-- 3. SALON-HİZMET İLİŞKİSİ (GymServices)
-- Her salonun sunduğu hizmetler, süre ve ücretleri
-- =============================================
CREATE TABLE GymServices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GymId INT NOT NULL,
    ServiceId INT NOT NULL,
    Duration INT NOT NULL, -- Dakika cinsinden süre
    Price DECIMAL(10,2) NOT NULL, -- Ücret
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_GymServices_Gym FOREIGN KEY (GymId) REFERENCES Gyms(Id) ON DELETE CASCADE,
    CONSTRAINT FK_GymServices_Service FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE,
    CONSTRAINT CK_GymServices_Duration CHECK (Duration > 0),
    CONSTRAINT CK_GymServices_Price CHECK (Price >= 0),
    CONSTRAINT UQ_GymServices_Gym_Service UNIQUE (GymId, ServiceId)
);
GO

-- =============================================
-- 4. ANTrenörLER (Trainers)
-- =============================================
CREATE TABLE Trainers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL, -- AspNetUsers ile ilişki
    GymId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    Bio NVARCHAR(1000), -- Kısa biyografi
    ExperienceYears INT DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    -- FK_Trainers_User constraint'i Identity kurulumundan sonra eklenecek
    -- CONSTRAINT FK_Trainers_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Trainers_Gym FOREIGN KEY (GymId) REFERENCES Gyms(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Trainers_Experience CHECK (ExperienceYears >= 0)
);
GO

-- =============================================
-- 5. ANTrenör UZMANLIK ALANLARI (TrainerSpecializations)
-- =============================================
CREATE TABLE TrainerSpecializations (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TrainerId INT NOT NULL,
    Specialization NVARCHAR(100) NOT NULL, -- kas geliştirme, kilo verme, yoga vb.
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrainerSpecializations_Trainer FOREIGN KEY (TrainerId) REFERENCES Trainers(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TrainerSpecializations_Trainer_Specialization UNIQUE (TrainerId, Specialization)
);
GO

-- =============================================
-- 6. ANTrenör-HİZMET İLİŞKİSİ (TrainerServices)
-- Antrenörün yapabildiği hizmet türleri
-- =============================================
CREATE TABLE TrainerServices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TrainerId INT NOT NULL,
    ServiceId INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrainerServices_Trainer FOREIGN KEY (TrainerId) REFERENCES Trainers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TrainerServices_Service FOREIGN KEY (ServiceId) REFERENCES Services(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_TrainerServices_Trainer_Service UNIQUE (TrainerId, ServiceId)
);
GO

-- =============================================
-- 7. ANTrenör MÜSAİTLİK SAATLERİ (TrainerAvailability)
-- =============================================
CREATE TABLE TrainerAvailability (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TrainerId INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 0=Pazar, 1=Pazartesi, ..., 6=Cumartesi
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_TrainerAvailability_Trainer FOREIGN KEY (TrainerId) REFERENCES Trainers(Id) ON DELETE CASCADE,
    CONSTRAINT CK_TrainerAvailability_DayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
    CONSTRAINT CK_TrainerAvailability_Time CHECK (StartTime < EndTime)
);
GO

-- =============================================
-- 8. ÜYELER (Members)
-- AspNetUsers ile ilişkili
-- =============================================
CREATE TABLE Members (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL UNIQUE, -- AspNetUsers ile ilişki
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    DateOfBirth DATE,
    Gender NVARCHAR(10), -- Male, Female, Other
    Height DECIMAL(5,2), -- Boy (cm)
    Weight DECIMAL(5,2), -- Kilo (kg)
    BodyType NVARCHAR(50), -- Ectomorph, Mesomorph, Endomorph
    HealthConditions NVARCHAR(500), -- Sağlık durumu/notlar
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2,
    -- FK_Members_User constraint'i Identity kurulumundan sonra eklenecek
    -- CONSTRAINT FK_Members_User FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT CK_Members_Height CHECK (Height > 0),
    CONSTRAINT CK_Members_Weight CHECK (Weight > 0)
);
GO

-- =============================================
-- 9. RANDEVULAR (Appointments)
-- =============================================
CREATE TABLE Appointments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MemberId INT NOT NULL,
    TrainerId INT NOT NULL,
    GymServiceId INT NOT NULL,
    AppointmentDate DATE NOT NULL,
    AppointmentTime TIME NOT NULL,
    Duration INT NOT NULL, -- Dakika
    Price DECIMAL(10,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected, Completed, Cancelled
    Notes NVARCHAR(500),
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedDate DATETIME2,
    CONSTRAINT FK_Appointments_Member FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Appointments_Trainer FOREIGN KEY (TrainerId) REFERENCES Trainers(Id) ON DELETE CASCADE,
    -- NO ACTION kullanıyoruz çünkü cascade path sorunu var (Trainers->Gyms ve GymServices->Gyms)
    CONSTRAINT FK_Appointments_GymService FOREIGN KEY (GymServiceId) REFERENCES GymServices(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Appointments_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Completed', 'Cancelled')),
    CONSTRAINT CK_Appointments_Duration CHECK (Duration > 0),
    CONSTRAINT CK_Appointments_Price CHECK (Price >= 0)
);
GO

-- =============================================
-- 10. YAPAY ZEKA ÖNERİLERİ (AIRecommendations)
-- =============================================
CREATE TABLE AIRecommendations (
    Id INT PRIMARY KEY IDENTITY(1,1),
    MemberId INT NOT NULL,
    RequestType NVARCHAR(50) NOT NULL, -- ExercisePlan, DietPlan, BodyTransformation
    InputData NVARCHAR(MAX), -- JSON formatında girilen veriler (boy, kilo, vücut tipi vb.)
    PhotoPath NVARCHAR(500), -- Yüklenen fotoğraf yolu
    AIResponse NVARCHAR(MAX), -- AI'dan gelen yanıt (öneriler)
    GeneratedImagePath NVARCHAR(500), -- AI tarafından oluşturulan görsel yolu (varsa)
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AIRecommendations_Member FOREIGN KEY (MemberId) REFERENCES Members(Id) ON DELETE CASCADE,
    CONSTRAINT CK_AIRecommendations_RequestType CHECK (RequestType IN ('ExercisePlan', 'DietPlan', 'BodyTransformation'))
);
GO

-- =============================================
-- İNDEKSLER (Performans için)
-- =============================================

-- Appointments tablosu için indeksler
CREATE INDEX IX_Appointments_MemberId ON Appointments(MemberId);
CREATE INDEX IX_Appointments_TrainerId ON Appointments(TrainerId);
CREATE INDEX IX_Appointments_AppointmentDate ON Appointments(AppointmentDate);
CREATE INDEX IX_Appointments_Status ON Appointments(Status);

-- TrainerAvailability tablosu için indeks
CREATE INDEX IX_TrainerAvailability_TrainerId ON TrainerAvailability(TrainerId);
CREATE INDEX IX_TrainerAvailability_DayOfWeek ON TrainerAvailability(DayOfWeek);

-- Members tablosu için indeks
CREATE INDEX IX_Members_UserId ON Members(UserId);

-- Trainers tablosu için indeks
CREATE INDEX IX_Trainers_UserId ON Trainers(UserId);
CREATE INDEX IX_Trainers_GymId ON Trainers(GymId);

-- GymServices tablosu için indeks
CREATE INDEX IX_GymServices_GymId ON GymServices(GymId);
CREATE INDEX IX_GymServices_ServiceId ON GymServices(ServiceId);

GO

-- =============================================
-- BAŞLANGIÇ VERİLERİ (Seed Data)
-- =============================================

-- Hizmet türleri
INSERT INTO Services (Name, Description) VALUES
('Fitness', 'Genel fitness ve kardiyovasküler egzersizler'),
('Yoga', 'Yoga ve esneklik egzersizleri'),
('Pilates', 'Pilates ve core güçlendirme'),
('CrossFit', 'CrossFit antrenmanları'),
('Kardiyo', 'Kardiyovasküler egzersizler'),
('Ağırlık Antrenmanı', 'Kas geliştirme ve güç antrenmanları'),
('Zumba', 'Dans temelli kardiyo egzersizleri'),
('Spinning', 'Bisiklet antrenmanları');

GO

-- =============================================
-- STORED PROCEDURES (İsteğe bağlı - API için kullanılabilir)
-- =============================================

-- Belirli bir tarihte uygun antrenörleri getiren stored procedure
CREATE PROCEDURE sp_GetAvailableTrainers
    @AppointmentDate DATE,
    @AppointmentTime TIME,
    @Duration INT,
    @ServiceId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DayOfWeek INT = DATEPART(WEEKDAY, @AppointmentDate) - 1; -- SQL Server'da 1=Pazar, bizim sistemde 0=Pazar
    IF @DayOfWeek = 0 SET @DayOfWeek = 6 ELSE SET @DayOfWeek = @DayOfWeek - 1;
    
    DECLARE @EndTime TIME = DATEADD(MINUTE, @Duration, @AppointmentTime);
    
    SELECT DISTINCT
        t.Id,
        t.FirstName,
        t.LastName,
        t.Bio,
        t.ExperienceYears,
        g.Name AS GymName
    FROM Trainers t
    INNER JOIN Gyms g ON t.GymId = g.Id
    INNER JOIN TrainerAvailability ta ON t.Id = ta.TrainerId
    LEFT JOIN TrainerServices ts ON t.Id = ts.TrainerId
    WHERE t.IsActive = 1
        AND ta.DayOfWeek = @DayOfWeek
        AND ta.IsAvailable = 1
        AND ta.StartTime <= @AppointmentTime
        AND ta.EndTime >= @EndTime
        AND (@ServiceId IS NULL OR ts.ServiceId = @ServiceId)
        AND NOT EXISTS (
            SELECT 1 
            FROM Appointments a 
            WHERE a.TrainerId = t.Id 
                AND a.AppointmentDate = @AppointmentDate
                AND a.Status IN ('Pending', 'Approved')
                AND (
                    (@AppointmentTime >= a.AppointmentTime AND @AppointmentTime < DATEADD(MINUTE, a.Duration, a.AppointmentTime))
                    OR (@EndTime > a.AppointmentTime AND @EndTime <= DATEADD(MINUTE, a.Duration, a.AppointmentTime))
                    OR (@AppointmentTime <= a.AppointmentTime AND @EndTime >= DATEADD(MINUTE, a.Duration, a.AppointmentTime))
                )
        );
END;
GO

-- Üye randevularını getiren stored procedure
CREATE PROCEDURE sp_GetMemberAppointments
    @MemberId INT,
    @Status NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        a.Id,
        a.AppointmentDate,
        a.AppointmentTime,
        a.Duration,
        a.Price,
        a.Status,
        a.Notes,
        t.FirstName + ' ' + t.LastName AS TrainerName,
        s.Name AS ServiceName,
        g.Name AS GymName
    FROM Appointments a
    INNER JOIN Trainers t ON a.TrainerId = t.Id
    INNER JOIN GymServices gs ON a.GymServiceId = gs.Id
    INNER JOIN Services s ON gs.ServiceId = s.Id
    INNER JOIN Gyms g ON t.GymId = g.Id
    WHERE a.MemberId = @MemberId
        AND (@Status IS NULL OR a.Status = @Status)
    ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC;
END;
GO

PRINT 'Veritabanı şeması başarıyla oluşturuldu!';
GO


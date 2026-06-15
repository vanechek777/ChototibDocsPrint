-- =============================================
-- Скрипт создания базы данных ЧТОТиБ Печать
-- Выполните в SQL Server Management Studio
-- =============================================

-- 1. Создание базы данных
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ChtotibDocPrint')
    CREATE DATABASE ChtotibDocPrint;
GO

USE ChtotibDocPrint;
GO

-- 2. Специальности
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Specialties')
CREATE TABLE Specialties (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL,
    Name NVARCHAR(300) NOT NULL,
    ShortName NVARCHAR(100) NULL,
    Qualification NVARCHAR(200) NULL DEFAULT '',
    StudyForm NVARCHAR(50) NOT NULL DEFAULT N'Очная',
    StudyYears DECIMAL(3,1) NOT NULL DEFAULT 3.10,
    IsActive BIT NOT NULL DEFAULT 1
);

-- 3. Группы
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Groups')
CREATE TABLE Groups (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    SpecialtyId INT NOT NULL,
    EnrollmentYear INT NOT NULL DEFAULT YEAR(GETDATE()),
    CourseNumber INT NOT NULL DEFAULT 1,
    Address NVARCHAR(200) NULL,
    IsGraduating BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Groups_Specialties FOREIGN KEY (SpecialtyId) REFERENCES Specialties(Id)
);

-- 4. Студенты
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Students')
CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    GroupId INT NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    MiddleName NVARCHAR(100) NULL,
    BirthDate DATE NULL,
    PreviousEducation NVARCHAR(300) NULL,
    PreviousEducationDoc NVARCHAR(300) NULL,
    DemoExamParticipantCode NVARCHAR(100) NULL,
    DemoExamScore DECIMAL(6,2) NULL,
    DemoExamMaxScore INT NOT NULL CONSTRAINT DF_Students_DemoExamMaxScore DEFAULT 70,
    DemoExamLevel NVARCHAR(100) NULL,
    RegistrationNumber NVARCHAR(50) NULL,
    Qualification NVARCHAR(200) NULL,
    IsGraduated BIT NOT NULL DEFAULT 0,
    IsExpelled BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Students_Groups FOREIGN KEY (GroupId) REFERENCES Groups(Id)
);

-- 5. Предметы
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subjects')
CREATE TABLE Subjects (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(300) NOT NULL,
    Course INT NOT NULL DEFAULT 1,
    Hours INT NULL,
    SubjectType NVARCHAR(100) NOT NULL DEFAULT N'Общеобразовательный',
    SpecialtyId INT NULL,
    IsExam BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Subjects_Specialties FOREIGN KEY (SpecialtyId) REFERENCES Specialties(Id)
);

-- 6. Оценки
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Grades')
CREATE TABLE Grades (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    SubjectId INT NOT NULL,
    Grade INT NOT NULL CHECK (Grade BETWEEN 2 AND 5),
    GradeType NVARCHAR(50) NOT NULL DEFAULT N'Итоговая',
    CONSTRAINT FK_Grades_Students FOREIGN KEY (StudentId) REFERENCES Students(Id),
    CONSTRAINT FK_Grades_Subjects FOREIGN KEY (SubjectId) REFERENCES Subjects(Id)
);

-- 7. Дипломы
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Diplomas')
CREATE TABLE Diplomas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL UNIQUE,
    Series NVARCHAR(20) NULL,
    Number NVARCHAR(20) NULL,
    DiplomaType NVARCHAR(50) NOT NULL DEFAULT N'Обычный',
    IssueDate DATE NULL,
    IsPrinted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Diplomas_Students FOREIGN KEY (StudentId) REFERENCES Students(Id)
);

-- 8. Члены комиссии
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CommissionMembers')
CREATE TABLE CommissionMembers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    Position NVARCHAR(200) NULL,
    Role NVARCHAR(100) NOT NULL DEFAULT N'Член комиссии',
    IsActive BIT NOT NULL DEFAULT 1
);

-- 9. История печати
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrintHistory')
CREATE TABLE PrintHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StudentId INT NOT NULL,
    DocumentType NVARCHAR(100) NOT NULL,
    PrintedAt DATETIME NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(50) NOT NULL DEFAULT N'Напечатано',
    CONSTRAINT FK_PrintHistory_Students FOREIGN KEY (StudentId) REFERENCES Students(Id)
);

-- 10. Настройки печати
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PrintSettings')
CREATE TABLE PrintSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FieldName NVARCHAR(100) NOT NULL,
    DocumentType NVARCHAR(100) NOT NULL,
    PositionX_mm FLOAT NOT NULL DEFAULT 0,
    PositionY_mm FLOAT NOT NULL DEFAULT 0,
    FontSize FLOAT NOT NULL DEFAULT 12,
    FontFamily NVARCHAR(100) NOT NULL DEFAULT N'Times New Roman',
    IsBold BIT NOT NULL DEFAULT 0,
    IsItalic BIT NOT NULL DEFAULT 0,
    TextAlignment NVARCHAR(20) NOT NULL DEFAULT N'Left',
    IsActive BIT NOT NULL DEFAULT 1
);

-- 11. Представление: выпускники без рег. номера
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_GraduatesWithoutRegNumber')
    DROP VIEW vw_GraduatesWithoutRegNumber;
GO

CREATE VIEW vw_GraduatesWithoutRegNumber AS
SELECT s.Id, s.LastName, s.FirstName, s.MiddleName, g.Name AS GroupName
FROM Students s
INNER JOIN Groups g ON s.GroupId = g.Id
WHERE g.IsGraduating = 1
  AND s.IsExpelled = 0
  AND (s.RegistrationNumber IS NULL OR s.RegistrationNumber = '');
GO

PRINT N'База данных ChtotibDocPrint создана успешно!';
GO

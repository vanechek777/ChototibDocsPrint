-- Демо-данные: группы СиС-22-1, ИСиП-22-2в, ИСиП-22-3а; 10 предметов; по 25 студентов в группе.
-- ВНИМАНИЕ: на Windows sqlcmd без UTF-8 портит кириллицу. Предпочтительно:
--   dotnet run --project Tools\SeedDemoData
-- Если всё же sqlcmd: chcp 65001 и sqlcmd ... -f 65001 -i SeedDemoData.sql
USE ChtotibDocPrint;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;

DECLARE @SpIsip INT, @SpSis INT;

IF NOT EXISTS (SELECT 1 FROM Specialties WHERE Code = N'09.02.07')
    INSERT INTO Specialties (Code, Name, ShortName, Qualification, StudyForm, StudyYears)
    VALUES (N'09.02.07', N'Информационные системы и программирование', N'ИСиП', N'Программист', N'Очная', 3.10);
SET @SpIsip = (SELECT Id FROM Specialties WHERE Code = N'09.02.07');

IF NOT EXISTS (SELECT 1 FROM Specialties WHERE Code = N'09.02.06')
    INSERT INTO Specialties (Code, Name, ShortName, Qualification, StudyForm, StudyYears)
    VALUES (N'09.02.06', N'Сетевое и системное администрирование', N'СиС', N'Специалист по защите информации', N'Очная', 3.10);
SET @SpSis = (SELECT Id FROM Specialties WHERE Code = N'09.02.06');

-- Предметы (общие для всех специальностей)
DECLARE @Subjects TABLE (Name NVARCHAR(300), Course INT, Hours INT, SubjectType NVARCHAR(100), IsExam BIT);
INSERT INTO @Subjects VALUES
 (N'Математика', 3, 144, N'Общеобразовательный', 1),
 (N'Русский язык', 3, 108, N'Общеобразовательный', 0),
 (N'Информатика', 3, 180, N'Профессиональный', 1),
 (N'Иностранный язык', 3, 72, N'Общеобразовательный', 0),
 (N'История', 3, 72, N'Общеобразовательный', 0),
 (N'Физическая культура', 3, 144, N'Общеобразовательный', 0),
 (N'Основы алгоритмизации и программирования', 3, 216, N'Профессиональный', 1),
 (N'Базы данных', 3, 144, N'Профессиональный', 1),
 (N'Разработка программных модулей', 3, 180, N'Профессиональный', 1),
 (N'Курсовая работа по профессии', 3, 72, N'Курсовая работа', 0);

INSERT INTO Subjects (Name, Course, Hours, SubjectType, SpecialtyId, IsExam)
SELECT s.Name, s.Course, s.Hours, s.SubjectType, NULL, s.IsExam
FROM @Subjects s
WHERE NOT EXISTS (SELECT 1 FROM Subjects sub WHERE sub.Name = s.Name);

-- Группы
DECLARE @Groups TABLE (Name NVARCHAR(100), SpecialtyId INT);
INSERT INTO @Groups VALUES (N'СиС-22-1', @SpSis), (N'ИСиП-22-2в', @SpIsip), (N'ИСиП-22-3а', @SpIsip);

INSERT INTO Groups (Name, SpecialtyId, EnrollmentYear, CourseNumber, Address, IsGraduating)
SELECT g.Name, g.SpecialtyId, 2022, 4, N'г. Чита, ул. Бабушкина, 109', 1
FROM @Groups g
WHERE NOT EXISTS (SELECT 1 FROM Groups gr WHERE gr.Name = g.Name);

-- ФИО (25 наборов)
DECLARE @LastNames TABLE (Idx INT PRIMARY KEY, Ln NVARCHAR(100));
INSERT INTO @LastNames VALUES
 (1,N'Иванов'),(2,N'Петров'),(3,N'Сидоров'),(4,N'Козлов'),(5,N'Новиков'),
 (6,N'Морозов'),(7,N'Волков'),(8,N'Соколов'),(9,N'Лебедев'),(10,N'Кузнецов'),
 (11,N'Попов'),(12,N'Васильев'),(13,N'Смирнов'),(14,N'Михайлов'),(15,N'Фёдоров'),
 (16,N'Андреев'),(17,N'Алексеев'),(18,N'Романов'),(19,N'Орлов'),(20,N'Семёнов'),
 (21,N'Егоров'),(22,N'Павлов'),(23,N'Голубев'),(24,N'Борисов'),(25,N'Яковлев');

DECLARE @FirstNames TABLE (Idx INT PRIMARY KEY, Fn NVARCHAR(100), Mn NVARCHAR(100));
INSERT INTO @FirstNames VALUES
 (1,N'Алексей',N'Сергеевич'),(2,N'Дмитрий',N'Андреевич'),(3,N'Максим',N'Игоревич'),
 (4,N'Иван',N'Петрович'),(5,N'Артём',N'Николаевич'),(6,N'Кирилл',N'Олегович'),
 (7,N'Никита',N'Владимирович'),(8,N'Егор',N'Дмитриевич'),(9,N'Даниил',N'Александрович'),
 (10,N'Тимофей',N'Романович'),(11,N'Матвей',N'Евгеньевич'),(12,N'Степан',N'Ильич'),
 (13,N'Глеб',N'Викторович'),(14,N'Филипп',N'Юрьевич'),(15,N'Павел',N'Константинович'),
 (16,N'Руслан',N'Тимурович'),(17,N'Владислав',N'Борисович'),(18,N'Георгий',N'Михайлович'),
 (19,N'Семён',N'Антонович'),(20,N'Лев',N'Станиславович'),(21,N'Марк',N'Вадимович'),
 (22,N'Ярослав',N'Геннадьевич'),(23,N'Родион',N'Павлович'),(24,N'Всеволод',N'Аркадьевич'),
 (25,N'Богдан',N'Филиппович');

DECLARE @FemaleFirst TABLE (Idx INT PRIMARY KEY, Fn NVARCHAR(100), Mn NVARCHAR(100));
INSERT INTO @FemaleFirst VALUES
 (1,N'Анна',N'Сергеевна'),(2,N'Мария',N'Андреевна'),(3,N'Елена',N'Игоревна'),
 (4,N'Ольга',N'Петровна'),(5,N'Дарья',N'Николаевна'),(6,N'Виктория',N'Олеговна'),
 (7,N'Полина',N'Владимировна'),(8,N'София',N'Дмитриевна'),(9,N'Алина',N'Александровна'),
 (10,N'Ксения',N'Романовна'),(11,N'Валерия',N'Евгеньевна'),(12,N'Юлия',N'Ильинична'),
 (13,N'Екатерина',N'Викторовна'),(14,N'Наталья',N'Юрьевна'),(15,N'Татьяна',N'Константиновна'),
 (16,N'Ирина',N'Тимуровна'),(17,N'Вероника',N'Борисовна'),(18,N'Анастасия',N'Михайловна'),
 (19,N'Людмила',N'Антоновна'),(20,N'Светлана',N'Станиславовна'),(21,N'Кристина',N'Вадимовна'),
 (22,N'Арина',N'Геннадьевна'),(23,N'Милана',N'Павловна'),(24,N'Диана',N'Аркадьевна'),
 (25,N'Варвара',N'Филипповна');

DECLARE @GroupName NVARCHAR(100), @GroupId INT, @GroupSlug NVARCHAR(20), @SpId INT, @Qual NVARCHAR(200);
DECLARE @i INT = 1, @StudentId INT, @SubjectId INT, @GradeVal INT, @GradeType NVARCHAR(50), @SubjectName NVARCHAR(300);
DECLARE @Reg NVARCHAR(50), @Series NVARCHAR(20) = N'107724', @DipNum NVARCHAR(20);
DECLARE @UseFemale BIT;

DECLARE gcur CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @Groups;
OPEN gcur;
FETCH NEXT FROM gcur INTO @GroupName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @GroupId = Id, @SpId = SpecialtyId FROM Groups WHERE Name = @GroupName;
    SET @GroupSlug = REPLACE(REPLACE(REPLACE(@GroupName, N'ИСиП', N'ISIP'), N'СиС', N'SIS'), N'-', N'');
    SELECT @Qual = Qualification FROM Specialties WHERE Id = @SpId;

    SET @i = 1;
    WHILE @i <= 25
    BEGIN
        SET @Reg = N'DEMO-' + @GroupSlug + N'-' + RIGHT(N'00' + CAST(@i AS NVARCHAR(2)), 2);
        IF NOT EXISTS (SELECT 1 FROM Students WHERE RegistrationNumber = @Reg)
        BEGIN
            SET @UseFemale = CASE WHEN @i % 3 = 0 THEN 1 ELSE 0 END;
            INSERT INTO Students (GroupId, LastName, FirstName, MiddleName, BirthDate,
                PreviousEducation, PreviousEducationDoc, RegistrationNumber, Qualification, IsGraduated)
            SELECT @GroupId,
                ln.Ln,
                CASE WHEN @UseFemale = 1 THEN ff.Fn ELSE fn.Fn END,
                CASE WHEN @UseFemale = 1 THEN ff.Mn ELSE fn.Mn END,
                DATEADD(YEAR, -20 - (@i % 3), DATEFROMPARTS(2006, (@i % 12) + 1, (@i % 28) + 1)),
                N'ГБПОУ «ЧТОТиБ»', N'Аттестат № ' + CAST(100000 + @i AS NVARCHAR(10)),
                @Reg, @Qual, 1
            FROM @LastNames ln
            INNER JOIN @FirstNames fn ON fn.Idx = @i
            INNER JOIN @FemaleFirst ff ON ff.Idx = @i
            WHERE ln.Idx = @i;
        END
        SET @i = @i + 1;
    END

    -- Оценки и дипломы
    DECLARE scur CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id FROM Students WHERE GroupId = @GroupId AND RegistrationNumber LIKE N'DEMO-' + @GroupSlug + N'-%';
    OPEN scur;
    FETCH NEXT FROM scur INTO @StudentId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DECLARE @StudIdx INT = TRY_CAST(RIGHT((SELECT RegistrationNumber FROM Students WHERE Id = @StudentId), 2) AS INT);
        IF @StudIdx IS NULL SET @StudIdx = 1;

        -- Отличники: первые 3 студента в ИСиП-22-3а — все «5»; в остальных группах — смешанные оценки
        SET @GradeVal = CASE
            WHEN @GroupName = N'ИСиП-22-3а' AND @StudIdx <= 3 THEN 5
            WHEN @StudIdx % 5 = 0 THEN 3
            WHEN @StudIdx % 4 = 0 THEN 4
            ELSE 5
        END;

        DECLARE subcur CURSOR LOCAL FAST_FORWARD FOR
            SELECT Id, Name FROM Subjects WHERE Name IN (SELECT Name FROM @Subjects);
        OPEN subcur;
        FETCH NEXT FROM subcur INTO @SubjectId, @SubjectName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @GradeType = CASE WHEN @SubjectName LIKE N'%Курсовая%' THEN N'Курсовая' ELSE N'Итоговая' END;
            IF NOT EXISTS (SELECT 1 FROM Grades WHERE StudentId = @StudentId AND SubjectId = @SubjectId)
                INSERT INTO Grades (StudentId, SubjectId, Grade, GradeType)
                VALUES (@StudentId, @SubjectId, @GradeVal, @GradeType);
            FETCH NEXT FROM subcur INTO @SubjectId, @SubjectName;
        END
        CLOSE subcur; DEALLOCATE subcur;

        SET @DipNum = RIGHT(N'000000' + CAST(100000 + @StudentId AS NVARCHAR(10)), 6);
        IF NOT EXISTS (SELECT 1 FROM Diplomas WHERE StudentId = @StudentId)
            INSERT INTO Diplomas (StudentId, Series, Number, DiplomaType, IssueDate, IsPrinted)
            VALUES (@StudentId, @Series, @DipNum,
                CASE WHEN @GroupName = N'ИСиП-22-3а' AND @StudIdx <= 3 THEN N'С отличием' ELSE N'Обычный' END,
                DATEFROMPARTS(2026, 6, 30), 0);
        ELSE
            UPDATE Diplomas SET
                Series = @Series, Number = @DipNum,
                DiplomaType = CASE WHEN @GroupName = N'ИСиП-22-3а' AND @StudIdx <= 3 THEN N'С отличием' ELSE N'Обычный' END,
                IssueDate = DATEFROMPARTS(2026, 6, 30)
            WHERE StudentId = @StudentId;

        FETCH NEXT FROM scur INTO @StudentId;
    END
    CLOSE scur; DEALLOCATE scur;

    FETCH NEXT FROM gcur INTO @GroupName;
END
CLOSE gcur; DEALLOCATE gcur;

DECLARE @CntGroups INT, @CntSubjects INT, @CntStudents INT;
SELECT @CntGroups = COUNT(*) FROM Groups WHERE Name IN (N'СиС-22-1', N'ИСиП-22-2в', N'ИСиП-22-3а');
SELECT @CntSubjects = COUNT(*) FROM Subjects WHERE Name IN (SELECT Name FROM @Subjects);
SELECT @CntStudents = COUNT(*) FROM Students WHERE RegistrationNumber LIKE N'DEMO-%';
PRINT N'Готово. Группы: ' + CAST(@CntGroups AS NVARCHAR(10));
PRINT N'Предметов (демо-набор): ' + CAST(@CntSubjects AS NVARCHAR(10));
PRINT N'Студентов DEMO: ' + CAST(@CntStudents AS NVARCHAR(10));
GO

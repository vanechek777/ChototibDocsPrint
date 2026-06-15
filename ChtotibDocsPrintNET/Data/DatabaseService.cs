using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Services;
using ChtotibDocsPrintNET.ViewModels;
using Microsoft.Data.SqlClient;

namespace ChtotibDocsPrintNET.Data;

public class DatabaseService
{
    private static DatabaseService? _instance;
    public static DatabaseService Instance => _instance ??= new DatabaseService();

    private string _connectionString;

    private DatabaseService()
    {
        _instance = this;
        var saved = ViewModels.AppSettings.LoadConnectionString();
        _connectionString = !string.IsNullOrEmpty(saved)
            ? saved
            : @"Server=(localdb)\MSSQLLocalDB;Database=ChtotibDocPrint;Trusted_Connection=True;TrustServerCertificate=True;";
        EnsureSchemaPatches();
    }

    public void SetConnectionString(string cs) => _connectionString = cs;

    /// <summary>Доп. колонки для существующих БД (идемпотентно).</summary>
    private void EnsureSchemaPatches()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'Qualification')
                  ALTER TABLE dbo.Students ADD Qualification NVARCHAR(200) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'PreviousEducationDoc')
                  ALTER TABLE dbo.Students ADD PreviousEducationDoc NVARCHAR(300) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Diplomas') AND name = N'IssueDate')
                  ALTER TABLE dbo.Diplomas ADD IssueDate DATE NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'DemoExamParticipantCode')
                  ALTER TABLE dbo.Students ADD DemoExamParticipantCode NVARCHAR(100) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'DemoExamScore')
                  ALTER TABLE dbo.Students ADD DemoExamScore DECIMAL(6,2) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'DemoExamMaxScore')
                  ALTER TABLE dbo.Students ADD DemoExamMaxScore INT NOT NULL CONSTRAINT DF_Students_DemoExamMaxScore DEFAULT 70;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Students') AND name = N'DemoExamLevel')
                  ALTER TABLE dbo.Students ADD DemoExamLevel NVARCHAR(100) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Grades') AND name = N'PracticePlace')
                  ALTER TABLE dbo.Grades ADD PracticePlace NVARCHAR(300) NULL;
                  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Grades') AND name = N'PracticeTrainingMeans')
                  ALTER TABLE dbo.Grades ADD PracticeTrainingMeans NVARCHAR(1000) NULL;", conn);
            cmd.ExecuteNonQuery();
        }
        catch { /* ignore */ }

        try { SchemaSlimmingService.ApplyIfNeeded(this); }
        catch { /* ignore */ }
    }

    private SqlConnection GetConnection() => new(_connectionString);

    // ===== STATISTICS =====
    public int GetTotalStudents() => ExecuteScalar<int>("SELECT COUNT(*) FROM Students WHERE IsExpelled=0");
    public int GetTotalGraduates() => ExecuteScalar<int>("SELECT COUNT(*) FROM Students WHERE IsGraduated=1 AND IsExpelled=0");
    public int GetTotalGroups() => ExecuteScalar<int>("SELECT COUNT(*) FROM Groups");
    public int GetTotalSpecialties() => ExecuteScalar<int>("SELECT COUNT(*) FROM Specialties WHERE IsActive=1");
    public int GetPrintedToday() => ExecuteScalar<int>("SELECT COUNT(*) FROM PrintHistory WHERE CAST(PrintedAt AS DATE)=CAST(GETDATE() AS DATE)");

    public DateTime? GetLastPrintDate()
    {
        var result = ExecuteScalar<object>("SELECT TOP 1 PrintedAt FROM PrintHistory ORDER BY PrintedAt DESC");
        return result as DateTime?;
    }

    public (int Filled, int Total) GetDataCompletionStats()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT COUNT(*) AS Total,
              SUM(CASE WHEN s.RegistrationNumber IS NOT NULL AND s.RegistrationNumber<>'' THEN 1 ELSE 0 END) AS Filled
              FROM Students s INNER JOIN Groups g ON s.GroupId=g.Id
              WHERE g.IsGraduating=1 AND s.IsExpelled=0", conn);
        using var r = cmd.ExecuteReader();
        if (r.Read()) return (r.GetInt32(1), r.GetInt32(0));
        return (0, 0);
    }

    public int GetGraduatesWithoutRegNumberCount() =>
        ExecuteScalar<int>("SELECT COUNT(*) FROM vw_GraduatesWithoutRegNumber");

    public List<RecentPrintItem> GetRecentPrints(int count)
    {
        var list = new List<RecentPrintItem>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            $@"SELECT TOP {count} s.LastName+' '+s.FirstName AS Name, ph.DocumentType, ph.PrintedAt, ph.Status
               FROM PrintHistory ph INNER JOIN Students s ON ph.StudentId=s.Id
               ORDER BY ph.PrintedAt DESC", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new RecentPrintItem
            {
                StudentName = r.GetString(0),
                DocumentType = r.GetString(1),
                PrintedAt = r.GetDateTime(2).ToString("dd.MM.yyyy HH:mm"),
                Status = r.GetString(3)
            });
        return list;
    }

    // ===== GROUPS =====
    public List<string> GetDistinctAddresses()
    {
        var list = new List<string>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT DISTINCT Address FROM Groups WHERE Address IS NOT NULL ORDER BY Address", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    public List<Group> GetGroups(bool onlyGraduating, string? address, string? search)
    {
        var list = new List<Group>();
        using var conn = GetConnection();
        conn.Open();
        var sql = @"SELECT g.Id, g.Name, g.SpecialtyId, g.Address, g.IsGraduating, g.EnrollmentYear, g.CourseNumber,
                    sp.ShortName AS SpecialtyName, (SELECT COUNT(*) FROM Students s WHERE s.GroupId=g.Id AND s.IsExpelled=0) AS StudentCount
                    FROM Groups g LEFT JOIN Specialties sp ON g.SpecialtyId=sp.Id WHERE 1=1";
        if (onlyGraduating) sql += " AND g.IsGraduating=1";
        if (address != null) sql += $" AND g.Address=@addr";
        if (search != null) sql += " AND g.Name LIKE @search";
        sql += " ORDER BY g.Name";

        using var cmd = new SqlCommand(sql, conn);
        if (address != null) cmd.Parameters.AddWithValue("@addr", address);
        if (search != null) cmd.Parameters.AddWithValue("@search", $"%{search}%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Group
            {
                Id = r.GetInt32(0), Name = r.GetString(1), SpecialtyId = r.GetInt32(2),
                Address = r.IsDBNull(3) ? null : r.GetString(3),
                IsGraduating = r.GetBoolean(4), EnrollmentYear = r.GetInt32(5),
                CourseNumber = r.GetInt32(6),
                SpecialtyName = r.IsDBNull(7) ? null : r.GetString(7),
                StudentCount = r.GetInt32(8)
            });
        return list;
    }

    public Group? GetGroupById(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT g.Id, g.Name, g.SpecialtyId, g.EnrollmentYear, g.CourseNumber, g.Address, g.IsGraduating,
                     sp.Name AS SpecialtyName
              FROM Groups g
              LEFT JOIN Specialties sp ON g.SpecialtyId=sp.Id WHERE g.Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Group
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            SpecialtyId = r.GetInt32(2),
            EnrollmentYear = r.GetInt32(3),
            CourseNumber = r.GetInt32(4),
            Address = r.IsDBNull(5) ? null : r.GetString(5),
            IsGraduating = r.GetBoolean(6),
            SpecialtyName = r.IsDBNull(7) ? null : r.GetString(7),
        };
    }

    // ===== STUDENTS =====
    public List<Student> GetAllStudents(bool onlyGraduates, string? search)
    {
        var list = new List<Student>();
        using var conn = GetConnection();
        conn.Open();
        var sql = @"SELECT s.Id, s.LastName, s.FirstName, s.MiddleName, s.GroupId, s.IsGraduated, g.Name AS GroupName
                    FROM Students s LEFT JOIN Groups g ON s.GroupId=g.Id WHERE s.IsExpelled=0";
        if (onlyGraduates) sql += " AND s.IsGraduated=1";
        if (search != null) sql += " AND (s.LastName LIKE @s OR s.FirstName LIKE @s OR s.MiddleName LIKE @s)";
        sql += " ORDER BY s.LastName, s.FirstName";

        using var cmd = new SqlCommand(sql, conn);
        if (search != null) cmd.Parameters.AddWithValue("@s", $"%{search}%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Student
            {
                Id = r.GetInt32(0), LastName = r.GetString(1), FirstName = r.GetString(2),
                MiddleName = r.IsDBNull(3) ? null : r.GetString(3),
                GroupId = r.GetInt32(4), IsGraduated = r.GetBoolean(5),
                GroupName = r.IsDBNull(6) ? null : r.GetString(6)
            });
        return list;
    }

    public List<Student> GetStudentsByGroup(int groupId, string? search)
    {
        var list = new List<Student>();
        using var conn = GetConnection();
        conn.Open();
        var sql = "SELECT Id,LastName,FirstName,MiddleName,GroupId,IsGraduated,RegistrationNumber FROM Students WHERE GroupId=@gid AND IsExpelled=0";
        if (search != null) sql += " AND (LastName LIKE @s OR FirstName LIKE @s)";
        sql += " ORDER BY LastName, FirstName";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@gid", groupId);
        if (search != null) cmd.Parameters.AddWithValue("@s", $"%{search}%");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Student
            {
                Id = r.GetInt32(0), LastName = r.GetString(1), FirstName = r.GetString(2),
                MiddleName = r.IsDBNull(3) ? null : r.GetString(3),
                GroupId = r.GetInt32(4), IsGraduated = r.GetBoolean(5),
                RegistrationNumber = r.IsDBNull(6) ? null : r.GetString(6)
            });
        return list;
    }

    public Student? GetStudentById(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT s.Id, s.GroupId, s.LastName, s.FirstName, s.MiddleName, s.BirthDate,
                     s.PreviousEducation, s.PreviousEducationDoc, s.RegistrationNumber, s.Qualification,
                     s.IsGraduated, s.IsExpelled, g.Name AS GroupName,
                     s.DemoExamParticipantCode, s.DemoExamScore, s.DemoExamMaxScore, s.DemoExamLevel
              FROM Students s
              LEFT JOIN Groups g ON s.GroupId=g.Id WHERE s.Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Student
        {
            Id = r.GetInt32(0),
            GroupId = r.GetInt32(1),
            LastName = r.GetString(2),
            FirstName = r.GetString(3),
            MiddleName = r.IsDBNull(4) ? null : r.GetString(4),
            BirthDate = r.IsDBNull(5) ? null : r.GetDateTime(5),
            PreviousEducation = r.IsDBNull(6) ? null : r.GetString(6),
            PreviousEducationDoc = r.IsDBNull(7) ? null : r.GetString(7),
            RegistrationNumber = r.IsDBNull(8) ? null : r.GetString(8),
            Qualification = r.IsDBNull(9) ? null : r.GetString(9),
            IsGraduated = r.GetBoolean(10),
            IsExpelled = r.GetBoolean(11),
            GroupName = r.IsDBNull(12) ? null : r.GetString(12),
            DemoExamParticipantCode = r.IsDBNull(13) ? null : r.GetString(13),
            DemoExamScore = r.IsDBNull(14) ? null : r.GetDecimal(14),
            DemoExamMaxScore = r.IsDBNull(15) ? 70 : r.GetInt32(15),
            DemoExamLevel = r.IsDBNull(16) ? null : r.GetString(16),
        };
    }

    /// <summary>Обновляет поля, связанные с дипломом у студента и запись Diplomas.</summary>
    public void UpdateStudentAndDiplomaBlank(
        int studentId,
        string? registrationNumber,
        string? qualification,
        string? diplomaSeries,
        string? diplomaNumber,
        string? diplomaType = null,
        string? previousEducation = null,
        string? previousEducationDoc = null,
        DateTime? diplomaIssueDate = null,
        string? demoExamParticipantCode = null,
        decimal? demoExamScore = null,
        int? demoExamMaxScore = null,
        string? demoExamLevel = null)
    {
        using var conn = GetConnection();
        conn.Open();
        using (var cmd = new SqlCommand(
                   @"UPDATE Students SET RegistrationNumber=@rn, Qualification=@q,
                     PreviousEducation=@pe, PreviousEducationDoc=@ped,
                     DemoExamParticipantCode=@dec, DemoExamScore=@des,
                     DemoExamMaxScore=@dem, DemoExamLevel=@del WHERE Id=@id", conn))
        {
            cmd.Parameters.AddWithValue("@id", studentId);
            cmd.Parameters.AddWithValue("@rn", string.IsNullOrWhiteSpace(registrationNumber) ? (object)DBNull.Value : registrationNumber);
            cmd.Parameters.AddWithValue("@q", string.IsNullOrWhiteSpace(qualification) ? (object)DBNull.Value : qualification);
            cmd.Parameters.AddWithValue("@pe", string.IsNullOrWhiteSpace(previousEducation) ? (object)DBNull.Value : previousEducation);
            cmd.Parameters.AddWithValue("@ped", string.IsNullOrWhiteSpace(previousEducationDoc) ? (object)DBNull.Value : previousEducationDoc);
            cmd.Parameters.AddWithValue("@dec", string.IsNullOrWhiteSpace(demoExamParticipantCode) ? (object)DBNull.Value : demoExamParticipantCode);
            cmd.Parameters.AddWithValue("@des", demoExamScore.HasValue ? demoExamScore.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@dem", demoExamMaxScore is > 0 ? demoExamMaxScore.Value : 70);
            cmd.Parameters.AddWithValue("@del", string.IsNullOrWhiteSpace(demoExamLevel) ? (object)DBNull.Value : demoExamLevel);
            cmd.ExecuteNonQuery();
        }

        UpsertDiplomaBlank(conn, studentId, diplomaSeries, diplomaNumber, diplomaType, diplomaIssueDate);
    }

    /// <summary>Дозаполняет пустые поля демоэкзамена, не трогая уже внесённые значения.</summary>
    public void BackfillStudentDemoExamIfEmpty(int studentId, string code, decimal score, int maxScore, string level)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE Students SET
                DemoExamParticipantCode = CASE WHEN DemoExamParticipantCode IS NULL OR LTRIM(RTRIM(DemoExamParticipantCode)) = N'' THEN @dec ELSE DemoExamParticipantCode END,
                DemoExamScore = CASE WHEN DemoExamScore IS NULL THEN @des ELSE DemoExamScore END,
                DemoExamMaxScore = CASE WHEN DemoExamMaxScore IS NULL OR DemoExamMaxScore <= 0 THEN @dem ELSE DemoExamMaxScore END,
                DemoExamLevel = CASE WHEN DemoExamLevel IS NULL OR LTRIM(RTRIM(DemoExamLevel)) = N'' THEN @del ELSE DemoExamLevel END
              WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", studentId);
        cmd.Parameters.AddWithValue("@dec", code);
        cmd.Parameters.AddWithValue("@des", score);
        cmd.Parameters.AddWithValue("@dem", maxScore > 0 ? maxScore : 70);
        cmd.Parameters.AddWithValue("@del", level);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Серия и номер бланка диплома (красные цифры), таблица Diplomas.</summary>
    private static void UpsertDiplomaBlank(
        SqlConnection conn,
        int studentId,
        string? series,
        string? number,
        string? diplomaType,
        DateTime? issueDate)
    {
        var type = string.IsNullOrWhiteSpace(diplomaType) ? "Обычный" : diplomaType.Trim();
        using var check = new SqlCommand("SELECT COUNT(1) FROM Diplomas WHERE StudentId=@sid", conn);
        check.Parameters.AddWithValue("@sid", studentId);
        var cnt = check.ExecuteScalar();
        var exists = cnt != null && cnt != DBNull.Value && Convert.ToInt32(cnt) > 0;
        if (exists)
        {
            using var u = new SqlCommand(
                "UPDATE Diplomas SET Series=@s, Number=@n, DiplomaType=@t, IssueDate=@idt WHERE StudentId=@sid", conn);
            u.Parameters.AddWithValue("@sid", studentId);
            u.Parameters.AddWithValue("@s", string.IsNullOrWhiteSpace(series) ? (object)DBNull.Value : series.Trim());
            u.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(number) ? (object)DBNull.Value : number.Trim());
            u.Parameters.AddWithValue("@t", type);
            u.Parameters.AddWithValue("@idt", issueDate.HasValue ? issueDate.Value.Date : DBNull.Value);
            u.ExecuteNonQuery();
            return;
        }

        using var ins = new SqlCommand(
            @"INSERT INTO Diplomas (StudentId,Series,Number,DiplomaType,IssueDate,IsPrinted)
              VALUES (@sid,@s,@n,@t,@idt,0)", conn);
        ins.Parameters.AddWithValue("@sid", studentId);
        ins.Parameters.AddWithValue("@s", string.IsNullOrWhiteSpace(series) ? (object)DBNull.Value : series.Trim());
        ins.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(number) ? (object)DBNull.Value : number.Trim());
        ins.Parameters.AddWithValue("@t", type);
        ins.Parameters.AddWithValue("@idt", issueDate.HasValue ? issueDate.Value.Date : DBNull.Value);
        ins.ExecuteNonQuery();
    }

    // ===== GRADES =====
    public List<Grade> GetStudentGrades(int studentId)
    {
        var list = new List<Grade>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT g.Id, g.StudentId, g.SubjectId, g.Grade, g.GradeType,
              sub.Name, sub.Course, sub.Hours, g.PracticePlace, g.PracticeTrainingMeans
              FROM Grades g
              INNER JOIN Subjects sub ON g.SubjectId=sub.Id
              INNER JOIN Students st ON st.Id=g.StudentId
              INNER JOIN Groups grp ON grp.Id=st.GroupId
              WHERE g.StudentId=@sid
                AND (sub.SpecialtyId IS NULL OR sub.SpecialtyId=grp.SpecialtyId)
                AND sub.Name <> N'Демонстрационный экзамен'
              ORDER BY sub.Course, sub.Name", conn);
        cmd.Parameters.AddWithValue("@sid", studentId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Grade
            {
                Id = r.GetInt32(0), StudentId = r.GetInt32(1), SubjectId = r.GetInt32(2),
                GradeValue = r.GetInt32(3), GradeType = r.GetString(4),
                SubjectName = r.GetString(5), Course = r.GetInt32(6),
                Hours = r.IsDBNull(7) ? null : r.GetInt32(7),
                PracticePlace = r.IsDBNull(8) ? null : r.GetString(8),
                PracticeTrainingMeans = r.IsDBNull(9) ? null : r.GetString(9),
            });
        return list;
    }

    public GroupGradeStats GetGroupGradeStats(int groupId)
    {
        var stats = new GroupGradeStats();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT COUNT(DISTINCT s.Id) AS Total,
              SUM(CASE WHEN avgG.AvgGrade >= 4.5 THEN 1 ELSE 0 END) AS Excellent,
              SUM(CASE WHEN avgG.AvgGrade >= 3.5 AND avgG.AvgGrade < 4.5 THEN 1 ELSE 0 END) AS Good
              FROM Students s
              LEFT JOIN (SELECT StudentId, AVG(CAST(Grade AS FLOAT)) AS AvgGrade FROM Grades GROUP BY StudentId) avgG ON s.Id=avgG.StudentId
              WHERE s.GroupId=@gid AND s.IsExpelled=0", conn);
        cmd.Parameters.AddWithValue("@gid", groupId);
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            stats.Total = r.GetInt32(0);
            stats.ExcellentCount = r.IsDBNull(1) ? 0 : r.GetInt32(1);
            stats.GoodCount = r.IsDBNull(2) ? 0 : r.GetInt32(2);
            stats.SuccessRate = stats.Total > 0 ? 100 : 0;
            stats.ExcellentPercent = stats.Total > 0 ? stats.ExcellentCount * 100.0 / stats.Total : 0;
            stats.GoodPercent = stats.Total > 0 ? stats.GoodCount * 100.0 / stats.Total : 0;
        }
        return stats;
    }

    // ===== DIPLOMAS =====
    public Diploma? GetDiplomaByStudent(int studentId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT Id, StudentId, Series, Number, DiplomaType, IssueDate, IsPrinted FROM Diplomas WHERE StudentId=@sid", conn);
        cmd.Parameters.AddWithValue("@sid", studentId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        DateTime? issueDate = null;
        try
        {
            var ord = r.GetOrdinal("IssueDate");
            if (!r.IsDBNull(ord)) issueDate = r.GetDateTime(ord);
        }
        catch { /* колонка ещё не добавлена */ }

        return new Diploma
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            StudentId = r.GetInt32(r.GetOrdinal("StudentId")),
            Series = r.IsDBNull(r.GetOrdinal("Series")) ? null : r.GetString(r.GetOrdinal("Series")),
            Number = r.IsDBNull(r.GetOrdinal("Number")) ? null : r.GetString(r.GetOrdinal("Number")),
            DiplomaType = r.GetString(r.GetOrdinal("DiplomaType")),
            IssueDate = issueDate,
            IsPrinted = r.GetBoolean(r.GetOrdinal("IsPrinted"))
        };
    }

    // ===== PRINT SETTINGS =====
    public List<PrintSetting> GetPrintSettings(string documentType)
    {
        var list = new List<PrintSetting>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT * FROM PrintSettings WHERE DocumentType=@dt AND IsActive=1", conn);
        cmd.Parameters.AddWithValue("@dt", documentType);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new PrintSetting
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                FieldName = r.GetString(r.GetOrdinal("FieldName")),
                DocumentType = r.GetString(r.GetOrdinal("DocumentType")),
                PositionX_mm = r.GetDouble(r.GetOrdinal("PositionX_mm")),
                PositionY_mm = r.GetDouble(r.GetOrdinal("PositionY_mm")),
                FontSize = r.GetDouble(r.GetOrdinal("FontSize")),
                FontFamily = r.GetString(r.GetOrdinal("FontFamily")),
                IsBold = r.GetBoolean(r.GetOrdinal("IsBold")),
                IsItalic = r.GetBoolean(r.GetOrdinal("IsItalic")),
                TextAlignment = r.GetString(r.GetOrdinal("TextAlignment"))
            });
        return list;
    }

    public List<CommissionMember> GetCommissionMembers()
    {
        var list = new List<CommissionMember>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand("SELECT Id,FullName,Position,Role FROM CommissionMembers WHERE IsActive=1", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new CommissionMember
            {
                Id = r.GetInt32(0), FullName = r.GetString(1),
                Position = r.IsDBNull(2) ? null : r.GetString(2),
                Role = r.GetString(3)
            });
        return list;
    }

    // ===== HELPERS =====
    private T ExecuteScalar<T>(string sql)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return default!;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    // ===== GET ALL =====
    public List<Specialty> GetAllSpecialties()
    {
        var list = new List<Specialty>();
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand("SELECT Id,Code,Name,Qualification,ShortName FROM Specialties WHERE IsActive=1 ORDER BY Code", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Specialty
            {
                Id = r.GetInt32(0), Code = r.GetString(1), Name = r.GetString(2),
                Qualification = r.GetString(3), ShortName = r.IsDBNull(4) ? null : r.GetString(4)
            });
        return list;
    }

    public Specialty? GetSpecialtyById(int id)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand("SELECT Id,Code,Name,Qualification,ShortName,StudyForm,StudyYears FROM Specialties WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new Specialty
        {
            Id = r.GetInt32(0), Code = r.GetString(1), Name = r.GetString(2),
            Qualification = r.IsDBNull(3) ? "" : r.GetString(3),
            ShortName = r.IsDBNull(4) ? null : r.GetString(4),
            StudyForm = r.IsDBNull(5) ? "Очная" : r.GetString(5),
            StudyYears = r.IsDBNull(6) ? 3.10m : r.GetDecimal(6)
        };
    }

    /// <param name="specialtyFilterId">Если задано — предметы этой специальности и общие (SpecialtyId IS NULL).</param>
    public List<Subject> GetAllSubjects(int? specialtyFilterId = null)
    {
        var list = new List<Subject>();
        using var conn = GetConnection(); conn.Open();
        var sql = @"SELECT s.Id,s.Name,s.Course,s.Hours,s.SubjectType,s.IsExam,s.SpecialtyId,
              sp.Code, sp.Name
              FROM Subjects s
              LEFT JOIN Specialties sp ON s.SpecialtyId=sp.Id
              WHERE 1=1";
        if (specialtyFilterId.HasValue)
            sql += " AND (s.SpecialtyId IS NULL OR s.SpecialtyId=@sid)";
        sql += " ORDER BY s.Course,s.Name";
        using var cmd = new SqlCommand(sql, conn);
        if (specialtyFilterId.HasValue) cmd.Parameters.AddWithValue("@sid", specialtyFilterId.Value);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            int? specId = r.IsDBNull(6) ? null : r.GetInt32(6);
            list.Add(new Subject
            {
                Id = r.GetInt32(0), Name = r.GetString(1), Course = r.GetInt32(2),
                Hours = r.IsDBNull(3) ? null : r.GetInt32(3),
                SubjectType = r.GetString(4), IsExam = r.GetBoolean(5),
                SpecialtyId = specId,
                SpecialtyDisplay = FormatSubjectSpecialtyDisplay(specId, r, 7, 8)
            });
        }
        return list;
    }

    /// <summary>Предметы, доступные группе: общие (SpecialtyId IS NULL) или свои по специальности.</summary>
    public List<Subject> GetSubjectsForGroup(int groupId)
    {
        var list = new List<Subject>();
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT s.Id,s.Name,s.Course,s.Hours,s.SubjectType,s.IsExam,s.SpecialtyId,
              sp.Code, sp.Name
              FROM Subjects s
              INNER JOIN Groups g ON g.Id=@gid
              LEFT JOIN Specialties sp ON s.SpecialtyId=sp.Id
              WHERE s.SpecialtyId IS NULL OR s.SpecialtyId=g.SpecialtyId
              ORDER BY s.Course,s.Name", conn);
        cmd.Parameters.AddWithValue("@gid", groupId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            int? specId = r.IsDBNull(6) ? null : r.GetInt32(6);
            list.Add(new Subject
            {
                Id = r.GetInt32(0), Name = r.GetString(1), Course = r.GetInt32(2),
                Hours = r.IsDBNull(3) ? null : r.GetInt32(3),
                SubjectType = r.GetString(4), IsExam = r.GetBoolean(5),
                SpecialtyId = specId,
                SpecialtyDisplay = FormatSubjectSpecialtyDisplay(specId, r, 7, 8)
            });
        }
        return list;
    }

    /// <summary>
    /// Предметы группы, по которым у студента ещё нет записи в Grades (любой тип оценки).
    /// </summary>
    public List<Subject> GetSubjectsAvailableForStudentGrade(int groupId, int studentId)
    {
        var list = new List<Subject>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT s.Id,s.Name,s.Course,s.Hours,s.SubjectType,s.IsExam,s.SpecialtyId,
              sp.Code, sp.Name
              FROM Subjects s
              INNER JOIN Groups g ON g.Id=@gid
              LEFT JOIN Specialties sp ON s.SpecialtyId=sp.Id
              WHERE (s.SpecialtyId IS NULL OR s.SpecialtyId=g.SpecialtyId)
                AND NOT EXISTS (
                    SELECT 1 FROM Grades gr
                    WHERE gr.StudentId=@stud AND gr.SubjectId=s.Id)
              ORDER BY s.Course,s.Name", conn);
        cmd.Parameters.AddWithValue("@gid", groupId);
        cmd.Parameters.AddWithValue("@stud", studentId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            int? specId = r.IsDBNull(6) ? null : r.GetInt32(6);
            list.Add(new Subject
            {
                Id = r.GetInt32(0), Name = r.GetString(1), Course = r.GetInt32(2),
                Hours = r.IsDBNull(3) ? null : r.GetInt32(3),
                SubjectType = r.GetString(4), IsExam = r.GetBoolean(5),
                SpecialtyId = specId,
                SpecialtyDisplay = FormatSubjectSpecialtyDisplay(specId, r, 7, 8)
            });
        }
        return list;
    }

    public bool StudentHasGradeForSubject(int studentId, int subjectId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT COUNT(1) FROM Grades WHERE StudentId=@sid AND SubjectId=@sub", conn);
        cmd.Parameters.AddWithValue("@sid", studentId);
        cmd.Parameters.AddWithValue("@sub", subjectId);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public bool StudentHasGradeForSubjectName(int studentId, string subjectName)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(
            @"SELECT COUNT(1) FROM Grades g
              INNER JOIN Subjects s ON s.Id=g.SubjectId
              WHERE g.StudentId=@sid AND s.Name=@n", conn);
        cmd.Parameters.AddWithValue("@sid", studentId);
        cmd.Parameters.AddWithValue("@n", subjectName);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>Один предмет на пару (название, курс): общий (SpecialtyId NULL) важнее дубликата по специальности.</summary>
    public int DeduplicateSubjectsByNameAndCourse()
    {
        var all = GetAllSubjects();
        var merged = 0;
        foreach (var group in all.GroupBy(s => (s.Name.Trim(), s.Course)).Where(g => g.Count() > 1))
        {
            var keeper = group
                .OrderBy(s => s.SpecialtyId.HasValue ? 1 : 0)
                .ThenBy(s => s.Id)
                .First();
            foreach (var dup in group.Where(s => s.Id != keeper.Id))
            {
                ExecuteNonQuery(
                    @"UPDATE g SET SubjectId=@keep
                      FROM Grades g
                      WHERE g.SubjectId=@dup
                        AND NOT EXISTS (
                            SELECT 1 FROM Grades x
                            WHERE x.StudentId=g.StudentId AND x.SubjectId=@keep)",
                    ("@keep", keeper.Id), ("@dup", dup.Id));
                ExecuteNonQuery("DELETE FROM Grades WHERE SubjectId=@dup", ("@dup", dup.Id));
                DeleteSubject(dup.Id);
                merged++;
            }
        }

        return merged;
    }

    private static string FormatSubjectSpecialtyDisplay(int? specialtyId, SqlDataReader r, int codeOrdinal, int nameOrdinal)
    {
        if (specialtyId == null) return "Общие";
        var code = r.IsDBNull(codeOrdinal) ? null : r.GetString(codeOrdinal);
        var spName = r.IsDBNull(nameOrdinal) ? null : r.GetString(nameOrdinal);
        return (code, spName) switch
        {
            (not null, not null) => $"{code} — {spName}",
            (not null, null) => code,
            (null, not null) => spName,
            _ => "—"
        };
    }

    /// <summary>Поиск специальности по коду или названию (без учёта регистра, пробелы по краям).</summary>
    public Specialty? FindSpecialtyByCodeOrName(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var t = token.Trim();
        foreach (var sp in GetAllSpecialties())
        {
            if (string.Equals(sp.Code, t, StringComparison.OrdinalIgnoreCase)) return sp;
            if (string.Equals(sp.Name, t, StringComparison.OrdinalIgnoreCase)) return sp;
            if (!string.IsNullOrEmpty(sp.ShortName) &&
                string.Equals(sp.ShortName, t, StringComparison.OrdinalIgnoreCase)) return sp;
        }
        return null;
    }

    // ===== INSERT =====
    public int InsertStudent(Student s)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO Students (GroupId,LastName,FirstName,MiddleName,BirthDate,PreviousEducation,PreviousEducationDoc,
              DemoExamParticipantCode,DemoExamScore,DemoExamMaxScore,DemoExamLevel)
              OUTPUT INSERTED.Id
              VALUES (@gid,@ln,@fn,@mn,@bd,@pe,@ped,@dec,@des,@dem,@del)", conn);
        cmd.Parameters.AddWithValue("@gid", s.GroupId);
        cmd.Parameters.AddWithValue("@ln", s.LastName);
        cmd.Parameters.AddWithValue("@fn", s.FirstName);
        cmd.Parameters.AddWithValue("@mn", (object?)s.MiddleName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bd", (object?)s.BirthDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pe", (object?)s.PreviousEducation ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ped", (object?)s.PreviousEducationDoc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dec", (object?)s.DemoExamParticipantCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@des", (object?)s.DemoExamScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dem", s.DemoExamMaxScore > 0 ? s.DemoExamMaxScore : 70);
        cmd.Parameters.AddWithValue("@del", (object?)s.DemoExamLevel ?? DBNull.Value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void InsertGroup(Group g)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO Groups (Name,SpecialtyId,EnrollmentYear,Address,CourseNumber,IsGraduating)
              VALUES (@n,@sid,@y,@a,@c,@grad)", conn);
        cmd.Parameters.AddWithValue("@n", g.Name);
        cmd.Parameters.AddWithValue("@sid", g.SpecialtyId);
        cmd.Parameters.AddWithValue("@y", g.EnrollmentYear);
        cmd.Parameters.AddWithValue("@a", (object?)g.Address ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@c", g.CourseNumber);
        cmd.Parameters.AddWithValue("@grad", g.IsGraduating);
        cmd.ExecuteNonQuery();
    }

    public void InsertSubject(string name, int course, int? hours, string type, int? specialtyId, bool isExam)
    {
        if (ShouldSkipSubjectInsert(name, course, specialtyId))
            return;

        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO Subjects (Name,Course,Hours,SubjectType,SpecialtyId,IsExam)
              VALUES (@n,@c,@h,@t,@sid,@e)", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@c", course);
        cmd.Parameters.AddWithValue("@h", (object?)hours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@t", type);
        cmd.Parameters.AddWithValue("@sid", (object?)specialtyId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@e", isExam);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSubjectSpecialty(int subjectId, int? specialtyId)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE Subjects SET SpecialtyId=@sid WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", subjectId);
        cmd.Parameters.AddWithValue("@sid", (object?)specialtyId ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSubject(Subject subject)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE Subjects
              SET Name=@n, Course=@c, Hours=@h, SubjectType=@t, SpecialtyId=@sid, IsExam=@e
              WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", subject.Id);
        cmd.Parameters.AddWithValue("@n", subject.Name);
        cmd.Parameters.AddWithValue("@c", subject.Course);
        cmd.Parameters.AddWithValue("@h", (object?)subject.Hours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@t", subject.SubjectType);
        cmd.Parameters.AddWithValue("@sid", (object?)subject.SpecialtyId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@e", subject.IsExam);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSubject(int subjectId)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Subjects WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", subjectId);
        cmd.ExecuteNonQuery();
    }

    private bool ShouldSkipSubjectInsert(string name, int course, int? specialtyId)
    {
        var existing = GetAllSubjects()
            .Where(s => string.Equals(s.Name.Trim(), name.Trim(), StringComparison.Ordinal)
                        && s.Course == course)
            .ToList();
        if (existing.Count == 0) return false;
        if (specialtyId == null) return true;
        if (existing.Any(s => s.SpecialtyId == null)) return true;
        return existing.Any(s => s.SpecialtyId == specialtyId);
    }

    public void InsertSpecialty(string code, string name, string shortName)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            "INSERT INTO Specialties (Code,Name,ShortName,Qualification) VALUES (@c,@n,@sn,'')", conn);
        cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@sn", shortName);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSpecialty(Specialty specialty)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE Specialties
              SET Code=@c, Name=@n, ShortName=@sn, Qualification=@q, StudyForm=@sf, StudyYears=@sy
              WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", specialty.Id);
        cmd.Parameters.AddWithValue("@c", specialty.Code);
        cmd.Parameters.AddWithValue("@n", specialty.Name);
        cmd.Parameters.AddWithValue("@sn", (object?)specialty.ShortName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@q", specialty.Qualification ?? "");
        cmd.Parameters.AddWithValue("@sf", specialty.StudyForm ?? "Очная");
        cmd.Parameters.AddWithValue("@sy", specialty.StudyYears);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSpecialty(int specialtyId)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Specialties WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", specialtyId);
        cmd.ExecuteNonQuery();
    }

    public void InsertCommissionMember(string fullName, string? position, string role)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            "INSERT INTO CommissionMembers (FullName,Position,Role) VALUES (@fn,@p,@r)", conn);
        cmd.Parameters.AddWithValue("@fn", fullName);
        cmd.Parameters.AddWithValue("@p", (object?)position ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@r", role);
        cmd.ExecuteNonQuery();
    }

    public void UpdateCommissionMember(CommissionMember member)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"UPDATE CommissionMembers
              SET FullName=@fn, Position=@p, Role=@r
              WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", member.Id);
        cmd.Parameters.AddWithValue("@fn", member.FullName);
        cmd.Parameters.AddWithValue("@p", (object?)member.Position ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@r", member.Role);
        cmd.ExecuteNonQuery();
    }

    public void DeleteCommissionMember(int memberId)
    {
        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand("DELETE FROM CommissionMembers WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", memberId);
        cmd.ExecuteNonQuery();
    }

    public void InsertGrade(int studentId, int subjectId, int grade, string gradeType) =>
        InsertGrade(studentId, subjectId, grade, gradeType, null, null);

    public void InsertGrade(
        int studentId,
        int subjectId,
        int grade,
        string gradeType,
        string? practicePlace,
        string? practiceTrainingMeans)
    {
        if (StudentHasGradeForSubject(studentId, subjectId))
            throw new InvalidOperationException("У этого студента уже есть оценка по выбранному предмету.");

        using var conn = GetConnection(); conn.Open();
        using var cmd = new SqlCommand(
            @"INSERT INTO Grades (StudentId,SubjectId,Grade,GradeType,PracticePlace,PracticeTrainingMeans)
              VALUES (@sid,@subid,@g,@gt,@pp,@ptm)", conn);
        cmd.Parameters.AddWithValue("@sid", studentId);
        cmd.Parameters.AddWithValue("@subid", subjectId);
        cmd.Parameters.AddWithValue("@g", grade);
        cmd.Parameters.AddWithValue("@gt", gradeType);
        cmd.Parameters.AddWithValue("@pp", string.IsNullOrWhiteSpace(practicePlace) ? DBNull.Value : practicePlace.Trim());
        cmd.Parameters.AddWithValue("@ptm", string.IsNullOrWhiteSpace(practiceTrainingMeans) ? DBNull.Value : practiceTrainingMeans.Trim());
        cmd.ExecuteNonQuery();
    }

    public void DeleteStudentGrade(int gradeId, int studentId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand("DELETE FROM Grades WHERE Id=@id AND StudentId=@sid", conn);
        cmd.Parameters.AddWithValue("@id", gradeId);
        cmd.Parameters.AddWithValue("@sid", studentId);
        if (cmd.ExecuteNonQuery() == 0)
            throw new InvalidOperationException("Оценка не найдена или уже удалена.");
    }

    /// <summary>Выполняет SQL без возврата набора строк; возвращает число затронутых строк.</summary>
    public int ExecuteNonQuery(string sql) => ExecuteNonQuery(sql, []);

    public int ExecuteNonQuery(string sql, params (string Name, object? Value)[] parameters)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    public T ExecuteScalarPublic<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return default!;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public string GetDatabaseSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Состояние базы:");
        sb.AppendLine($"• Студенты: {ExecuteScalar<int>("SELECT COUNT(*) FROM Students")}");
        sb.AppendLine($"• Группы: {ExecuteScalar<int>("SELECT COUNT(*) FROM Groups")}");
        sb.AppendLine($"• Специальности: {ExecuteScalar<int>("SELECT COUNT(*) FROM Specialties")}");
        sb.AppendLine($"• Предметы: {ExecuteScalar<int>("SELECT COUNT(*) FROM Subjects")}");
        sb.AppendLine($"• Оценки: {ExecuteScalar<int>("SELECT COUNT(*) FROM Grades")}");
        sb.AppendLine($"• Дипломы: {ExecuteScalar<int>("SELECT COUNT(*) FROM Diplomas")}");
        sb.AppendLine($"• История печати: {ExecuteScalar<int>("SELECT COUNT(*) FROM PrintHistory")}");
        return sb.ToString();
    }
}

public class GroupGradeStats
{
    public int Total { get; set; }
    public int ExcellentCount { get; set; }
    public int GoodCount { get; set; }
    public double SuccessRate { get; set; }
    public double ExcellentPercent { get; set; }
    public double GoodPercent { get; set; }
}

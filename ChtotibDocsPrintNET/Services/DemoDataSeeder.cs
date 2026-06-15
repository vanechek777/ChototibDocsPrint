using ChtotibDocsPrintNET.Data;

using ChtotibDocsPrintNET.Models;



namespace ChtotibDocsPrintNET.Services;



/// <summary>Демо-данные с корректной Unicode (в отличие от sqlcmd без UTF-8).</summary>

public static class DemoDataSeeder

{

    private const string DemoAddress = "г. Чита, ул. Бабушкина, 109";

    private const string ExcludeGroup = "ИСиП-22-1п";

    /// <summary>Пилотная группа: не добавляем 25 студентов, но дозаполняем данные существующим.</summary>
    private const string PilotGroup = "ИСиП-22-1п";

    private const int TargetStudentsPerGroup = 25;



    public static readonly string[] PrimaryGroupNames =

    [

        "ИСиП-22-2в", "ИСиП-22-3а", "ИСиП-22-4к", "АРХ-22-1", "СиС-22-1",

    ];



    private static readonly (string Code, string Name, string Short, string Qualification)[] Specialties =

    [

        ("09.02.06", "Сетевое и системное администрирование", "СиС", "Системный администратор"),

        ("09.02.07", "Информационные системы и программирование", "ИСиП", "Программист"),

        ("07.02.01", "Архитектура", "АРХ", "Архитектор"),

    ];



    private static readonly (string Name, int Course, int Hours, string Type, bool Exam, string? SpecialtyCode)[] SubjectCatalog =

    [

        ("Математика", 1, 144, "Общеобразовательный", true, null),

        ("Русский язык", 1, 108, "Общеобразовательный", false, null),

        ("История", 1, 72, "Общеобразовательный", false, null),

        ("Физическая культура", 1, 144, "Общеобразовательный", false, null),

        ("Информатика", 1, 180, "Профессиональный", true, null),

        ("Иностранный язык", 2, 72, "Общеобразовательный", false, null),

        ("Основы экономики", 2, 54, "Общеобразовательный", false, null),

        ("Основы алгоритмизации и программирования", 2, 216, "Профессиональный", true, "09.02.07"),

        ("Архитектура и организация компьютерных систем", 2, 108, "Профессиональный", false, "09.02.07"),

        ("Основы сетевых технологий", 2, 144, "Профессиональный", true, "09.02.06"),

        ("История архитектуры", 2, 108, "Профессиональный", false, "07.02.01"),

        ("Основы архитектурного проектирования", 2, 180, "Профессиональный", false, "07.02.01"),

        ("Базы данных", 3, 144, "Профессиональный", true, "09.02.07"),

        ("Разработка программных модулей", 3, 180, "Профессиональный", true, "09.02.07"),

        ("Разработка веб-приложений", 3, 144, "Профессиональный", true, "09.02.07"),

        ("Администрирование баз данных", 3, 108, "Профессиональный", true, "09.02.07"),

        ("Защита информации", 3, 108, "Профессиональный", true, "09.02.06"),

        ("Строительные материалы", 3, 72, "Профессиональный", false, "07.02.01"),

        ("Учебная практика", 3, 108, "Практика", false, null),

        ("Курсовая работа по профессии", 4, 72, "Курсовая работа", false, "09.02.07"),

        ("Курсовой проект по архитектуре", 4, 72, "Курсовая работа", false, "07.02.01"),

        ("Производственная практика", 4, 216, "Практика", false, null),

        ("Демонстрационный экзамен", 4, 36, "Профессиональный", true, null),

        ("Системное администрирование", 4, 144, "Профессиональный", true, "09.02.06"),

        ("Проектирование зданий", 4, 180, "Профессиональный", true, "07.02.01"),

    ];



    private static readonly string[] LastNames =

    [

        "Иванов", "Петров", "Сидоров", "Козлов", "Новиков", "Морозов", "Волков", "Соколов", "Лебедев", "Кузнецов",

        "Попов", "Васильев", "Смирнов", "Михайлов", "Фёдоров", "Андреев", "Алексеев", "Романов", "Орлов", "Семёнов",

        "Егоров", "Павлов", "Голубев", "Борисов", "Яковлев", "Бахметьев", "Григорьев", "Зайцев", "Медведев", "Никитин",

    ];



    private static readonly (string Fn, string Mn)[] MaleNames =

    [

        ("Алексей", "Сергеевич"), ("Дмитрий", "Андреевич"), ("Максим", "Игоревич"), ("Иван", "Петрович"),

        ("Артём", "Николаевич"), ("Кирилл", "Олегович"), ("Никита", "Владимирович"), ("Егор", "Дмитриевич"),

        ("Даниил", "Александрович"), ("Тимофей", "Романович"), ("Матвей", "Евгеньевич"), ("Степан", "Олегович"),

        ("Глеб", "Викторович"), ("Филипп", "Юрьевич"), ("Павел", "Константинович"), ("Руслан", "Тимурович"),

        ("Владислав", "Борисович"), ("Георгий", "Михайлович"), ("Семён", "Антонович"), ("Лев", "Станиславович"),

        ("Марк", "Вадимович"), ("Ярослав", "Геннадьевич"), ("Родион", "Павлович"), ("Всеволод", "Аркадьевич"),

        ("Богдан", "Филиппович"), ("Тимур", "Рашидович"), ("Олег", "Валерьевич"), ("Илья", "Семёнович"),

        ("Виктор", "Геннадьевич"), ("Антон", "Игоревич"),

    ];



    private static readonly (string Fn, string Mn)[] FemaleNames =

    [

        ("Анна", "Сергеевна"), ("Мария", "Андреевна"), ("Елена", "Игоревна"), ("Ольга", "Петровна"),

        ("Дарья", "Николаевна"), ("Виктория", "Олеговна"), ("Полина", "Владимировна"), ("София", "Дмитриевна"),

        ("Алина", "Александровна"), ("Ксения", "Романовна"), ("Валерия", "Евгеньевна"), ("Юлия", "Ильинична"),

        ("Екатерина", "Викторовна"), ("Наталья", "Юрьевна"), ("Татьяна", "Константиновна"), ("Ирина", "Тимуровна"),

        ("Вероника", "Борисовна"), ("Анастасия", "Михайловна"), ("Людмила", "Антоновна"), ("Светлана", "Станиславовна"),

        ("Кристина", "Вадимовна"), ("Арина", "Геннадьевна"), ("Милана", "Павловна"), ("Диана", "Аркадьевна"),

        ("Варвара", "Филипповна"), ("Карина", "Руслановна"), ("Алёна", "Максимовна"), ("Ева", "Денисовна"),

        ("Злата", "Артёмовна"), ("Ульяна", "Николаевна"),

    ];



    public static DemoSeedResult Run(DatabaseService db)

    {

        EnsureSpecialties(db);

        EnsureSubjectCatalog(db);

        var result = new DemoSeedResult();

        result.SubjectsMerged = db.DeduplicateSubjectsByNameAndCourse();

        SyncDemoStudentQualifications(db);

        var groupsToFill = CollectGroupsToFill(db);



        foreach (var group in groupsToFill)

        {

            var specialtyCode = ResolveSpecialtyCode(group.Name);

            var sp = db.FindSpecialtyByCodeOrName(specialtyCode)

                ?? throw new InvalidOperationException($"Специальность {specialtyCode} не найдена.");

            var groupId = EnsureGroup(db, group.Name, sp.Id);

            var qual = QualificationForGroup(group.Name, sp);

            var slug = GroupSlug(group.Name);

            var existing = CountActiveStudents(db, groupId);

            var toAdd = Math.Max(0, TargetStudentsPerGroup - existing);



            for (var i = 0; i < toAdd; i++)

            {

                var idx = existing + i + 1;

                var reg = $"DEMO-{slug}-{idx:00}";

                if (StudentExistsByReg(db, reg)) continue;



                var female = idx % 3 == 0;

                var nameIdx = (idx - 1) % LastNames.Length;

                var (fn, mn) = female ? FemaleNames[nameIdx] : MaleNames[nameIdx];

                var studentId = db.InsertStudent(new Student

                {

                    GroupId = groupId,

                    LastName = LastNames[nameIdx],

                    FirstName = fn,

                    MiddleName = mn,

                    BirthDate = new DateTime(2005, (idx % 12) + 1, (idx % 28) + 1),

                    PreviousEducation = null,

                    PreviousEducationDoc = $"Аттестат серия АА № {200000 + idx}",

                    DemoExamParticipantCode = $"1.{(idx % 3) + 1}-2023-2025",

                    DemoExamScore = 24m + (idx % 16) + (idx % 5) * 0.14m,

                    DemoExamMaxScore = 70,

                    DemoExamLevel = "профильный уровень",

                });



                var honor = group.Name == "ИСиП-22-3а" && idx <= 3;

                db.ExecuteNonQuery(

                    @"UPDATE Students SET RegistrationNumber=@r, IsGraduated=1, Qualification=@q WHERE Id=@id",

                    ("@r", reg), ("@q", qual), ("@id", studentId));



                db.UpdateStudentAndDiplomaBlank(

                    studentId, reg, qual, "107724", $"{100000 + studentId:D6}",

                    honor ? "С отличием" : "Обычный",

                    null, $"Аттестат серия АА № {200000 + idx}",

                    new DateTime(2026, 6, 22),

                    $"1.{(idx % 3) + 1}-2023-2025",

                    24m + (idx % 16) + (idx % 5) * 0.14m,

                    70,

                    "профильный уровень");



                EnsureGradesForStudent(db, studentId, groupId, group.Name, idx, honor);

                result.StudentsAdded++;

            }



            BackfillGradesForGroup(db, groupId, group.Name);

            result.DemoExamBackfilled += BackfillDemoExamForGroup(db, groupId, group.Name);

            BackfillPreviousEducationDocForGroup(db, groupId, group.Name);

            if (!result.Groups.Contains(group.Name))

                result.Groups.Add(group.Name);

        }



        result.PilotGroupStudentsBackfilled = BackfillPilotGroupIsip221p(db);



        return result;

    }



    /// <summary>Дозаполняет оценки, диплом, демоэкзамен и аттестат для 4 студентов ИСиП-22-1п.</summary>

    private static int BackfillPilotGroupIsip221p(DatabaseService db)

    {

        var group = db.GetGroups(false, null, PilotGroup).FirstOrDefault(g => g.Name == PilotGroup);

        if (group == null) return 0;



        var sp = db.FindSpecialtyByCodeOrName(ResolveSpecialtyCode(PilotGroup));

        var qual = QualificationForGroup(PilotGroup, sp ?? new Specialty { Qualification = "Программист" });

        var students = db.GetStudentsByGroup(group.Id, null);

        var idx = 0;

        foreach (var s in students)

        {

            idx++;

            var full = db.GetStudentById(s.Id);

            if (full == null) continue;



            var reg = full.RegistrationNumber?.Trim();

            if (string.IsNullOrEmpty(reg))

                reg = $"DEMO-ISIP221P-{idx:00}";



            var (code, score) = DemoExamValuesForIndex(idx);

            var att = $"Аттестат серия АА № {200000 + idx}";



            db.UpdateStudentAndDiplomaBlank(

                full.Id, reg, qual, "107724", $"{100000 + full.Id:D6}",

                "Обычный", null, att,

                new DateTime(2026, 6, 22),

                code, score, 70, "профильный уровень");



            db.ExecuteNonQuery(

                @"UPDATE Students SET IsGraduated=1, PreviousEducation=NULL WHERE Id=@id",

                ("@id", full.Id));



            EnsureGradesForStudent(db, full.Id, group.Id, PilotGroup, idx, honor: false);

        }



        BackfillGradesForGroup(db, group.Id, PilotGroup);

        BackfillPreviousEducationDocForGroup(db, group.Id, PilotGroup);

        return students.Count;

    }



    private static List<Group> CollectGroupsToFill(DatabaseService db)

    {

        var map = new Dictionary<string, Group>(StringComparer.Ordinal);

        foreach (var name in PrimaryGroupNames)

        {

            var g = db.GetGroups(false, null, name).FirstOrDefault(x => x.Name == name);

            if (g != null) map[name] = g;

            else map[name] = new Group { Name = name };

        }



        foreach (var g in db.GetGroups(false, null, null))

        {

            if (string.Equals(g.Name, ExcludeGroup, StringComparison.Ordinal)) continue;

            map[g.Name] = g;

        }



        return map.Values.OrderBy(g => g.Name).ToList();

    }



    private static void BackfillGradesForGroup(DatabaseService db, int groupId, string groupName)

    {

        var students = db.GetStudentsByGroup(groupId, null);

        var idx = 0;

        foreach (var s in students)

        {

            idx++;

            var honor = groupName == "ИСиП-22-3а" && idx <= 3;

            EnsureGradesForStudent(db, s.Id, groupId, groupName, idx, honor);

        }

    }



    private static int BackfillDemoExamForGroup(DatabaseService db, int groupId, string groupName)

    {

        if (string.Equals(groupName, ExcludeGroup, StringComparison.Ordinal))

            return 0;



        var students = db.GetStudentsByGroup(groupId, null);

        var filled = 0;

        var idx = 0;

        foreach (var s in students)

        {

            idx++;

            var full = db.GetStudentById(s.Id);

            if (full == null) continue;

            if (!string.IsNullOrWhiteSpace(full.DemoExamParticipantCode) && full.DemoExamScore.HasValue)

                continue;



            var studentIdx = TryParseDemoStudentIndex(full.RegistrationNumber) ?? idx;

            var (code, score) = DemoExamValuesForIndex(studentIdx);

            db.BackfillStudentDemoExamIfEmpty(s.Id, code, score, 70, "профильный уровень");

            filled++;

        }



        return filled;

    }



    private static (string Code, decimal Score) DemoExamValuesForIndex(int idx) =>

        ($"1.{(idx % 3) + 1}-2023-2025", 24m + (idx % 16) + (idx % 5) * 0.14m);



    private static void BackfillPreviousEducationDocForGroup(DatabaseService db, int groupId, string groupName)

    {

        if (string.Equals(groupName, ExcludeGroup, StringComparison.Ordinal))

            return;



        var students = db.GetStudentsByGroup(groupId, null);

        foreach (var s in students)

        {

            var full = db.GetStudentById(s.Id);

            if (full == null) continue;

            var doc = full.PreviousEducationDoc?.Trim();

            if (!string.IsNullOrEmpty(doc) && !doc.Contains("МБОУ", StringComparison.OrdinalIgnoreCase)

                && !doc.Contains("СОШ", StringComparison.OrdinalIgnoreCase))

                continue;



            var idx = TryParseDemoStudentIndex(full.RegistrationNumber) ?? s.Id % 100;

            var att = $"Аттестат серия АА № {200000 + idx}";

            db.ExecuteNonQuery(

                @"UPDATE Students SET PreviousEducationDoc=@doc,

                  PreviousEducation=CASE WHEN PreviousEducation LIKE N'%МБОУ%' OR PreviousEducation LIKE N'%СОШ%'

                    THEN NULL ELSE PreviousEducation END

                  WHERE Id=@id",

                ("@doc", att), ("@id", full.Id));

        }

    }



    private static int? TryParseDemoStudentIndex(string? registrationNumber)

    {

        if (string.IsNullOrWhiteSpace(registrationNumber)

            || !registrationNumber.StartsWith("DEMO-", StringComparison.Ordinal))

            return null;

        var lastDash = registrationNumber.LastIndexOf('-');

        if (lastDash < 0 || lastDash >= registrationNumber.Length - 1)

            return null;

        return int.TryParse(registrationNumber[(lastDash + 1)..], out var n) && n > 0 ? n : null;

    }



    private static void EnsureGradesForStudent(DatabaseService db, int studentId, int groupId, string groupName, int studentIndex, bool honor)

    {

        var subjects = db.GetSubjectsForGroup(groupId);

        var gradeVal = honor ? 5 : studentIndex % 5 == 0 ? 3 : studentIndex % 4 == 0 ? 4 : 5;



        foreach (var sub in subjects)

        {

            if (db.StudentHasGradeForSubject(studentId, sub.Id)) continue;

            if (db.StudentHasGradeForSubjectName(studentId, sub.Name)) continue;

            if (!SubjectAppliesToGroup(sub.Name, groupName)) continue;

            db.InsertGrade(studentId, sub.Id, gradeVal, ResolveGradeType(sub));

        }

    }



    private static bool SubjectAppliesToGroup(string subjectName, string groupName)

    {

        if (string.Equals(subjectName, "Демонстрационный экзамен", StringComparison.OrdinalIgnoreCase))

            return false;



        if (groupName.StartsWith("АРХ", StringComparison.Ordinal))

            return !subjectName.Contains("веб", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("баз данных", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("программн", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("сетев", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("защит", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("системн", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("архитект", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("строительн", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("проектирован", StringComparison.OrdinalIgnoreCase)

                   || subjectName is "Математика" or "Русский язык" or "История" or "Физическая культура"

                       or "Информатика" or "Иностранный язык" or "Основы экономики"

                       or "Учебная практика" or "Производственная практика" or "Демонстрационный экзамен";



        if (groupName.StartsWith("СиС", StringComparison.Ordinal))

            return !subjectName.Contains("веб", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("архитект", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("строительн", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("проектирован", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("сет", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("защит", StringComparison.OrdinalIgnoreCase)

                   || subjectName.Contains("системн", StringComparison.OrdinalIgnoreCase)

                   || subjectName is "Математика" or "Русский язык" or "История" or "Физическая культура"

                       or "Информатика" or "Иностранный язык" or "Основы экономики"

                       or "Учебная практика" or "Производственная практика" or "Демонстрационный экзамен";



        if (groupName == "ИСиП-22-2в")

            return !subjectName.Contains("Администрирование баз данных", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("архитектур", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("строительн", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("сетев", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("защит", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("системн", StringComparison.OrdinalIgnoreCase);



        if (groupName == "ИСиП-22-3а")

            return !subjectName.Contains("веб", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("архитектур", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("строительн", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("сетев", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("защит", StringComparison.OrdinalIgnoreCase)

                   && !subjectName.Contains("системн", StringComparison.OrdinalIgnoreCase);



        return !subjectName.Contains("веб", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("Администрирование баз данных", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("архитектур", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("строительн", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("сетев", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("защит", StringComparison.OrdinalIgnoreCase)

               && !subjectName.Contains("системн", StringComparison.OrdinalIgnoreCase);

    }



    private static string ResolveGradeType(Subject sub)

    {

        if (sub.SubjectType.Contains("Курсовая", StringComparison.OrdinalIgnoreCase)

            || sub.Name.Contains("Курсов", StringComparison.OrdinalIgnoreCase))

            return "Курсовая";

        if (sub.SubjectType.Contains("Практика", StringComparison.OrdinalIgnoreCase)

            || sub.Name.Contains("практика", StringComparison.OrdinalIgnoreCase))

            return "Практика";

        if (sub.IsExam || sub.Name.Contains("экзамен", StringComparison.OrdinalIgnoreCase))

            return "Экзамен";

        if (sub.SubjectType.Contains("Общеобразовательный", StringComparison.OrdinalIgnoreCase) && !sub.IsExam)

            return "Зачёт";

        return "Итоговая";

    }



    private static string QualificationForGroup(string groupName, Specialty sp) => groupName switch

    {

        "ИСиП-22-2в" => "Веб-дизайнер",

        "ИСиП-22-3а" => "Администратор баз данных",

        "ИСиП-22-4к" => "Программист",

        "ИСиП-22-1п" => "Программист",

        "СиС-22-1" => "Системный администратор",

        "АРХ-22-1" => "Архитектор",

        _ => sp.Qualification ?? "",

    };



    private static string ResolveSpecialtyCode(string groupName) => groupName switch

    {

        "АРХ-22-1" => "07.02.01",

        "СиС-22-1" => "09.02.06",

        _ when groupName.StartsWith("ИСиП", StringComparison.Ordinal) => "09.02.07",

        _ => "09.02.07",

    };



    public static void RemoveCorruptedDemoData(DatabaseService db)

    {

        db.ExecuteNonQuery("DELETE FROM Grades WHERE StudentId IN (SELECT Id FROM Students WHERE RegistrationNumber LIKE 'DEMO-%')");

        db.ExecuteNonQuery("DELETE FROM Diplomas WHERE StudentId IN (SELECT Id FROM Students WHERE RegistrationNumber LIKE 'DEMO-%')");

        db.ExecuteNonQuery("DELETE FROM Students WHERE RegistrationNumber LIKE 'DEMO-%'");

    }



    public static void RepairSpecialtyEncoding(DatabaseService db) => EnsureSpecialties(db);



    private static void EnsureSpecialties(DatabaseService db)

    {

        foreach (var (code, name, shortName, qualification) in Specialties)

        {

            var sp = db.FindSpecialtyByCodeOrName(code);

            if (sp == null)

            {

                db.InsertSpecialty(code, name, shortName);

                sp = db.FindSpecialtyByCodeOrName(code)

                    ?? throw new InvalidOperationException($"Не удалось создать специальность {code}.");

            }



            if (string.Equals(sp.Name, name, StringComparison.Ordinal)

                && string.Equals(sp.ShortName, shortName, StringComparison.Ordinal)

                && string.Equals(sp.Qualification, qualification, StringComparison.Ordinal))

                continue;



            sp.Name = name;

            sp.ShortName = shortName;

            sp.Qualification = qualification;

            db.UpdateSpecialty(sp);

        }

    }



    private static void SyncDemoStudentQualifications(DatabaseService db)

    {

        db.ExecuteNonQuery(

            @"UPDATE s SET Qualification = CASE g.Name

                WHEN N'ИСиП-22-2в' THEN N'Веб-дизайнер'

                WHEN N'ИСиП-22-3а' THEN N'Администратор баз данных'

                WHEN N'ИСиП-22-4к' THEN N'Программист'

                WHEN N'СиС-22-1' THEN N'Системный администратор'

                WHEN N'АРХ-22-1' THEN N'Архитектор'

                ELSE sp.Qualification END

              FROM Students s

              INNER JOIN Groups g ON g.Id = s.GroupId

              INNER JOIN Specialties sp ON sp.Id = g.SpecialtyId

              WHERE s.RegistrationNumber LIKE 'DEMO-%' OR g.Name IN (N'ИСиП-22-2в', N'ИСиП-22-3а', N'ИСиП-22-4к', N'АРХ-22-1', N'СиС-22-1')");

    }



    private static void EnsureSubjectCatalog(DatabaseService db)

    {

        var existing = db.GetAllSubjects();

        foreach (var seed in SubjectCatalog)

        {

            int? specId = null;

            if (!string.IsNullOrEmpty(seed.SpecialtyCode))

            {

                var sp = db.FindSpecialtyByCodeOrName(seed.SpecialtyCode);

                specId = sp?.Id;

            }



            var sub = existing.FirstOrDefault(s =>

                string.Equals(s.Name, seed.Name, StringComparison.Ordinal)

                && s.Course == seed.Course);

            if (sub != null) continue;



            db.InsertSubject(seed.Name, seed.Course, seed.Hours, seed.Type, specId, seed.Exam);

            existing = db.GetAllSubjects();

        }

    }



    private static int EnsureGroup(DatabaseService db, string name, int specialtyId)

    {

        var found = db.GetGroups(false, null, name).FirstOrDefault(g => g.Name == name);

        if (found != null) return found.Id;



        db.InsertGroup(new Group

        {

            Name = name,

            SpecialtyId = specialtyId,

            EnrollmentYear = 2022,

            CourseNumber = 4,

            Address = DemoAddress,

            IsGraduating = true,

        });

        return db.GetGroups(false, null, name).First(g => g.Name == name).Id;

    }



    private static int CountActiveStudents(DatabaseService db, int groupId) =>

        db.ExecuteScalarPublic<int>(

            "SELECT COUNT(*) FROM Students WHERE GroupId=@g AND IsExpelled=0",

            ("@g", groupId));



    private static bool StudentExistsByReg(DatabaseService db, string reg) =>

        db.ExecuteScalarPublic<int>(

            "SELECT COUNT(*) FROM Students WHERE RegistrationNumber=@r",

            ("@r", reg)) > 0;



    private static string GroupSlug(string groupName) =>

        groupName.Replace("ИСиП", "ISIP", StringComparison.Ordinal)

            .Replace("СиС", "SIS", StringComparison.Ordinal)

            .Replace("АРХ", "ARH", StringComparison.Ordinal)

            .Replace("-", "", StringComparison.Ordinal);

}



public sealed class DemoSeedResult

{

    public List<string> Groups { get; } = [];

    public int StudentsAdded { get; set; }

    public int DemoExamBackfilled { get; set; }

    public int SubjectsMerged { get; set; }

    public int PilotGroupStudentsBackfilled { get; set; }



    public string BuildSummary(DatabaseService db)

    {

        var lines = new List<string>

        {

            $"Обработано групп: {Groups.Count}",

            $"Добавлено студентов: {StudentsAdded}",

            $"Дозаполнено демоэкзаменов: {DemoExamBackfilled}",

            $"Объединено дубликатов предметов: {SubjectsMerged}",

            $"Дозаполнено студентов ИСиП-22-1п: {PilotGroupStudentsBackfilled}",

            "",

            "Целевые группы (по 25 студентов; ИСиП-22-1п — только дозаполнение 4 существующих):",

        };

        foreach (var name in DemoDataSeeder.PrimaryGroupNames)

        {

            var g = db.GetGroups(false, null, name).FirstOrDefault(x => x.Name == name);

            lines.Add(g != null

                ? $"  {name}: {g.StudentCount} студ."

                : $"  {name}: не создана");

        }

        var demoStudents = db.ExecuteScalarPublic<int>(

            "SELECT COUNT(*) FROM Students WHERE RegistrationNumber LIKE 'DEMO-%' AND IsExpelled=0");

        var diplomas = db.ExecuteScalarPublic<int>(

            @"SELECT COUNT(*) FROM Diplomas d

              INNER JOIN Students s ON s.Id=d.StudentId

              WHERE s.RegistrationNumber LIKE 'DEMO-%'");

        lines.Add("");

        lines.Add($"Всего DEMO-студентов: {demoStudents}");

        lines.Add($"Записей дипломов (серия/номер): {diplomas}");

        lines.Add("");

        lines.Add("Данные диплома и демоэкзамена — в карточке студента (вкладка «Студенты» → открыть ФИО).");

        return string.Join(Environment.NewLine, lines);

    }

}



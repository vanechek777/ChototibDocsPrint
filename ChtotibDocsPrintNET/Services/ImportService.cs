using System.Globalization;
using System.IO;
using System.Windows;
using ClosedXML.Excel;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Views.Dialogs;

namespace ChtotibDocsPrintNET.Services;

public static class ImportService
{
    /// <summary>Импорт групп из Excel/CSV. Колонки: Название, Год набора, Корпус, Выпускная (да/1), Специальность (код или название, необязательно)</summary>
    public static string? ImportGroups()
    {
        var path = PickFile(ImportDataKind.Groups);
        if (path == null) return null;
        try
        {
            return ImportGroupsFromFile(path);
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка импорта: {ex.Message}"); return null; }
    }

    public static string ImportGroupsFromFile(string path)
    {
            var rows = ReadFile(path);
            var db = DatabaseService.Instance;
            int imported = 0;
            foreach (var row in rows)
            {
                if (row.Count < 2) continue;
                var specialtyId = 1;
                if (row.Count > 4 && !string.IsNullOrWhiteSpace(row[4]))
                {
                    var spec = db.FindSpecialtyByCodeOrName(row[4].Trim());
                    if (spec != null) specialtyId = spec.Id;
                }
                var g = new Group
                {
                    Name = row[0],
                    EnrollmentYear = int.TryParse(row[1], out int y) ? y : DateTime.Now.Year,
                    Address = row.Count > 2 ? row[2] : null,
                    IsGraduating = row.Count > 3 && (row[3] == "1" || row[3].Equals("да", StringComparison.OrdinalIgnoreCase)),
                    SpecialtyId = specialtyId,
                    CourseNumber = 1
                };
                g.CourseNumber = Math.Clamp(DateTime.Now.Year - g.EnrollmentYear + (DateTime.Now.Month >= 9 ? 1 : 0), 1, 5);
                db.InsertGroup(g);
                imported++;
            }
            return $"Импортировано групп: {imported}";
    }

    /// <summary>Импорт студентов. Колонки: Фамилия, Имя, Отчество, Дата рождения, ID группы</summary>
    public static string? ImportStudents()
    {
        var path = PickFile(ImportDataKind.Students);
        if (path == null) return null;
        try
        {
            return ImportStudentsFromFile(path);
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка импорта: {ex.Message}"); return null; }
    }

    public static string ImportStudentsFromFile(string path)
    {
            var rows = ReadFile(path);
            var db = DatabaseService.Instance;
            int imported = 0;
            foreach (var row in rows)
            {
                if (row.Count < 2) continue;
                var s = new Student
                {
                    LastName = row[0],
                    FirstName = row[1],
                    MiddleName = row.Count > 2 && !string.IsNullOrWhiteSpace(row[2]) ? row[2] : null,
                    BirthDate = row.Count > 3 && TryParseImportDate(row[3], out var bd) ? bd : null,
                    GroupId = row.Count > 4 && int.TryParse(row[4], out int gid) ? gid : 1,
                    DemoExamParticipantCode = row.Count > 5 && !string.IsNullOrWhiteSpace(row[5]) ? row[5].Trim() : null,
                    DemoExamScore = row.Count > 6 && TryParseImportDecimal(row[6], out var score) ? score : null,
                    DemoExamMaxScore = row.Count > 7 && int.TryParse(row[7], out int max) && max > 0 ? max : 70,
                    DemoExamLevel = row.Count > 8 && !string.IsNullOrWhiteSpace(row[8]) ? row[8].Trim() : null,
                };
                var studentId = db.InsertStudent(s);
                if (row.Count > 9 && TryParseImportDate(row[9], out var issueDate))
                {
                    db.UpdateStudentAndDiplomaBlank(
                        studentId, null, null, null, null, null, null, null, issueDate,
                        s.DemoExamParticipantCode, s.DemoExamScore, s.DemoExamMaxScore, s.DemoExamLevel);
                }
                imported++;
            }
            return $"Импортировано студентов: {imported}";
    }

    private static bool TryParseImportDate(string? text, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return DateTime.TryParse(text.Trim(), CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out date)
               || DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryParseImportDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var t = text.Trim().Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// Импорт предметов. Рекомендуемый формат (первая строка — заголовок): Наименование, Специальность, Курс, Часы, Тип.
    /// Во второй колонке: код или название специальности, либо «Все» — общий предмет для всех специальностей (одна запись, SpecialtyId пустой).
    /// Устаревший формат без специальности: Наименование, Курс (1–5), Часы, Тип — если во второй колонке число курса и специальность с таким кодом не найдена.
    /// </summary>
    public static string? ImportSubjects()
    {
        var path = PickFile(ImportDataKind.Subjects);
        if (path == null) return null;
        try
        {
            return ImportSubjectsFromFile(path);
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка импорта: {ex.Message}"); return null; }
    }

    public static string ImportSubjectsFromFile(string path)
    {
            var rows = ReadFile(path);
            var db = DatabaseService.Instance;
            int imported = 0;
            var warn = new List<string>();
            foreach (var row in rows)
            {
                if (!TryParseSubjectImportRow(row, db, out var name, out var course, out var hours, out var type, out var specId, out var err))
                {
                    if (err != null) warn.Add($"{name}: {err}");
                    continue;
                }
                db.InsertSubject(name, course, hours, type, specId, false);
                imported++;
            }
            var msg = $"Импортировано предметов: {imported}";
            if (warn.Count > 0) msg += "\n\n" + string.Join("\n", warn.Take(20)) + (warn.Count > 20 ? $"\n… и ещё {warn.Count - 20}" : "");
            return msg;
    }

    private static bool IsAllSpecialtiesToken(string s)
    {
        var t = s.Trim();
        return t.Equals("Все", StringComparison.OrdinalIgnoreCase)
               || t.Equals("All", StringComparison.OrdinalIgnoreCase);
    }

    /// <returns>false и err==null — пустая строка, пропуск без предупреждения.</returns>
    private static bool TryParseSubjectImportRow(
        List<string> row, DatabaseService db,
        out string name, out int course, out int? hours, out string type, out int? specialtyId, out string? err)
    {
        name = "";
        course = 1;
        hours = null;
        type = "Общеобразовательный";
        specialtyId = null;
        err = null;
        if (row.Count < 1 || string.IsNullOrWhiteSpace(row[0])) return false;
        name = row[0].Trim();
        if (row.Count < 2 || string.IsNullOrWhiteSpace(row[1]))
        {
            return true;
        }

        var col1 = row[1].Trim();
        if (IsAllSpecialtiesToken(col1))
        {
            specialtyId = null;
            course = row.Count > 2 && int.TryParse(row[2].Trim(), out int c0) ? c0 : 1;
            hours = row.Count > 3 && int.TryParse(row[3].Trim(), out int h0) ? h0 : null;
            type = row.Count > 4 && !string.IsNullOrWhiteSpace(row[4]) ? row[4].Trim() : "Общеобразовательный";
            return true;
        }

        var spec = db.FindSpecialtyByCodeOrName(col1);
        if (spec != null)
        {
            specialtyId = spec.Id;
            course = row.Count > 2 && int.TryParse(row[2].Trim(), out int c1) ? c1 : 1;
            hours = row.Count > 3 && int.TryParse(row[3].Trim(), out int h1) ? h1 : null;
            type = row.Count > 4 && !string.IsNullOrWhiteSpace(row[4]) ? row[4].Trim() : "Общеобразовательный";
            return true;
        }

        if (int.TryParse(col1, out int legacyCourse) && legacyCourse is >= 1 and <= 5)
        {
            specialtyId = null;
            course = legacyCourse;
            hours = row.Count > 2 && int.TryParse(row[2].Trim(), out int h2) ? h2 : null;
            type = row.Count > 3 && !string.IsNullOrWhiteSpace(row[3]) ? row[3].Trim() : "Общеобразовательный";
            return true;
        }

        err = $"неизвестная специальность «{col1}»";
        return false;
    }

    /// <summary>Импорт специальностей. Колонки: Код, Название, Квалификация</summary>
    public static string? ImportSpecialties()
    {
        var path = PickFile(ImportDataKind.Specialties);
        if (path == null) return null;
        try
        {
            return ImportSpecialtiesFromFile(path);
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка импорта: {ex.Message}"); return null; }
    }

    public static string ImportSpecialtiesFromFile(string path)
    {
            var rows = ReadFile(path);
            int imported = 0;
            foreach (var row in rows)
            {
                if (row.Count < 3) continue;
                DatabaseService.Instance.InsertSpecialty(row[0], row[1], row[2]);
                imported++;
            }
            return $"Импортировано специальностей: {imported}";
    }

    // ===== Helpers =====
    private static string? PickFile(ImportDataKind kind)
    {
        var owner = Application.Current?.MainWindow;
        if (owner is { IsLoaded: false }) owner = null;
        var dlg = new ImportPickFileDialog(kind) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.SelectedPath : null;
    }

    private static List<List<string>> ReadFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".csv" => ReadCsv(path),
            ".xlsx" or ".xls" => ReadExcel(path),
            _ => ReadCsv(path)
        };
    }

    private static List<List<string>> ReadCsv(string path)
    {
        var result = new List<List<string>>();
        var lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);
        bool first = true;
        foreach (var line in lines)
        {
            if (first) { first = false; continue; } // skip header
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = line.Split(new[] { ';', ',' }).Select(c => c.Trim().Trim('"')).ToList();
            result.Add(cells);
        }
        return result;
    }

    private static List<List<string>> ReadExcel(string path)
    {
        var result = new List<List<string>>();
        using var wb = new XLWorkbook(path);
        var ws = wb.Worksheet(1);
        var rows = ws.RowsUsed().Skip(1); // skip header
        foreach (var row in rows)
        {
            var cells = new List<string>();
            foreach (var cell in row.CellsUsed())
            {
                // Заполняем пустоты
                while (cells.Count < cell.Address.ColumnNumber - 1) cells.Add("");
                cells.Add(cell.GetString().Trim());
            }
            if (cells.Count > 0 && cells.Any(c => !string.IsNullOrWhiteSpace(c)))
                result.Add(cells);
        }
        return result;
    }
}

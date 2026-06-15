using System.Text;
using ChtotibDocsPrintNET.Data;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Очистка сиротских записей и отчёт о состоянии БД.</summary>
public static class DatabaseMaintenanceService
{
    public static string RunCleanup()
    {
        var db = DatabaseService.Instance;
        var sb = new StringBuilder();
        sb.AppendLine("Очистка базы данных:");
        sb.AppendLine();

        var orphansGradesStudent = db.ExecuteNonQuery(
            "DELETE FROM Grades WHERE StudentId NOT IN (SELECT Id FROM Students)");
        var orphansGradesSubject = db.ExecuteNonQuery(
            "DELETE FROM Grades WHERE SubjectId NOT IN (SELECT Id FROM Subjects)");
        var orphansDiplomas = db.ExecuteNonQuery(
            "DELETE FROM Diplomas WHERE StudentId NOT IN (SELECT Id FROM Students)");
        var orphansPrint = db.ExecuteNonQuery(
            "DELETE FROM PrintHistory WHERE StudentId NOT IN (SELECT Id FROM Students)");

        sb.AppendLine($"• Оценки без студента: {orphansGradesStudent}");
        sb.AppendLine($"• Оценки без предмета: {orphansGradesSubject}");
        sb.AppendLine($"• Дипломы без студента: {orphansDiplomas}");
        sb.AppendLine($"• История печати без студента: {orphansPrint}");
        sb.AppendLine();
        sb.AppendLine(db.GetDatabaseSummary());
        return sb.ToString();
    }
}

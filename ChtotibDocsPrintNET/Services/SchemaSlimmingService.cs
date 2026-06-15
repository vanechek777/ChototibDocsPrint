using ChtotibDocsPrintNET.Data;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Удаляет из БД колонки, не используемые приложением печати дипломов.</summary>
public static class SchemaSlimmingService
{
    private static readonly (string Table, string Column)[] ColumnsToDrop =
    {
        ("Students", "SNILS"), ("Students", "Phone"), ("Students", "Email"), ("Students", "Gender"),
        ("Students", "PassportSeries"), ("Students", "PassportNumber"), ("Students", "PassportIssueDate"),
        ("Students", "PassportIssuedBy"), ("Students", "PassportCode"),
        ("Students", "PreviousEducationDate"),
        ("Students", "ExpelledDate"), ("Students", "ExpelledReason"), ("Students", "Notes"),
        ("Groups", "CuratorName"), ("Groups", "GraduationDate"),
        ("Groups", "DemoExamDate1"), ("Groups", "DemoExamDate2"),
        ("Groups", "DiplomaDefenseDate1"), ("Groups", "DiplomaDefenseDate2"),
        ("Diplomas", "RegistrationNumber"),
        ("Diplomas", "CommissionChairman"), ("Diplomas", "DirectorName"),
        ("Diplomas", "PrintedDate"), ("Diplomas", "SupplementIsPrinted"),
        ("Diplomas", "SupplementPrintedDate"), ("Diplomas", "Notes"),
    };

    public static void ApplyIfNeeded(DatabaseService db)
    {
        foreach (var (table, column) in ColumnsToDrop)
        {
            try
            {
                db.ExecuteNonQuery($@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.{table}') AND name = N'{column}')
    ALTER TABLE dbo.[{table}] DROP COLUMN [{column}];");
            }
            catch
            {
                // игнорируем: нет прав или колонка уже удалена
            }
        }
    }
}

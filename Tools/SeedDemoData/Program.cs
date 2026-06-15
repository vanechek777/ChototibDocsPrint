using System.Text.Json;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Services;

var connectionString = args.FirstOrDefault();
if (string.IsNullOrWhiteSpace(connectionString))
{
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "settings.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "ChtotibDocsPrintNET", "bin", "Debug", "net9.0-windows", "settings.json")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "ChtotibDocsPrintNET", "bin", "Debug", "net9.0-windows", "settings.json")),
    };
    foreach (var path in candidates)
    {
        if (!File.Exists(path)) continue;
        try
        {
            var data = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(path));
            connectionString = data?.ConnectionString;
            if (!string.IsNullOrWhiteSpace(connectionString)) break;
        }
        catch { /* next */ }
    }
}

connectionString ??=
    @"Server=(localdb)\MSSQLLocalDB;Database=ChtotibDocPrint;Trusted_Connection=True;TrustServerCertificate=True;";

DatabaseService.Instance.SetConnectionString(connectionString);
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("Исправление кодировки специальностей и DEMO-данных…");
DemoDataSeeder.RepairSpecialtyEncoding(DatabaseService.Instance);
var result = DemoDataSeeder.Run(DatabaseService.Instance);
Console.WriteLine($"Группы: {string.Join(", ", result.Groups)}");
Console.WriteLine($"Добавлено студентов: {result.StudentsAdded}");
Console.WriteLine($"Всего DEMO-студентов: {DatabaseService.Instance.ExecuteScalarPublic<int>("SELECT COUNT(*) FROM Students WHERE RegistrationNumber LIKE 'DEMO-%'")}");

file sealed class SettingsFile
{
    public string? ConnectionString { get; set; }
}

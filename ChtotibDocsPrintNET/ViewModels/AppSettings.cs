using System.IO;
using System.Text.Json;

namespace ChtotibDocsPrintNET.ViewModels;

public class SettingsData
{
    public string? ConnectionString { get; set; }
    public bool IsDarkTheme { get; set; }
    public int FontSize { get; set; }
    public string? OrganizationName { get; set; }
    public string? DirectorName { get; set; }
}

/// <summary>Настройки приложения (файл в %AppData%, не теряется при пересборке).</summary>
public static class AppSettings
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private const string DefaultConnectionString =
        "Server=CRYMORE;Database=ChtotibDocPrint;Trusted_Connection=True;TrustServerCertificate=True;";

    public static string SettingsFilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChtotibDocsPrintNET");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    /// <summary>Создаёт или переносит settings.json до первого обращения к БД.</summary>
    public static void EnsureInitialized()
    {
        if (File.Exists(SettingsFilePath))
            return;

        var legacy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        if (File.Exists(legacy))
        {
            File.Copy(legacy, SettingsFilePath);
            return;
        }

        Save(new SettingsData
        {
            ConnectionString = DefaultConnectionString,
            FontSize = 13,
            OrganizationName = "ГПОУ «Читинский Техникум Отраслевых Технологий и Бизнеса»",
            DirectorName = ""
        });
    }

    public static SettingsData Load()
    {
        EnsureInitialized();
        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
        }
        catch
        {
            return new SettingsData { ConnectionString = DefaultConnectionString };
        }
    }

    public static void Save(SettingsData data) =>
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(data, JsonOpts));

    public static string? LoadConnectionString() => Load().ConnectionString;
}

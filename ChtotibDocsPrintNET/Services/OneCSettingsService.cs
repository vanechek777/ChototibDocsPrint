using System.IO;
using System.Text.Json;

namespace ChtotibDocsPrintNET.Services;

public class OneCSettings
{
    public string? ODataBaseUrl { get; set; }
    public string? ODataUsername { get; set; }
    public string? ODataPassword { get; set; }
    /// <summary>Имя публикации OData, например Catalog_Студенты или Document_Ведомость.</summary>
    public string StudentsEntity { get; set; } = "Catalog_Студенты";
    public bool UseFileExport { get; set; } = true;
}

public static class OneCSettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static string SettingsPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "onec_settings.json");

    public static OneCSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new OneCSettings();
            return JsonSerializer.Deserialize<OneCSettings>(File.ReadAllText(SettingsPath), JsonOpts) ?? new OneCSettings();
        }
        catch
        {
            return new OneCSettings();
        }
    }

    public static void Save(OneCSettings settings) =>
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOpts));
}

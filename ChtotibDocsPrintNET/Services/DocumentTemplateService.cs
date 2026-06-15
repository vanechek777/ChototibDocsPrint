using System.IO;
using System.Text.Json;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Services;

/// <summary>
/// Пути к JPG-бланкам: встроенные в DocsTemplates или пользовательские (копируются/читаются по абсолютному пути).
/// </summary>
public static class DocumentTemplateService
{
    public const string DefaultDiplomaFront = "Diploma_Front.jpg";
    public const string DefaultDiplomaBack = "Diploma_Back.jpg";
    public const string DefaultDiplomaExcellentFront = "DiplomaExcellent_Front.jpg";
    public const string DefaultDiplomaExcellentBack = "DiplomaExcellent_Back.jpg";
    public const string DefaultAppendixFront = "Prilojenie_Front.jpg";
    public const string DefaultAppendixBack = "Prilojenie_Back.jpg";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static string TemplatesDirectory =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DocsTemplates");

    public static string TemplateSettingsPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template_settings.json");

    public static DocumentTemplateSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(TemplateSettingsPath)) return new DocumentTemplateSettings();
            var json = File.ReadAllText(TemplateSettingsPath);
            return JsonSerializer.Deserialize<DocumentTemplateSettings>(json, JsonOpts) ?? new DocumentTemplateSettings();
        }
        catch
        {
            return new DocumentTemplateSettings();
        }
    }

    public static void SaveSettings(DocumentTemplateSettings settings)
    {
        Directory.CreateDirectory(TemplatesDirectory);
        File.WriteAllText(TemplateSettingsPath, JsonSerializer.Serialize(settings, JsonOpts));
    }

    /// <param name="documentType">Как в UI: «Диплом (лицевая)» и т.д.</param>
    /// <param name="useHonor">Для диплома — шаблон с отличием, если файл задан.</param>
    public static byte[]? LoadTemplateBytes(string documentType, bool useHonor = false)
    {
        var settings = LoadSettings();
        var fileName = ResolveFileName(documentType, useHonor);
        if (fileName == null) return null;

        var customPath = settings.GetPath(documentType, useHonor);
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
            return File.ReadAllBytes(customPath);

        var bundled = Path.Combine(TemplatesDirectory, fileName);
        if (File.Exists(bundled))
            return File.ReadAllBytes(bundled);

        foreach (var alt in ResolveAlternateFileNames(fileName))
        {
            var altPath = Path.Combine(TemplatesDirectory, alt);
            if (File.Exists(altPath))
                return File.ReadAllBytes(altPath);
        }

        return null;
    }

    /// <summary>Старые имена файлов (опечатки в комплекте шаблонов).</summary>
    private static IEnumerable<string> ResolveAlternateFileNames(string fileName)
    {
        if (string.Equals(fileName, DefaultDiplomaExcellentBack, StringComparison.OrdinalIgnoreCase))
            yield return "DiplomaExcelent_Back.jpg";
    }

    public static string? ResolveFileName(string documentType, bool useHonor) => documentType switch
    {
        "Диплом (лицевая)" => useHonor ? DefaultDiplomaExcellentFront : DefaultDiplomaFront,
        "Диплом (обратная сторона)" => useHonor ? DefaultDiplomaExcellentBack : DefaultDiplomaBack,
        "Приложение (лицевая)" => DefaultAppendixFront,
        "Приложение (оборотная)" => DefaultAppendixBack,
        _ => null
    };

    /// <summary>Копирует выбранный файл в DocsTemplates под стандартным именем и запоминает путь.</summary>
    public static void SetCustomTemplate(string documentType, bool useHonor, string sourceFilePath)
    {
        var fileName = ResolveFileName(documentType, useHonor);
        if (fileName == null) throw new InvalidOperationException("Неизвестный тип документа.");
        Directory.CreateDirectory(TemplatesDirectory);
        var dest = Path.Combine(TemplatesDirectory, fileName);
        File.Copy(sourceFilePath, dest, overwrite: true);
        var settings = LoadSettings();
        settings.SetPath(documentType, useHonor, dest);
        SaveSettings(settings);
    }
}

public class DocumentTemplateSettings
{
    public string? DiplomaFrontPath { get; set; }
    public string? DiplomaBackPath { get; set; }
    public string? DiplomaExcellentFrontPath { get; set; }
    public string? DiplomaExcellentBackPath { get; set; }
    public string? AppendixFrontPath { get; set; }
    public string? AppendixBackPath { get; set; }

    public string? GetPath(string documentType, bool useHonor) => (documentType, useHonor) switch
    {
        ("Диплом (лицевая)", true) => DiplomaExcellentFrontPath,
        ("Диплом (лицевая)", false) => DiplomaFrontPath,
        ("Диплом (обратная сторона)", true) => DiplomaExcellentBackPath,
        ("Диплом (обратная сторона)", false) => DiplomaBackPath,
        ("Приложение (лицевая)", _) => AppendixFrontPath,
        ("Приложение (оборотная)", _) => AppendixBackPath,
        _ => null
    };

    public void SetPath(string documentType, bool useHonor, string path)
    {
        switch (documentType, useHonor)
        {
            case ("Диплом (лицевая)", true): DiplomaExcellentFrontPath = path; break;
            case ("Диплом (лицевая)", false): DiplomaFrontPath = path; break;
            case ("Диплом (обратная сторона)", true): DiplomaExcellentBackPath = path; break;
            case ("Диплом (обратная сторона)", false): DiplomaBackPath = path; break;
            case ("Приложение (лицевая)", _): AppendixFrontPath = path; break;
            case ("Приложение (оборотная)", _): AppendixBackPath = path; break;
        }
    }
}

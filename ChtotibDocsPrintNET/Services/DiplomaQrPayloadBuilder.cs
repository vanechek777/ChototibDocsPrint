using System.Globalization;

using ChtotibDocsPrintNET.Models;



namespace ChtotibDocsPrintNET.Services;



/// <summary>

/// Формирует однострочный текст для QR-кода на обороте диплома (поля через « | »).

/// </summary>

public static class DiplomaQrPayloadBuilder

{

    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");



    public const string DefaultInstitution = "ГПОУ \"ЧТОТиБ\"";

    public const string DefaultDemoLevel = "Профильный уровень";

    public const int DefaultDemoMaxScore = 70;



    /// <summary>Дата выдачи для QR: из карточки студента или 22.06 года выпуска.</summary>

    public static DateTime ResolveQrIssueDate(DateTime? diplomaIssueDate, DateTime fallbackYearSource) =>

        diplomaIssueDate?.Date ?? new DateTime(fallbackYearSource.Year, 6, 22);



    /// <summary>Краткое наименование только для текста QR-кода (на бланке — полное имя из настроек).</summary>
    public static string FormatInstitution(string? orgNameFromSettings)
    {
        if (string.IsNullOrWhiteSpace(orgNameFromSettings))
            return DefaultInstitution;

        var t = orgNameFromSettings.Trim();
        if (string.Equals(t, DefaultInstitution, StringComparison.OrdinalIgnoreCase))
            return DefaultInstitution;

        // Полное юр. название из настроек → краткое для QR/приложения.
        if (t.Contains("ЧТОТиБ", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Читинск", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Государственное Профессиональное", StringComparison.OrdinalIgnoreCase)
            || t.Contains("Отраслевых Технологий", StringComparison.OrdinalIgnoreCase))
            return DefaultInstitution;

        if (t.Contains("ГПОУ", StringComparison.OrdinalIgnoreCase))
            return t;

        return $"ГПОУ \"{t}\"";
    }



    public static string Build(

        Student student,

        DateTime issueDate,

        string? organizationName,

        string? demoExamParticipantCode,

        decimal? demoExamScore,

        int demoExamMaxScore = DefaultDemoMaxScore,

        string? demoExamLevel = null)

    {

        var fio = student.FullName.Trim();

        var dateLine = issueDate.ToString("dd.MM.yyyy", Ru);

        var level = string.IsNullOrWhiteSpace(demoExamLevel) ? DefaultDemoLevel : demoExamLevel.Trim();

        var institution = FormatInstitution(organizationName);

        var code = (demoExamParticipantCode ?? "").Trim();

        var max = demoExamMaxScore > 0 ? demoExamMaxScore : DefaultDemoMaxScore;

        var scoreText = demoExamScore.HasValue

            ? demoExamScore.Value.ToString("0.##", Ru)

            : "—";



        var codePart = string.IsNullOrEmpty(code) ? "КОД" : $"КОД {code}";

        const string sep = " | ";
        return string.Join(sep, new[]
        {
            fio,
            dateLine,
            level,
            institution,
            codePart,
            $"результат: {scoreText} из {max} баллов",
        });

    }

}



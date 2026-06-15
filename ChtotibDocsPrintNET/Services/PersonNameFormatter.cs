using System.Globalization;
using System.Linq;

namespace ChtotibDocsPrintNET.Services;

/// <summary>Формат «И. О. Фамилия» для подписантов (Фамилия Имя Отчество в одной строке).</summary>
public static class PersonNameFormatter
{
    /// <param name="fullName">Обычно «Фамилия Имя Отчество».</param>
    public static string ToShortRussianOfficial(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var t = fullName.Trim();

        var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return t;

        // Короткая форма (И.О. Фамилия, Л.В. Смирнова …)
        if (parts.Any(static p => p.Contains('.', StringComparison.Ordinal)))
            return t;

        if (parts.Length == 1)
            return Capitalize(parts[0]);

        if (parts.Length == 2)
        {
            // Фамилия Имя
            var surname = Capitalize(parts[0]);
            if (parts[1].Length == 0) return t;
            var initial = char.ToUpper(parts[1][0], Ru);
            return $"{initial}. {surname}";
        }

        // Фамилия Имя Отчество
        if (parts[1].Length == 0 || parts[2].Length == 0) return t;
        var ln = Capitalize(parts[0]);
        var i1 = char.ToUpper(parts[1][0], Ru);
        var i2 = char.ToUpper(parts[2][0], Ru);
        return $"{i1}.{i2}. {ln}";
    }

    private static readonly CultureInfo Ru = new("ru-RU");

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpper(Ru);
        return char.ToUpper(s[0], Ru) + s[1..];
    }
}

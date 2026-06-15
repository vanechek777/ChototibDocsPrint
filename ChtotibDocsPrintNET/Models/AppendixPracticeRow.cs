namespace ChtotibDocsPrintNET.Models;

/// <summary>Строка раздела учебной/производственной практики на обороте приложения.</summary>
public sealed class AppendixPracticeRow
{
    public string ActivityText { get; init; } = "";
    public string TrainingMeansText { get; init; } = "";
    public string PlaceText { get; init; } = "";
}

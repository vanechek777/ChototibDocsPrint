using System.Collections.ObjectModel;
using ChtotibDocsPrintNET.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;

    [ObservableProperty] private int _totalStudents;
    [ObservableProperty] private int _totalGraduates;
    [ObservableProperty] private string _dataCompletion = "0%";
    [ObservableProperty] private int _printedToday;
    [ObservableProperty] private bool _hasWarning;
    [ObservableProperty] private string _warningMessage = string.Empty;
    [ObservableProperty] private int _totalGroups;
    [ObservableProperty] private int _totalSpecialties;
    [ObservableProperty] private string _lastPrintDate = "—";

    public DashboardViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var db = DatabaseService.Instance;
            TotalStudents = db.GetTotalStudents();
            TotalGraduates = db.GetTotalGraduates();
            TotalGroups = db.GetTotalGroups();
            TotalSpecialties = db.GetTotalSpecialties();
            PrintedToday = db.GetPrintedToday();
            LastPrintDate = db.GetLastPrintDate()?.ToString("dd.MM.yyyy HH:mm") ?? "—";

            var (filled, total) = db.GetDataCompletionStats();
            DataCompletion = total > 0 ? $"{filled * 100 / total}%" : "—";

            int warnCount = db.GetGraduatesWithoutRegNumberCount();
            HasWarning = warnCount > 0;
            if (HasWarning)
                WarningMessage = $"Не все регистрационные номера заполнены ({warnCount} студ.). Перейдите во вкладку Студенты и заполните их.";
        }
        catch { /* БД не подключена */ }
    }

    [RelayCommand] private void NavigateToPrint() => _mainVm.NavigateTo("Print");
    [RelayCommand] private void NavigateToStudents() => _mainVm.NavigateTo("Students");
    [RelayCommand] private void NavigateToGroups() => _mainVm.NavigateTo("Groups");
    [RelayCommand] private void Refresh() => LoadData();

    public void Reload() => LoadData();
}

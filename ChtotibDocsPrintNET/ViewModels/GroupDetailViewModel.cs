using System.Collections.ObjectModel;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class GroupDetailViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;
    [ObservableProperty] private int _groupId;
    [ObservableProperty] private string _groupName = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalStudents;
    [ObservableProperty] private string _successRate = "—";
    [ObservableProperty] private string _excellentCount = "0";
    [ObservableProperty] private string _goodCount = "0";
    [ObservableProperty] private string _specialtyName = "—";
    public ObservableCollection<Student> Students { get; } = new();

    public GroupDetailViewModel(MainViewModel mainVm, int groupId, string groupName)
    { _mainVm = mainVm; _groupId = groupId; _groupName = groupName; LoadData(); }

    partial void OnSearchTextChanged(string value) => LoadStudents();

    private void LoadData()
    {
        try {
            var db = DatabaseService.Instance;
            var group = db.GetGroupById(GroupId);
            if (group != null)
                SpecialtyName = group.SpecialtyName ?? "—";
            LoadStudents();
            var stats = db.GetGroupGradeStats(GroupId);
            TotalStudents = stats.Total;
            SuccessRate = stats.Total > 0 ? $"{stats.SuccessRate}%" : "—";
            ExcellentCount = $"{stats.ExcellentCount} ({stats.ExcellentPercent:F1}%)";
            GoodCount = $"{stats.GoodCount} ({stats.GoodPercent:F1}%)";
        } catch { }
    }

    private void LoadStudents()
    {
        try {
            var students = DatabaseService.Instance.GetStudentsByGroup(GroupId, string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Students.Clear(); foreach (var s in students) Students.Add(s);
            TotalStudents = Students.Count;
        } catch { }
    }

    [RelayCommand] private void OpenStudent(Student? s) { if (s != null) _mainVm.NavigateToStudentDetail(s.Id); }
    [RelayCommand] private void GoBack() => _mainVm.NavigateTo("Groups");
}

using System.Collections.ObjectModel;
using System.Windows;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class StudentsViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;
    [ObservableProperty] private bool _showOnlyGraduates;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalStudentsCount;
    public ObservableCollection<Student> Students { get; } = new();

    public StudentsViewModel(MainViewModel mainVm) { _mainVm = mainVm; LoadStudents(); }

    partial void OnShowOnlyGraduatesChanged(bool value) => LoadStudents();
    partial void OnSearchTextChanged(string value) => LoadStudents();

    private void LoadStudents()
    {
        try {
            var students = DatabaseService.Instance.GetAllStudents(ShowOnlyGraduates,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Students.Clear(); foreach (var s in students) Students.Add(s);
            TotalStudentsCount = Students.Count;
        } catch { TotalStudentsCount = 0; }
    }

    [RelayCommand] private void OpenStudent(Student? s) { if (s != null) _mainVm.NavigateToStudentDetail(s.Id); }

    [RelayCommand]
    private void AddStudent()
    {
        var dlg = new AddStudentDialog();
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true) LoadStudents();
    }

    [RelayCommand]
    private void ImportStudents()
    {
        var result = Services.ImportService.ImportStudents();
        if (result != null)
        {
            MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadStudents();
        }
    }

    [RelayCommand] private void Refresh() => LoadStudents();

    public void Reload() => LoadStudents();
}

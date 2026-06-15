using System.Collections.ObjectModel;
using System.Windows;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class StudentDetailViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;

    [ObservableProperty] private int _studentId;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _groupName = string.Empty;
    [ObservableProperty] private string _registrationNumber = "—";
    [ObservableProperty] private string _birthDate = "—";
    [ObservableProperty] private string _previousEducation = "—";
    [ObservableProperty] private string _previousEducationDoc = "—";
    [ObservableProperty] private string _diplomaIssueDateDisplay = "—";
    [ObservableProperty] private string _diplomaSeriesDisplay = "—";
    [ObservableProperty] private string _diplomaNumberDisplay = "—";
    [ObservableProperty] private string _diplomaTypeDisplay = "—";
    [ObservableProperty] private string _qualificationDisplay = "—";
    [ObservableProperty] private string _demoExamParticipantCodeDisplay = "—";
    [ObservableProperty] private string _demoExamScoreDisplay = "—";
    [ObservableProperty] private string _demoExamLevelDisplay = "—";

    public ObservableCollection<string> DiplomaTypes { get; } = new() { "Обычный", "С отличием" };

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editRegistrationNumber = "";
    [ObservableProperty] private string _editQualification = "";
    [ObservableProperty] private string _editDiplomaSeries = "";
    [ObservableProperty] private string _editDiplomaNumber = "";
    [ObservableProperty] private string _editDiplomaType = "Обычный";
    [ObservableProperty] private string _editPreviousEducation = "";
    [ObservableProperty] private string _editPreviousEducationDoc = "";
    [ObservableProperty] private DateTime? _editDiplomaIssueDate;
    [ObservableProperty] private string _editDemoExamParticipantCode = "";
    [ObservableProperty] private string _editDemoExamScore = "";
    [ObservableProperty] private string _editDemoExamMaxScore = "70";
    [ObservableProperty] private string _editDemoExamLevel = "";

    public ObservableCollection<GradeRow> GradeRows { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteGradeCommand))]
    private GradeRow? _selectedGradeRow;

    public StudentDetailViewModel(MainViewModel mainVm, int studentId)
    {
        _mainVm = mainVm;
        _studentId = studentId;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var db = DatabaseService.Instance;
            var student = db.GetStudentById(StudentId);
            if (student == null) return;
            FullName = student.FullName;
            GroupName = student.GroupName ?? "—";
            RegistrationNumber = string.IsNullOrWhiteSpace(student.RegistrationNumber) ? "—" : student.RegistrationNumber;
            QualificationDisplay = string.IsNullOrWhiteSpace(student.Qualification) ? "—" : student.Qualification;
            BirthDate = student.BirthDate?.ToString("dd.MM.yyyy") ?? "—";
            PreviousEducation = string.IsNullOrWhiteSpace(student.PreviousEducation) ? "—" : student.PreviousEducation;
            PreviousEducationDoc = string.IsNullOrWhiteSpace(student.PreviousEducationDoc) ? "—" : student.PreviousEducationDoc;

            var dip = db.GetDiplomaByStudent(StudentId);
            DiplomaSeriesDisplay = string.IsNullOrWhiteSpace(dip?.Series) ? "—" : dip.Series;
            DiplomaNumberDisplay = string.IsNullOrWhiteSpace(dip?.Number) ? "—" : dip.Number;
            DiplomaTypeDisplay = dip?.DiplomaType ?? "Обычный";
            DiplomaIssueDateDisplay = dip?.IssueDate?.ToString("dd.MM.yyyy") ?? "—";
            DemoExamParticipantCodeDisplay = string.IsNullOrWhiteSpace(student.DemoExamParticipantCode) ? "—" : student.DemoExamParticipantCode;
            DemoExamLevelDisplay = string.IsNullOrWhiteSpace(student.DemoExamLevel) ? "профильный уровень" : student.DemoExamLevel;
            DemoExamScoreDisplay = student.DemoExamScore.HasValue
                ? $"{student.DemoExamScore.Value.ToString("0.##")} из {student.DemoExamMaxScore} баллов"
                : "—";

            var grades = db.GetStudentGrades(StudentId);
            SelectedGradeRow = null;
            GradeRows.Clear();
            int currentCourse = 0, idx = 0;
            foreach (var g in grades)
            {
                if (g.Course != currentCourse)
                {
                    currentCourse = g.Course;
                    idx = 0;
                    GradeRows.Add(new GradeRow
                    {
                        IsCourseHeader = true,
                        CourseName = $"{ToRoman(currentCourse)} курс"
                    });
                }
                idx++;
                GradeRows.Add(new GradeRow
                {
                    GradeId = g.Id,
                    Number = idx,
                    SubjectName = g.SubjectName ?? "",
                    GradeType = g.GradeType,
                    Grade = g.GradeValue,
                    Hours = g.Hours
                });
            }
        }
        catch { }
    }

    partial void OnIsEditingChanged(bool value)
    {
        if (!value) return;
        var db = DatabaseService.Instance;
        var student = db.GetStudentById(StudentId);
        var dip = db.GetDiplomaByStudent(StudentId);
        EditRegistrationNumber = student?.RegistrationNumber ?? "";
        EditQualification = student?.Qualification ?? "";
        EditDiplomaSeries = dip?.Series ?? "";
        EditDiplomaNumber = dip?.Number ?? "";
        EditDiplomaType = string.IsNullOrWhiteSpace(dip?.DiplomaType) ? "Обычный" : dip.DiplomaType;
        EditPreviousEducation = student?.PreviousEducation ?? "";
        EditPreviousEducationDoc = student?.PreviousEducationDoc ?? "";
        EditDiplomaIssueDate = dip?.IssueDate;
        EditDemoExamParticipantCode = student?.DemoExamParticipantCode ?? "";
        EditDemoExamScore = student?.DemoExamScore?.ToString("0.##") ?? "";
        EditDemoExamMaxScore = (student?.DemoExamMaxScore ?? 70).ToString();
        EditDemoExamLevel = student?.DemoExamLevel ?? "";
    }

    private static string ToRoman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => n.ToString() };

    [RelayCommand]
    private void PrintDiploma() => _mainVm.NavigateToPrintForStudent(StudentId);

    [RelayCommand]
    private void ToggleEdit() => IsEditing = !IsEditing;

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private void SaveDiplomaFields()
    {
        try
        {
            decimal? demoScore = null;
            if (!string.IsNullOrWhiteSpace(EditDemoExamScore))
            {
                var t = EditDemoExamScore.Trim().Replace(',', '.');
                if (!decimal.TryParse(t, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    MessageBox.Show("Некорректное значение баллов демоэкзамена.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                demoScore = parsed;
            }

            if (!int.TryParse(EditDemoExamMaxScore, out var demoMax) || demoMax <= 0)
                demoMax = 70;

            DatabaseService.Instance.UpdateStudentAndDiplomaBlank(
                StudentId,
                EditRegistrationNumber,
                EditQualification,
                EditDiplomaSeries,
                EditDiplomaNumber,
                EditDiplomaType,
                EditPreviousEducation,
                EditPreviousEducationDoc,
                EditDiplomaIssueDate,
                string.IsNullOrWhiteSpace(EditDemoExamParticipantCode) ? null : EditDemoExamParticipantCode.Trim(),
                demoScore,
                demoMax,
                string.IsNullOrWhiteSpace(EditDemoExamLevel) ? null : EditDemoExamLevel.Trim());
            IsEditing = false;
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand] private void GoBack() => _mainVm.NavigateTo("Students");

    [RelayCommand]
    private void AddGrade()
    {
        var dlg = new AddGradeDialog(StudentId);
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true)
            LoadData();
    }

    private bool CanDeleteGrade() =>
        SelectedGradeRow is { IsCourseHeader: false, GradeId: > 0 };

    [RelayCommand(CanExecute = nameof(CanDeleteGrade))]
    private void DeleteGrade()
    {
        var row = SelectedGradeRow;
        if (row is not { IsCourseHeader: false, GradeId: > 0 })
            return;

        var typeSuffix = string.IsNullOrWhiteSpace(row.GradeType) ? "" : $" ({row.GradeType})";
        if (MessageBox.Show(
                $"Удалить предмет «{row.SubjectName}»{typeSuffix} и оценку {row.Grade}?",
                "Удаление предмета",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            DatabaseService.Instance.DeleteStudentGrade(row.GradeId, StudentId);
            SelectedGradeRow = null;
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось удалить: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

public class GradeRow
{
    public int GradeId { get; set; }
    public bool IsCourseHeader { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int Number { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string GradeType { get; set; } = string.Empty;
    public int Grade { get; set; }
    public int? Hours { get; set; }
}

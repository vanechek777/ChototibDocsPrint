using System.Windows;
using System.Windows.Controls;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Services;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddGradeDialog : Window
{
    private readonly int _studentId;

    public AddGradeDialog(int studentId)
    {
        InitializeComponent();
        _studentId = studentId;
        TbTrainingMeans.Text = AppendixPracticeDisplayBuilder.TrainingMeansText;
        CbPracticePlace.ItemsSource = AppendixPracticeDisplayBuilder.PracticePlaces;
        LoadSubjects();
    }

    private void LoadSubjects()
    {
        try
        {
            var db = DatabaseService.Instance;
            var st = db.GetStudentById(_studentId);
            if (st == null)
            {
                CbSubject.ItemsSource = Array.Empty<Subject>();
                ShowNoSubjectsState("Студент не найден.");
                return;
            }

            var subjects = db.GetSubjectsAvailableForStudentGrade(st.GroupId, _studentId);
            CbSubject.ItemsSource = subjects;

            if (subjects.Count == 0)
            {
                ShowNoSubjectsState("Нет предметов без оценки: по всем доступным предметам оценка уже выставлена.");
                return;
            }

            TbNoSubjects.Visibility = Visibility.Collapsed;
            CbSubject.IsEnabled = true;
            CbSubject.SelectedIndex = 0;
            UpdateFieldMode();
        }
        catch (Exception ex)
        {
            ShowNoSubjectsState($"Не удалось загрузить предметы: {ex.Message}");
        }
    }

    private void ShowNoSubjectsState(string message)
    {
        TbNoSubjects.Text = message;
        TbNoSubjects.Visibility = Visibility.Visible;
        CbSubject.IsEnabled = false;
        BtnSave.IsEnabled = false;
    }

    private void CbSubject_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateFieldMode();

    private void UpdateFieldMode()
    {
        if (CbSubject.SelectedItem is not Subject subject)
        {
            PanelDiscipline.Visibility = Visibility.Visible;
            PanelPractice.Visibility = Visibility.Collapsed;
            return;
        }

        var isPractice = SubjectPracticeHelper.IsPracticeSubject(subject);
        PanelDiscipline.Visibility = isPractice ? Visibility.Collapsed : Visibility.Visible;
        PanelPractice.Visibility = isPractice ? Visibility.Visible : Visibility.Collapsed;

        if (!isPractice)
            return;

        TbActivity.Text = SubjectPracticeHelper.DefaultActivityText(subject);
        if (string.IsNullOrWhiteSpace(TbTrainingMeans.Text))
            TbTrainingMeans.Text = AppendixPracticeDisplayBuilder.TrainingMeansText;

        if (CbPracticePlace.SelectedItem == null && string.IsNullOrWhiteSpace(CbPracticePlace.Text))
            CbPracticePlace.SelectedIndex = 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CbSubject.SelectedItem is not Subject subject)
        {
            MessageBox.Show("Выберите предмет.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (SubjectPracticeHelper.IsPracticeSubject(subject))
                SavePractice(subject);
            else
                SaveDiscipline(subject);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveDiscipline(Subject subject)
    {
        if (CbGrade.SelectedItem is not ComboBoxItem gradeItem)
        {
            MessageBox.Show("Выберите оценку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int gradeValue = gradeItem.Content?.ToString()?[0] switch
        {
            '5' => 5, '4' => 4, '3' => 3, '2' => 2, _ => 0
        };
        if (gradeValue < 2)
        {
            MessageBox.Show("Выберите оценку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string gradeType = (CbGradeType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Итоговая";
        DatabaseService.Instance.InsertGrade(_studentId, subject.Id, gradeValue, gradeType);
    }

    private void SavePractice(Subject subject)
    {
        var place = (CbPracticePlace.Text ?? "").Trim();
        if (string.IsNullOrEmpty(place))
        {
            MessageBox.Show("Укажите место прохождения практики.", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var trainingMeans = (TbTrainingMeans.Text ?? "").Trim();
        if (string.IsNullOrEmpty(trainingMeans))
            trainingMeans = AppendixPracticeDisplayBuilder.TrainingMeansText;

        var gradeType = SubjectPracticeHelper.ResolveGradeType(subject);
        DatabaseService.Instance.InsertGrade(
            _studentId,
            subject.Id,
            grade: 5,
            gradeType,
            practicePlace: place,
            practiceTrainingMeans: trainingMeans);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

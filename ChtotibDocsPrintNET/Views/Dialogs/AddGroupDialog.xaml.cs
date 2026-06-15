using System.Windows;
using System.Windows.Controls;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddGroupDialog : Window
{
    public Group? CreatedGroup { get; private set; }

    public AddGroupDialog()
    {
        InitializeComponent();
        TbYear.Text = DateTime.Now.Year.ToString();
        TbYear.TextChanged += (_, _) => UpdateCourseHint();
        UpdateCourseHint();
        try { CbSpecialty.ItemsSource = DatabaseService.Instance.GetAllSpecialties(); } catch { }
    }

    private void UpdateCourseHint()
    {
        if (int.TryParse(TbYear.Text, out int year))
        {
            int course = DateTime.Now.Year - year + (DateTime.Now.Month >= 9 ? 1 : 0);
            if (course < 1) course = 1;
            if (course > 5) course = 5;
            TbCourseHint.Text = $"Вычисленный курс: {course}";
        }
        else TbCourseHint.Text = "";
    }

    private int CalculateCourse(int year)
    {
        int course = DateTime.Now.Year - year + (DateTime.Now.Month >= 9 ? 1 : 0);
        return Math.Clamp(course, 1, 5);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbName.Text))
        { MessageBox.Show("Введите наименование группы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (CbSpecialty.SelectedItem is not Specialty spec)
        { MessageBox.Show("Выберите специальность.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (!int.TryParse(TbYear.Text, out int year))
        { MessageBox.Show("Введите корректный год.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var group = new Group
        {
            Name = TbName.Text.Trim(),
            SpecialtyId = spec.Id,
            EnrollmentYear = year,
            Address = (CbAddress.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            CourseNumber = CalculateCourse(year),
            IsGraduating = ChkGraduating.IsChecked == true
        };

        try
        {
            DatabaseService.Instance.InsertGroup(group);
            CreatedGroup = group;
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

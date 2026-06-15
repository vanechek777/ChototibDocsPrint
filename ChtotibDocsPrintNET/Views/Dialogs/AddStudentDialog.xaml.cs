using System.Windows;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddStudentDialog : Window
{
    public Student? CreatedStudent { get; private set; }

    public AddStudentDialog()
    {
        InitializeComponent();
        try { CbGroup.ItemsSource = DatabaseService.Instance.GetGroups(false, null, null); } catch { }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbLastName.Text) || string.IsNullOrWhiteSpace(TbFirstName.Text))
        { MessageBox.Show("Заполните обязательные поля (Фамилия, Имя).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (CbGroup.SelectedItem is not Group group)
        { MessageBox.Show("Выберите группу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        var student = new Student
        {
            LastName = TbLastName.Text.Trim(),
            FirstName = TbFirstName.Text.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(TbMiddleName.Text) ? null : TbMiddleName.Text.Trim(),
            GroupId = group.Id,
            BirthDate = DpBirthDate.SelectedDate
        };

        try
        {
            DatabaseService.Instance.InsertStudent(student);
            CreatedStudent = student;
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddSubjectDialog : Window
{
    public AddSubjectDialog(int? preselectSpecialtyId = null)
    {
        InitializeComponent();
        try
        {
            var list = new List<Specialty>
            {
                new Specialty { Id = 0, Name = "Общие (для всех специальностей)", Code = "—" }
            };
            list.AddRange(DatabaseService.Instance.GetAllSpecialties());
            CbSpecialty.ItemsSource = list;

            if (preselectSpecialtyId is int sid && sid > 0)
            {
                var sp = list.FirstOrDefault(x => x.Id == sid);
                if (sp != null)
                    CbSpecialty.SelectedItem = sp;
                else
                    CbSpecialty.SelectedIndex = 0;
            }
            else
            {
                CbSpecialty.SelectedIndex = 0;
            }
        }
        catch { }

        CbSpecialty.SelectionChanged += (_, _) => UpdateSpecialtyHint();
        UpdateSpecialtyHint();
    }

    private void UpdateSpecialtyHint()
    {
        if (CbSpecialty.SelectedItem is Specialty sp && sp.Id > 0)
            TbSpecialtyHint.Text = $"Будет сохранено для специальности: {sp.ComboDisplay}";
        else
            TbSpecialtyHint.Text = "Будет сохранено как общий предмет (для всех специальностей).";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbName.Text))
        { MessageBox.Show("Введите наименование предмета.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        if (CbSpecialty.SelectedItem is not Specialty spec)
        {
            MessageBox.Show("Выберите специальность.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int course = int.Parse(((CbCourse.SelectedItem as ComboBoxItem)?.Content?.ToString()) ?? "1");
        int? hours = int.TryParse(TbHours.Text, out int h) ? h : null;
        string type = (CbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Общеобразовательный";
        int? specId = spec.Id > 0 ? spec.Id : null;

        try
        {
            DatabaseService.Instance.InsertSubject(TbName.Text.Trim(), course, hours, type, specId, ChkExam.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

using System.Windows;
using ChtotibDocsPrintNET.Data;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddSpecialtyDialog : Window
{
    public AddSpecialtyDialog() { InitializeComponent(); }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbCode.Text) || string.IsNullOrWhiteSpace(TbName.Text) || string.IsNullOrWhiteSpace(TbShortName.Text))
        { MessageBox.Show("Заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try
        {
            DatabaseService.Instance.InsertSpecialty(TbCode.Text.Trim(), TbName.Text.Trim(), TbShortName.Text.Trim());
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

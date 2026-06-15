using System.Windows;
using System.Windows.Controls;
using ChtotibDocsPrintNET.Data;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class AddCommissionMemberDialog : Window
{
    public AddCommissionMemberDialog() { InitializeComponent(); }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TbFullName.Text))
        { MessageBox.Show("Введите ФИО.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        string role = (CbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Председатель";
        try
        {
            DatabaseService.Instance.InsertCommissionMember(TbFullName.Text.Trim(),
                string.IsNullOrWhiteSpace(TbPosition.Text) ? null : TbPosition.Text.Trim(), role);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}

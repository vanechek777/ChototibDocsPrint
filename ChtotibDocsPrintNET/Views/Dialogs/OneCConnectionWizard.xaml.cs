using System.Windows;
using ChtotibDocsPrintNET.Services;

namespace ChtotibDocsPrintNET.Views.Dialogs;

public partial class OneCConnectionWizard : Window
{
    public OneCConnectionWizard()
    {
        InitializeComponent();
        var s = OneCSettingsService.Load();
        TbODataUrl.Text = s.ODataBaseUrl ?? "";
        TbUser.Text = s.ODataUsername ?? "";
        TbEntity.Text = s.StudentsEntity;
    }

    private OneCSettings ReadSettings() => new()
    {
        ODataBaseUrl = TbODataUrl.Text.Trim(),
        ODataUsername = TbUser.Text.Trim(),
        ODataPassword = TbPassword.Password,
        StudentsEntity = string.IsNullOrWhiteSpace(TbEntity.Text) ? "Catalog_Студенты" : TbEntity.Text.Trim(),
    };

    private async void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        BtnTest.IsEnabled = false;
        TbStatus.Text = "Проверка…";
        var (ok, msg) = await OneCImportService.TestODataAsync(ReadSettings());
        TbStatus.Text = msg;
        BtnTest.IsEnabled = true;
    }

    private async void BtnSample_Click(object sender, RoutedEventArgs e)
    {
        BtnSample.IsEnabled = false;
        TbStatus.Text = "Запрос…";
        var (ok, msg) = await OneCImportService.FetchEntitySampleAsync(ReadSettings());
        TbStatus.Text = ok ? "✓ Образец получен" : "✗ " + msg;
        TbSample.Text = msg;
        BtnSample.IsEnabled = true;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        OneCSettingsService.Save(ReadSettings());
        TbStatus.Text = "✓ Настройки сохранены в onec_settings.json";
    }

    private void ImportSpecialties_Click(object sender, RoutedEventArgs e) =>
        ShowImport(ImportService.ImportSpecialties());

    private void ImportGroups_Click(object sender, RoutedEventArgs e) =>
        ShowImport(ImportService.ImportGroups());

    private void ImportStudents_Click(object sender, RoutedEventArgs e) =>
        ShowImport(ImportService.ImportStudents());

    private void ImportSubjects_Click(object sender, RoutedEventArgs e) =>
        ShowImport(ImportService.ImportSubjects());

    private static void ShowImport(string? result)
    {
        if (result != null)
            MessageBox.Show(result, "Импорт из файла", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

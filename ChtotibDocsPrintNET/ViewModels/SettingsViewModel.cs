using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=ChtotibDocPrint;Trusted_Connection=True;TrustServerCertificate=True;";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatus;
    [ObservableProperty] private bool _isError;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private int _selectedFontSize = 13;
    [ObservableProperty] private string _organizationName = "ЧТОТИБ";
    [ObservableProperty] private string _directorName = string.Empty;

    public ObservableCollection<int> FontSizes { get; } = new() { 11, 12, 13, 14, 15, 16 };

    public SettingsViewModel()
    {
        LoadSettings();
    }

    partial void OnIsDarkThemeChanged(bool value) => ApplyTheme(value);

    private void ApplyTheme(bool dark)
    {
        var app = Application.Current;
        var mergedDicts = app.Resources.MergedDictionaries;
        // Найти AppStyles и заменить
        for (int i = mergedDicts.Count - 1; i >= 0; i--)
        {
            var src = mergedDicts[i].Source?.ToString() ?? "";
            if (src.Contains("AppStyles"))
            {
                mergedDicts.RemoveAt(i);
                break;
            }
        }
        var uri = dark
            ? new Uri("Styles/DarkStyles.xaml", UriKind.Relative)
            : new Uri("Styles/AppStyles.xaml", UriKind.Relative);
        try { mergedDicts.Add(new ResourceDictionary { Source = uri }); } catch { }
    }

    [RelayCommand]
    private void TestConnection()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            StatusMessage = $"✓ Подключение успешно! Сервер: {conn.DataSource}, БД: {conn.Database}";
            IsError = false; HasStatus = true;
        }
        catch (Exception ex)
        { StatusMessage = $"✗ Ошибка подключения: {ex.Message}"; IsError = true; HasStatus = true; }
    }

    [RelayCommand]
    private void SaveAll()
    {
        try
        {
            DatabaseService.Instance.SetConnectionString(ConnectionString);
            SaveSettings();
            StatusMessage = "✓ Все настройки сохранены.";
            IsError = false; HasStatus = true;
        }
        catch (Exception ex) { StatusMessage = $"✗ Ошибка: {ex.Message}"; IsError = true; HasStatus = true; }
    }

    [RelayCommand]
    private void ImportSpecialties()
    {
        var result = Services.ImportService.ImportSpecialties();
        if (result != null) MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ImportSubjects()
    {
        var result = Services.ImportService.ImportSubjects();
        if (result != null) MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ImportGroups()
    {
        var result = Services.ImportService.ImportGroups();
        if (result != null) MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ImportStudents()
    {
        var result = Services.ImportService.ImportStudents();
        if (result != null) MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void CleanDatabase()
    {
        if (MessageBox.Show(
                "Удалить сиротские записи (оценки без студента/предмета, дипломы и история печати без студента)?\nДанные студентов не затрагиваются.",
                "Очистка базы",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var report = DatabaseMaintenanceService.RunCleanup();
            StatusMessage = "✓ Очистка выполнена.";
            IsError = false;
            HasStatus = true;
            MessageBox.Show(report, "Очистка базы", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ {ex.Message}";
            IsError = true;
            HasStatus = true;
        }
    }

    [RelayCommand]
    private void OpenOneCWizard()
    {
        var w = new Views.Dialogs.OneCConnectionWizard
        {
            Owner = Application.Current.MainWindow,
        };
        w.ShowDialog();
    }

    // ===== Persist =====
    private void SaveSettings()
    {
        AppSettings.Save(new SettingsData
        {
            ConnectionString = ConnectionString,
            IsDarkTheme = IsDarkTheme,
            FontSize = SelectedFontSize,
            OrganizationName = OrganizationName,
            DirectorName = DirectorName
        });
    }

    private void LoadSettings()
    {
        try
        {
            var data = AppSettings.Load();
            ConnectionString = data.ConnectionString ?? ConnectionString;
            IsDarkTheme = data.IsDarkTheme;
            SelectedFontSize = data.FontSize > 0 ? data.FontSize : 13;
            OrganizationName = data.OrganizationName ?? "ЧТОТИБ";
            DirectorName = data.DirectorName ?? "";
        }
        catch { }
    }
}

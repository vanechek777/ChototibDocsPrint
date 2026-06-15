using System.Collections.ObjectModel;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Threading;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class AdminViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;
    /// <summary>Пункт фильтра «Все предметы» (Id не из БД).</summary>
    private readonly Specialty _subjectsFilterAll = new() { Id = 0, Name = "Все", Code = "" };
    /// <summary>Пункт фильтра «Общие» (Id не из БД).</summary>
    private readonly Specialty _subjectsFilterCommon = new() { Id = -1, Name = "Общие", Code = "" };
    /// <summary>Опция для редактирования SpecialtyId у предмета (Id=0 => NULL).</summary>
    private readonly Specialty _subjectSpecialtyCommon = new() { Id = 0, Name = "Общие", Code = "" };

    [ObservableProperty] private Specialty? _selectedSpecialtyFilter;
    [ObservableProperty] private Specialty? _selectedSpecialty;
    [ObservableProperty] private Subject? _selectedSubject;
    [ObservableProperty] private CommissionMember? _selectedCommissionMember;
    [ObservableProperty] private bool _isSeedingDemoData;
    public ObservableCollection<Specialty> Specialties { get; } = new();
    public ObservableCollection<Specialty> SpecialtiesFilter { get; } = new();
    public ObservableCollection<Specialty> SubjectSpecialtyOptions { get; } = new();
    public ObservableCollection<Subject> Subjects { get; } = new();
    public ObservableCollection<CommissionMember> CommissionMembers { get; } = new();

    public AdminViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        LoadAll();
    }

    partial void OnSelectedSpecialtyFilterChanged(Specialty? value) => LoadSubjects();

    private void LoadAll()
    {
        try
        {
            var db = DatabaseService.Instance;
            var prevFilterId = SelectedSpecialtyFilter?.Id;
            Specialties.Clear(); SpecialtiesFilter.Clear();
            SpecialtiesFilter.Add(_subjectsFilterAll);
            SpecialtiesFilter.Add(_subjectsFilterCommon);
            SubjectSpecialtyOptions.Clear();
            SubjectSpecialtyOptions.Add(_subjectSpecialtyCommon);
            var allSpec = db.GetAllSpecialties();
            foreach (var s in allSpec)
            {
                Specialties.Add(s);
                SpecialtiesFilter.Add(s);
                SubjectSpecialtyOptions.Add(s);
            }
            SelectedSpecialty ??= Specialties.FirstOrDefault();
            SelectedSpecialtyFilter = prevFilterId is int pid && SpecialtiesFilter.Any(x => x.Id == pid)
                ? SpecialtiesFilter.First(x => x.Id == pid)
                : _subjectsFilterCommon;
            LoadSubjects();
            CommissionMembers.Clear();
            foreach (var c in db.GetCommissionMembers()) CommissionMembers.Add(c);
            SelectedCommissionMember ??= CommissionMembers.FirstOrDefault();
        }
        catch { }
    }

    private void LoadSubjects()
    {
        try
        {
            Subjects.Clear();
            int? sid = SelectedSpecialtyFilter switch
            {
                { Id: > 0 } f => f.Id,     // спец. + общие
                { Id: -1 } => -1,          // только общие (см. SQL: IS NULL OR = @sid)
                _ => null                  // все предметы
            };
            var subjects = DatabaseService.Instance.GetAllSubjects(sid);
            foreach (var s in subjects) Subjects.Add(s);
            SelectedSubject ??= Subjects.FirstOrDefault();
        }
        catch { }
    }

    [RelayCommand]
    private void AddSpecialty()
    {
        var dlg = new Views.Dialogs.AddSpecialtyDialog();
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true) LoadAll();
    }

    [RelayCommand]
    private void AddSubject()
    {
        int? preselectSpecId = SelectedSpecialtyFilter is { Id: > 0 } sp ? sp.Id : null;
        var dlg = new Views.Dialogs.AddSubjectDialog(preselectSpecId);
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true) LoadSubjects();
    }

    [RelayCommand]
    private void AddCommissionMember()
    {
        var dlg = new Views.Dialogs.AddCommissionMemberDialog();
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true) LoadAll();
    }

    private bool CanSeedDemoData() => !IsSeedingDemoData;

    [RelayCommand(CanExecute = nameof(CanSeedDemoData))]
    private async Task SeedDemoDataAsync()
    {
        if (MessageBox.Show(
                "Создать или дозаполнить демо-группы и студентов?\n\n" +
                "• ИСиП-22-2в, 3а, 4к, АРХ-22-1, СиС-22-1 — по 25 студентов\n" +
                "• ИСиП-22-1п — дозаполнение данных 4 существующим студентам\n" +
                "• Остальные группы — дозаполнение до 25\n" +
                "• Предметы, оценки, дипломы (серия 107724), демоэкзамен\n\n" +
                "Продолжить?",
                "Демо-данные", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        IsSeedingDemoData = true;
        SeedDemoDataCommand.NotifyCanExecuteChanged();
        try
        {
            var (result, error) = await Task.Run(() =>
            {
                try
                {
                    var db = DatabaseService.Instance;
                    Services.DemoDataSeeder.RepairSpecialtyEncoding(db);
                    var seedResult = Services.DemoDataSeeder.Run(db);
                    return (seedResult, (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (null, ex);
                }
            });

            if (error != null)
            {
                MessageBox.Show($"Не удалось заполнить демо-данные:\n{error.Message}", "Демо-данные",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _mainVm.RefreshAfterDemoSeed(refreshPrint: _mainVm.SelectedNavIndex == 3);
                LoadAll();
            }, DispatcherPriority.Background);

            var summary = result!.BuildSummary(DatabaseService.Instance);
            MessageBox.Show(summary, "Демо-данные", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            IsSeedingDemoData = false;
            SeedDemoDataCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnIsSeedingDemoDataChanged(bool value) =>
        SeedDemoDataCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void ImportSpecialties()
    {
        var result = Services.ImportService.ImportSpecialties();
        if (result != null) { MessageBox.Show(result, "Импорт"); LoadAll(); }
    }

    [RelayCommand]
    private void ImportSubjects()
    {
        var result = Services.ImportService.ImportSubjects();
        if (result != null) { MessageBox.Show(result, "Импорт"); LoadSubjects(); }
    }

    public void SaveSubjectSpecialty(Subject? subject)
    {
        if (subject == null || subject.Id <= 0) return;
        try
        {
            DatabaseService.Instance.UpdateSubject(subject);
            LoadSubjects();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить специальность предмета: {ex.Message}", "Предметы",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void SaveSpecialty(Specialty? specialty)
    {
        if (specialty == null || specialty.Id <= 0) return;
        try
        {
            DatabaseService.Instance.UpdateSpecialty(specialty);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить специальность: {ex.Message}", "Специальности",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void SaveCommissionMember(CommissionMember? member)
    {
        if (member == null || member.Id <= 0) return;
        try
        {
            DatabaseService.Instance.UpdateCommissionMember(member);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось сохранить члена комиссии: {ex.Message}", "Комиссия",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DeleteSelectedSubject()
    {
        if (SelectedSubject == null) return;
        if (MessageBox.Show($"Удалить предмет «{SelectedSubject.Name}»?", "Удаление",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            DatabaseService.Instance.DeleteSubject(SelectedSubject.Id);
            LoadSubjects();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось удалить предмет: {ex.Message}", "Предметы",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DeleteSelectedSpecialty()
    {
        if (SelectedSpecialty == null) return;
        if (MessageBox.Show($"Удалить специальность «{SelectedSpecialty.Name}»?\n\nЕсли к ней привязаны группы/предметы, удаление не получится.",
                "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            DatabaseService.Instance.DeleteSpecialty(SelectedSpecialty.Id);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось удалить специальность: {ex.Message}", "Специальности",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void DeleteSelectedCommissionMember()
    {
        if (SelectedCommissionMember == null) return;
        if (MessageBox.Show($"Удалить «{SelectedCommissionMember.FullName}»?", "Удаление",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        try
        {
            DatabaseService.Instance.DeleteCommissionMember(SelectedCommissionMember.Id);
            LoadAll();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось удалить члена комиссии: {ex.Message}", "Комиссия",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

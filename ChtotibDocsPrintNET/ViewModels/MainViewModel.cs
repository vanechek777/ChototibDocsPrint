using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private BaseViewModel _currentViewModel;

    [ObservableProperty]
    private int _selectedNavIndex;

    private readonly DashboardViewModel _dashboardVm;
    private readonly GroupsViewModel _groupsVm;
    private readonly StudentsViewModel _studentsVm;
    private readonly PrintViewModel _printVm;
    private readonly AdminViewModel _adminVm;
    private readonly SettingsViewModel _settingsVm;

    public MainViewModel()
    {
        _dashboardVm = new DashboardViewModel(this);
        _groupsVm = new GroupsViewModel(this);
        _studentsVm = new StudentsViewModel(this);
        _printVm = new PrintViewModel();
        _adminVm = new AdminViewModel(this);
        _settingsVm = new SettingsViewModel();
        _currentViewModel = _dashboardVm;
        _selectedNavIndex = 0;
    }

    [RelayCommand]
    public void NavigateTo(string page)
    {
        switch (page)
        {
            case "Dashboard": CurrentViewModel = _dashboardVm; SelectedNavIndex = 0; break;
            case "Groups":    CurrentViewModel = _groupsVm;    SelectedNavIndex = 1; break;
            case "Students":  CurrentViewModel = _studentsVm;  SelectedNavIndex = 2; break;
            case "Print":     CurrentViewModel = _printVm;     SelectedNavIndex = 3; break;
            case "Admin":     CurrentViewModel = _adminVm;     SelectedNavIndex = 4; break;
            case "Settings":  CurrentViewModel = _settingsVm;  SelectedNavIndex = 5; break;
            default:          CurrentViewModel = _dashboardVm; SelectedNavIndex = 0; break;
        }
    }

    public void NavigateToGroupDetail(int groupId, string groupName)
    {
        CurrentViewModel = new GroupDetailViewModel(this, groupId, groupName);
        SelectedNavIndex = 1;
    }

    public void NavigateToStudentDetail(int studentId)
    {
        CurrentViewModel = new StudentDetailViewModel(this, studentId);
        SelectedNavIndex = 2;
    }

    /// <summary>Переход на печать с преднастройкой: группа студента, режим «выбранные», отмечен только он.</summary>
    public void NavigateToPrintForStudent(int studentId)
    {
        if (!_printVm.PreparePrintForStudent(studentId))
            return;
        NavigateTo("Print");
    }

    public void RefreshAfterDemoSeed(bool refreshPrint = false)
    {
        _groupsVm.Reload();
        _studentsVm.Reload();
        _dashboardVm.Reload();
        if (refreshPrint)
            _printVm.ReloadData();
    }
}

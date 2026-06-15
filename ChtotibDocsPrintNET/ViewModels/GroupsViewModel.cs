using System.Collections.ObjectModel;
using System.Windows;
using ChtotibDocsPrintNET.Data;
using ChtotibDocsPrintNET.Models;
using ChtotibDocsPrintNET.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class GroupsViewModel : BaseViewModel
{
    private readonly MainViewModel _mainVm;
    [ObservableProperty] private bool _showOnlyGraduating;
    [ObservableProperty] private string _selectedAddress = "Все";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalGroupsCount;
    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<string> Addresses { get; } = new();

    public GroupsViewModel(MainViewModel mainVm) { _mainVm = mainVm; LoadAddresses(); LoadGroups(); }

    partial void OnShowOnlyGraduatingChanged(bool value) => LoadGroups();
    partial void OnSelectedAddressChanged(string value) => LoadGroups();
    partial void OnSearchTextChanged(string value) => LoadGroups();

    private void LoadAddresses()
    {
        Addresses.Clear();
        Addresses.Add("Все");
        Addresses.Add("Бабушкина, 66");
        Addresses.Add("Бабушкина, 2Б");
        try {
            foreach (var a in DatabaseService.Instance.GetDistinctAddresses())
                if (!Addresses.Contains(a)) Addresses.Add(a);
        } catch { }
        SelectedAddress = "Все";
    }

    private void LoadGroups()
    {
        try {
            var groups = DatabaseService.Instance.GetGroups(ShowOnlyGraduating,
                SelectedAddress == "Все" ? null : SelectedAddress,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Groups.Clear();
            foreach (var g in groups) Groups.Add(g);
            TotalGroupsCount = Groups.Count;
        } catch { TotalGroupsCount = 0; }
    }

    [RelayCommand] private void OpenGroup(Group? g) { if (g != null) _mainVm.NavigateToGroupDetail(g.Id, g.Name); }

    [RelayCommand]
    private void AddGroup()
    {
        var dlg = new AddGroupDialog();
        dlg.Owner = Application.Current.MainWindow;
        if (dlg.ShowDialog() == true) { LoadAddresses(); LoadGroups(); }
    }

    [RelayCommand]
    private void ImportGroups()
    {
        var result = Services.ImportService.ImportGroups();
        if (result != null)
        {
            MessageBox.Show(result, "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadAddresses(); LoadGroups();
        }
    }

    [RelayCommand] private void Refresh() => LoadGroups();

    public void Reload() => LoadGroups();
}

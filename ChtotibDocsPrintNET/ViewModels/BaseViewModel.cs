using CommunityToolkit.Mvvm.ComponentModel;

namespace ChtotibDocsPrintNET.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    protected void ClearError() => ErrorMessage = null;
}

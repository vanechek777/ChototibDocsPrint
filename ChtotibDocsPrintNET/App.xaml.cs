using System.Windows;
using ChtotibDocsPrintNET.ViewModels;

namespace ChtotibDocsPrintNET
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppSettings.EnsureInitialized();
            base.OnStartup(e);
        }
    }
}
